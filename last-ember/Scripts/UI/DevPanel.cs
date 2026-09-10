using System;
using Godot;

namespace LastEmber;

// Manual sandbox UI, opt-in per application session. No automatic play or tests.
public partial class DevPanel : CanvasLayer
{
    public RunManager Run { get; set; }=null!;
    public bool Open=>_root?.Visible==true;
    private Control? _root;
    private VBoxContainer _rows=null!;
    private Label _status=null!;
    private LineEdit _search=null!;
    private int _category;
    private bool _resume;
    public override void _Ready(){Layer=40;ProcessMode=ProcessModeEnum.Always;}
    public void Toggle()
    {
        if(Open){Close();return;}
        if(!Run.DevEnabled||Run.State is not (RunState.Playing or RunState.Pause))return;
        _resume=Run.Playing;
        if(_resume)Run.TogglePause();
        BuildPanel();Run.Player.SuppressUiClick();
    }
    public void Close()
    {
        if(_root==null)return;
        _root.Hide();
        if(_resume&&Run.State==RunState.Pause)Run.TogglePause();
        _resume=false;
    }
    private void BuildPanel()
    {
        if(_root!=null){RemoveChild(_root);_root.QueueFree();}
        _root=new Control {MouseFilter=Control.MouseFilterEnum.Stop};
        _root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);AddChild(_root);
        _root.AddChild(new ColorRect {Size=new Vector2(1920,1080),Color=new Color(.025f,.03f,.04f,.99f)});
        LabelAt("DEV MODE / BUILD LAB",new Vector2(70,25),30);
        LabelAt("Game paused. Granting a talent also grants its required Core and SubCores.",new Vector2(70,76),21);
        AddButton(_root,"CLOSE / F1",new Vector2(1590,30),new Vector2(250,52),Close);
        var invulnerable=new CheckButton {Text="Ignore incoming damage",Position=new Vector2(70,123),ButtonPressed=Run.DevInvulnerable};
        invulnerable.Toggled+=value=>Run.DevInvulnerable=value;_root.AddChild(invulnerable);
        AddButton(_root,"RESTORE FLAME",new Vector2(420,120),new Vector2(230,48),()=>{Run.Player.Flame.Heal(Run.Player.Flame.Maximum);Refresh();});
        AddButton(_root,"LAST EMBER",new Vector2(670,120),new Vector2(230,48),()=>{Run.Player.Flame.Damage(Mathf.Max(0,Run.Player.Flame.Current-1));Refresh();});
        AddButton(_root,"RESET BUILD",new Vector2(920,120),new Vector2(230,48),()=>{Run.Player.ResetDevBuild();Refresh();});
        AddButton(_root,"ALL UPGRADES",new Vector2(1170,120),new Vector2(260,48),()=>
        {foreach(var data in Run.Rewards.Catalog)Run.Rewards.GrantWithPrerequisites(Run.Player.Progression,data);Refresh();});
        AddButton(_root,"ALL ARTIFACTS",new Vector2(1450,120),new Vector2(290,48),()=>
        {foreach(var data in Run.Artifacts)Run.Player.Build.Apply(data,Run.Player.Flame);Refresh();});
        AddButton(_root,"TARGET",new Vector2(70,185),new Vector2(225,48),()=>Run.DevSpawnTarget());
        AddButton(_root,"ARMORED TARGET",new Vector2(310,185),new Vector2(270,48),()=>Run.DevSpawnTarget(armored:true));
        AddButton(_root,"FIRE TARGET",new Vector2(595,185),new Vector2(240,48),()=>Run.DevSpawnTarget(fire:true));
        AddButton(_root,"WEAK POINTS 8s",new Vector2(850,185),new Vector2(270,48),()=>{foreach(var enemy in Run.Enemies)enemy.OpenWeakPoint(8);});
        AddButton(_root,"CLEAR TARGETS",new Vector2(1135,185),new Vector2(270,48),Run.DevClearTargets);
        AddButton(_root,"RESET: MELEE MIX",new Vector2(1420,185),new Vector2(340,48),()=>
        {
            Run.Player.ResetDevBuild();
            foreach(var id in new[]{"combo_core","heavy_core","crit_core"})
            {
                var data=Run.Rewards.Catalog.Find(item=>item.Id==id);
                if(data!=null)Run.Rewards.GrantWithPrerequisites(Run.Player.Progression,data);
            }
            Refresh();
        });
        _status=LabelAt("",new Vector2(70,251),20);_status.Size=new Vector2(1770,32);_status.ClipText=true;
        var category=new OptionButton {Position=new Vector2(70,294),Size=new Vector2(300,48)};
        foreach(var name in new[]{"All","Cores","SubCores","Talents","Synergies","Artifacts"})category.AddItem(name);
        category.Selected=_category;category.ItemSelected+=value=>{_category=(int)value;Refresh();};_root.AddChild(category);
        _search=new LineEdit {Position=new Vector2(400,294),Size=new Vector2(950,48),PlaceholderText="Search name, description or tag"};
        _search.TextChanged+=unused=>Refresh();_root.AddChild(_search);
        LabelAt("Owned upgrades stay active when DEV MODE is turned off. RESET BUILD removes upgrades, artifacts and altar bonuses.",new Vector2(70,992),19);
        var scroll=new ScrollContainer {Position=new Vector2(70,363),Size=new Vector2(1770,600),HorizontalScrollMode=ScrollContainer.ScrollMode.Disabled};
        _root.AddChild(scroll);
        _rows=new VBoxContainer {SizeFlagsHorizontal=Control.SizeFlags.ExpandFill};
        _rows.AddThemeConstantOverride("separation",10);scroll.AddChild(_rows);Refresh();
    }
    private bool Matches(string text)=>text.Contains(_search.Text,StringComparison.OrdinalIgnoreCase);
    private void Refresh()
    {
        if(_root==null)return;
        foreach(var child in _rows.GetChildren()){_rows.RemoveChild(child);child.QueueFree();}
        var player=Run.Player;
        _status.Text=$"FLAME {player.Flame.Current:0}/{player.Flame.Maximum:0}   |   {player.Progression.Upgrades.Count} upgrades   |   {player.Build.Artifacts.Count} artifacts   |   Tags: {string.Join(", ",player.Progression.Tags)}";
        foreach(var data in Run.Rewards.Catalog)
        {
            bool visible=_category==0 || _category==1&&data is CoreData || _category==2&&data is SubCoreData ||
                _category==3&&data is TalentData&&!data.Synergy || _category==4&&data.Synergy;
            if(!visible || !Matches(data.DisplayName+" "+data.Description+" "+string.Join(" ",data.Tags)))continue;
            int rank=player.Progression.Rank(data.Id);
            string requirements=data.RequiredIds.Length>0?"  |  Needs: "+string.Join(", ",data.RequiredIds):"";
            Row(data.Category+" / "+data.DisplayName+"  ["+string.Join(", ",data.Tags)+"]",data.Description+requirements,
                rank>=data.MaxRank?"OWNED":rank>0?$"RANK {rank} +":"GRANT",rank>=data.MaxRank,()=>
                {Run.Rewards.GrantWithPrerequisites(player.Progression,data);Refresh();});
        }
        if(_category is 0 or 5)
        foreach(var data in Run.Artifacts)
        {
            if(!Matches(data.DisplayName+" "+data.Description+" "+string.Join(" ",data.Tags)))continue;
            bool owned=player.Build.Owns(data.Id);
            bool locked=owned&&!data.Stackable || data.Effect==ArtifactEffect.Glass&&player.Flame.Maximum<50;
            Row("ARTIFACT / "+data.DisplayName,data.Description,locked?owned?"OWNED":"MAX FLAME LOW":owned?"ADD STACK":"GRANT",locked,()=>
                {player.Build.Apply(data,player.Flame);Refresh();});
        }
    }
    private void Row(string title,string description,string action,bool disabled,Action pressed)
    {
        var panel=new PanelContainer {CustomMinimumSize=new Vector2(1700,118)};
        panel.AddThemeStyleboxOverride("panel",new StyleBoxFlat {BgColor=new Color(.075f,.085f,.105f),ContentMarginLeft=18,ContentMarginRight=18,ContentMarginTop=12,ContentMarginBottom=12});
        var row=new HBoxContainer();row.AddThemeConstantOverride("separation",25);panel.AddChild(row);
        var column=new VBoxContainer {SizeFlagsHorizontal=Control.SizeFlags.ExpandFill};row.AddChild(column);
        var heading=new Label {Text=title};heading.AddThemeFontSizeOverride("font_size",22);heading.AddThemeColorOverride("font_color",Hud.Amber);column.AddChild(heading);
        var detail=new Label {Text=description,AutowrapMode=TextServer.AutowrapMode.WordSmart,CustomMinimumSize=new Vector2(1260,0)};
        detail.AddThemeFontSizeOverride("font_size",19);column.AddChild(detail);
        var button=new Button {Text=action,Disabled=disabled,CustomMinimumSize=new Vector2(210,66)};
        button.Pressed+=pressed;row.AddChild(button);_rows.AddChild(panel);
    }
    private Label LabelAt(string text,Vector2 position,int size)
    {
        var label=new Label {Text=text,Position=position};label.AddThemeFontSizeOverride("font_size",size);_root!.AddChild(label);return label;
    }
    private static void AddButton(Control parent,string title,Vector2 position,Vector2 size,Action pressed)
    {
        var button=new Button {Text=title,Position=position,Size=size};button.AddThemeFontSizeOverride("font_size",19);button.Pressed+=pressed;parent.AddChild(button);
    }
}
