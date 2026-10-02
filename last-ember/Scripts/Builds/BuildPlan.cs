using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
namespace LastEmber;

// Menu-owned planning state. Uses the real acquisition rules on an independent BuildState.
public sealed class BuildPlan
{
    private const string Path="user://build_plan.cfg";
    private readonly RewardGenerator _rewards;
    private BuildState _state=new();
    public IReadOnlyList<BuildUpgradeData> Upgrades=>_state.Upgrades;
    public string Status {get;private set;}="";
    public bool Saved {get;private set;}=true;
    public bool Contains(string id)=>_state.Rank(id)>0;
    public BuildPlan(RewardGenerator rewards){_rewards=rewards;Load();}
    private BuildState Rebuild(IEnumerable<string> ids)
    {
        var state=new BuildState();
        foreach(var id in ids.Distinct(StringComparer.Ordinal))
        {
            var data=_rewards.Catalog.Find(item=>item.Id==id);
            if(data!=null&&state.Rank(id)==0)_rewards.GrantWithPrerequisites(state,data);
        }
        return state;
    }
    public bool Add(string id)
    {
        if(Contains(id))return false;
        var data=_rewards.Catalog.Find(item=>item.Id==id);
        if(data==null){Status="Улучшение отсутствует в каталоге.";return false;}
        var next=Rebuild(_state.Upgrades.Select(item=>item.Id));
        if(!_rewards.GrantWithPrerequisites(next,data))
        {Status="Не удалось собрать зависимости этого таланта.";return false;}
        int added=next.Upgrades.Count-_state.Upgrades.Count;
        _state=next;Save($"Добавлено узлов: {added}. Зависимости включены.");return true;
    }
    public int Remove(string id)
    {
        if(!Contains(id))return 0;
        var next=new BuildState();
        var pending=_state.Upgrades.Where(item=>item.Id!=id).ToList();
        // Acquiring only eligible nodes removes every orphan, including RequiredTags users.
        bool progress;
        do
        {
            progress=false;
            for(int i=pending.Count-1;i>=0;i--)
                if(next.CanAcquire(pending[i])){next.Acquire(pending[i]);pending.RemoveAt(i);progress=true;}
        }while(progress);
        int removed=_state.Upgrades.Count-next.Upgrades.Count;
        _state=next;Save($"Убрано узлов: {removed}. Зависимые ветки обновлены.");return removed;
    }
    public void Clear(){_state=new BuildState();Save("План очищен.");}
    public void RetrySave()=>Save("План сохранён.");
    private void Save(string message)
    {
        var config=new ConfigFile();config.SetValue("plan","version",1);
        config.SetValue("plan","upgrades",_state.Upgrades.Select(item=>item.Id).ToArray());
        Saved=config.Save(Path)==Error.Ok;
        Status=Saved?message:"Не удалось сохранить план. Попробуйте сохранить ещё раз.";
    }
    private void Load()
    {
        var config=new ConfigFile();var error=config.Load(Path);
        if(error==Error.FileNotFound)return;
        if(error!=Error.Ok){Saved=false;Status="Сохранённый план не удалось прочитать.";return;}
        try
        {
            var version=config.GetValue("plan","version",0);
            if(version.VariantType!=Variant.Type.Int||version.AsInt32()!=1)
            {Saved=false;Status="Версия сохранённого плана не поддерживается.";return;}
            var stored=config.GetValue("plan","upgrades",Array.Empty<string>());
            if(stored.VariantType!=Variant.Type.PackedStringArray)
            {Saved=false;Status="Формат сохранённого плана повреждён.";return;}
            var ids=stored.AsStringArray();
            _state=Rebuild(ids);
            Status=ids.Any(id=>!_rewards.Catalog.Any(item=>item.Id==id))
                ?"План загружен; устаревшие узлы пропущены.":"Сохранённый план загружен.";
        }
        catch(InvalidCastException){Saved=false;Status="Формат сохранённого плана повреждён.";}
    }
}
