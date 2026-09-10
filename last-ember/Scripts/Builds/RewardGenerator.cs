using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace LastEmber;

public sealed class RewardOption
{
    public ArtifactData? Artifact { get; init; }
    public BuildUpgradeData? Upgrade { get; init; }
    public string Id => Upgrade?.Id ?? Artifact!.Id;
    public string DisplayName => Upgrade?.DisplayName ?? Artifact!.DisplayName;
    public string Description => Upgrade?.Description ?? Artifact!.Description;
    public int Rarity => Upgrade?.Rarity ?? Artifact!.Rarity;
    public string Category => Upgrade?.Category ?? "ARTIFACT";
    public string[] Tags => Upgrade?.Tags ?? Artifact!.Tags;
    public bool Acquire(Player player) => Upgrade!=null ? player.Progression.Acquire(Upgrade) : player.Build.Apply(Artifact!,player.Flame);
}

public sealed class RewardGenerator
{
    public List<BuildUpgradeData> Catalog { get; } = new();
    public void Load()
    {
        Catalog.Clear();
        foreach(var file in ResourceLoader.ListDirectory("res://Resources/Builds"))
            if(file.EndsWith(".tres",StringComparison.Ordinal))
                Catalog.Add(ResourceLoader.Load<BuildUpgradeData>("res://Resources/Builds/"+file));
        Catalog.Sort((a,b)=>string.CompareOrdinal(a.Id,b.Id));
    }
    public List<RewardOption> Generate(Player player,IEnumerable<ArtifactData> artifacts,RandomNumberGenerator rng,Func<ArtifactData,float> artifactWeight,int count=3)
    {
        var available=new List<RewardOption>();
        foreach(var data in Catalog)if(player.Progression.CanAcquire(data))available.Add(new RewardOption {Upgrade=data});
        foreach(var data in artifacts)
            if((data.Stackable||!player.Build.Owns(data.Id)) &&
               (data.Effect!=ArtifactEffect.Glass || player.Flame.Maximum>=50))
                available.Add(new RewardOption {Artifact=data});
        var tags=new HashSet<string>(player.Progression.Tags,StringComparer.Ordinal);
        foreach(var artifact in player.Build.Artifacts)tags.UnionWith(artifact.Tags);
        var result=new List<RewardOption>();
        while(result.Count<count && available.Count>0)
        {
            float roll=rng.Randf();
            var bucket=available.Where(item=>roll<.7f
                ? item.Upgrade!=null && (item.Upgrade is CoreData || !item.Upgrade.Synergy && item.Tags.Any(tags.Contains))
                : roll<.9f ? item.Upgrade?.Synergy==true || item.Upgrade is SubCoreData
                : true).ToList();
            // Empty categories fall back to ALL eligible rewards, never to locked talents.
            if(bucket.Count==0)bucket=available;
            float Weight(RewardOption item)=>Mathf.Max(.01f,item.Upgrade?.Weight ?? artifactWeight(item.Artifact!));
            float pick=rng.Randf()*bucket.Sum(Weight);var chosen=bucket[^1];
            foreach(var item in bucket){pick-=Weight(item);if(pick<=0){chosen=item;break;}}
            result.Add(chosen);available.Remove(chosen);
        }
        return result;
    }
    // DEV grants prerequisites in dependency order, keeping real acquisition rules intact.
    public bool GrantWithPrerequisites(BuildState state,BuildUpgradeData data)
        => Grant(state,data,new HashSet<string>(StringComparer.Ordinal));
    private bool Grant(BuildState state,BuildUpgradeData data,HashSet<string> visiting)
    {
        if(state.Rank(data.Id)>=data.MaxRank)return false;
        if(!visiting.Add(data.Id))return false;
        foreach(var id in data.RequiredIds)
        {
            if(state.Rank(id)>0)continue;
            var prerequisite=Catalog.Find(item=>item.Id==id);
            if(prerequisite==null || !Grant(state,prerequisite,visiting))return false;
        }
        visiting.Remove(data.Id);return state.Acquire(data);
    }
}
