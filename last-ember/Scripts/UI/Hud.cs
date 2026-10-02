using System;
using System.Linq;
using Godot;

namespace LastEmber;

public partial class Hud : CanvasLayer
{
    public RunManager Run { get; set; } = null!;
    public Control Root { get; private set; } = null!;
    public Control? Modal { get; private set; }
    private Control _hud = null!;
    private Label _flame=null!,_room=null!,_dash=null!,_reveal=null!,_relics=null!,_toast=null!,_bossName=null!,_prompt=null!;
    private ProgressBar _flameBar=null!,_dashBar=null!,_bossBar=null!;
    private float _toastTime;
    private BiomeIntro? _biomeIntro;
    public void ClearBiomeIntro()
    {
        if(GodotObject.IsInstanceValid(_biomeIntro)){_hud.RemoveChild(_biomeIntro!);_biomeIntro!.QueueFree();}
        _biomeIntro=null;
    }
    public void ShowBiomeIntro()
    {
        ClearBiomeIntro();
        _biomeIntro=new BiomeIntro {Run=Run,Title=Run.Dungeon.Definition.DisplayName,Subtitle=Run.Dungeon.Definition.Lesson};
        _hud.AddChild(_biomeIntro);
    }
    public bool CollectionOpen { get; private set; }
    public static readonly Color Amber=new(1,.62f,.28f), Cream=new(.94f,.89f,.79f), Muted=new(.57f,.56f,.56f);
    public override void _Ready()
    {
        Layer=10;ProcessMode=ProcessModeEnum.Always;
        Root=new Control {MouseFilter=Control.MouseFilterEnum.Ignore};Root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);AddChild(Root);
        _hud=new Control {MouseFilter=Control.MouseFilterEnum.Ignore};_hud.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);Root.AddChild(_hud);
        Panel(_hud,new Rect2(0,0,1920,127),new Color(.035f,.037f,.046f,.97f));
        Panel(_hud,new Rect2(0,984,1920,96),new Color(.035f,.037f,.046f,.97f));
        Text(_hud,"L A S T   E M B E R",new Rect2(62,24,450,34),24,Muted);
        _flame=Text(_hud,"",new Rect2(62,64,340,37),29,Cream);
        _flameBar=Bar(_hud,new Rect2(380,72,375,12),Amber);
        _room=Text(_hud,"",new Rect2(820,27,305,62),23,Cream);
        _dash=Text(_hud,"",new Rect2(1150,33,230,36),21,Cream);
        _dashBar=Bar(_hud,new Rect2(1150,78,182,5),new Color(.72f,.71f,.77f));
        _reveal=Text(_hud,"",new Rect2(1450,33,410,64),20,Cream);
        _relics=Text(_hud,"",new Rect2(62,995,1260,80),17,Muted,true);
        Text(_hud,"WASD  MOVE     LMB  STRIKE     SPACE  DASH\nQ  ATTUNEMENT    E  USE    B  BUILD    ESC  PAUSE",new Rect2(1370,1004,530,56),17,Muted);
        _toast=Text(_hud,"",new Rect2(350,150,1220,52),27,Cream);_toast.HorizontalAlignment=HorizontalAlignment.Center;
        _bossName=Text(_hud,"",new Rect2(620,861,680,40),24,Cream);_bossName.HorizontalAlignment=HorizontalAlignment.Center;
        _bossBar=Bar(_hud,new Rect2(620,908,680,9),new Color(.82f,.25f,.18f));
        _prompt=Text(_hud,"",new Rect2(570,936,780,36),20,Amber);_prompt.HorizontalAlignment=HorizontalAlignment.Center;
    }
    public override void _Process(double delta)
    {
        if(Run.State==RunState.Menu)return;
        var player=Run.Player;if(!GodotObject.IsInstanceValid(player))return;
        bool blue=player.Flame.LastEmber;
        _flame.Text=$"{(blue?"LAST EMBER":"FLAME")}   {Mathf.CeilToInt(player.Flame.Current)} / {player.Flame.Maximum:0}";
        _flame.Modulate=blue?new Color(.3f,.75f,1):player.Flame.Ratio<.3f?new Color(1,.43f,.29f):Colors.White;
        if(_flameBar.GetThemeStylebox("fill") is StyleBoxFlat flameStyle)flameStyle.BgColor=blue?new Color(.25f,.7f,1):Amber;
        _flameBar.Value=player.Flame.Ratio*100;
        _room.Text=$"{Run.StageIndex+1:00} / {Run.RoomCount:00}   ·   {(Run.CurrentRoomPlan?.DisplayType??Run.CurrentStage.ToString().ToUpperInvariant())}\n{Run.Kills} FALLEN     {TimeText(Run.RunTime)}";
        _dash.Text=player.DashCooldown<=0?"SPACE   DASH READY":$"SPACE   {player.DashCooldown:0.0}s";
        _dashBar.Value=(1-player.DashCooldown)*100;
        _reveal.Text=$"Q   {player.AbilityName.ToUpperInvariant()}  "+(blue?"FREE":$"-{player.RevealCost:0}")+"\n"+(player.RevealCooldown<=0?Run.Dungeon.Definition.DisplayName:$"RECHARGING {player.RevealCooldown:0.0}s");
        _reveal.Modulate=blue?new Color(.35f,.8f,1):Colors.White;
        string relics="";foreach(var artifact in player.Build.Artifacts)relics+=(relics.Length>0?"  /  ":"")+artifact.DisplayName;
        if(relics.Length>110)relics=relics[..107]+"…";
        _relics.Text=$"RELICS  {player.Build.Artifacts.Count:00}"+(player.Build.AltarUsed?"    •    SACRIFICE BOUND":"")+"\n"+(relics.Length==0?"Carry a little light into the dark.":relics);
        string cores="";
        foreach(var upgrade in player.Progression.Upgrades)if(upgrade is CoreData)cores+=(cores.Length>0?" / ":"")+upgrade.DisplayName.Replace(" Core","").ToUpperInvariant();
        if(cores.Length>0)_relics.Text+=$"\nCORES {cores}  •  {player.Progression.Upgrades.Count} UPGRADES"+(player.Melee.HasCore?$"  •  MELEE {player.Melee.LastStep}/3  •  STREAK {player.Melee.HitStreak}":"")+(player.Melee.Charging?$"  •  CHARGE {player.Melee.ChargeRatio:P0}":"");
        if(player.Cores.OrbitSurge>0)_relics.Text+=$"  •  SURGE {player.Cores.OrbitSurge:0.0}s";
        if(player.Cores.OrbitHaste>0)_relics.Text+=$"  •  HASTE {player.Cores.OrbitHaste:0.0}s";
        if(Run.DevEnabled)_relics.Text+="   •   DEV MODE: F1";
        Enemy? boss=null;foreach(var enemy in Run.Enemies)if(enemy.Kind==EnemyKind.Boss)boss=enemy;
        _bossBar.Visible=boss!=null;_bossName.Visible=boss!=null;
        if(boss!=null)
        {
            _bossBar.Value=boss.Health/boss.MaxHealth*100;
            int phase=boss is ElementalBoss elemental?elemental.Phase:boss is Extinguisher extinguisher?extinguisher.Phase:1;
            _bossName.Text=Run.Dungeon.Definition.BossName.ToUpperInvariant()+$"   •   PHASE {phase}";
        }
        if(Run.Playing){_toastTime-=(float)delta;_toast.Visible=_toastTime>0;}
        bool nearShrine=Run.ShrineAvailable&&player.Position.DistanceTo(Run.Room.ShrinePosition)<90;
        bool nearAltar=Run.CurrentStage==StageKind.Altar&&player.Position.DistanceTo(Run.Room.Bounds.GetCenter())<110;
        _prompt.Text=Run.CurrentRoomPlan?.Type==RoomType.Ability && !Run.Room.Cleared && !Run.ShrineAvailable ? Run.Encounter!.Objective : Run.Encounter?.RiskAvailable==true ? Run.Encounter!.Objective : nearShrine?"E • EMBER SHRINE • RESTORE OR ARTIFACT":Run.Room.Cleared?"EASTERN GATE OPEN   →":nearAltar?"E • ALTAR OF SACRIFICE":"";
        if(boss is ElementalBoss guardian)
            _prompt.Text=guardian.Weakened>0?"FIRE INTERRUPTED • STRIKE NOW":guardian.Warning>0?$"{guardian.AttackName} • MOVE OUT OF THE MARKED AREA":guardian.Frost&&guardian.IceArmored?"Q • BREAK THE WARDEN'S ARMOR":guardian.WeakPointRemaining>0?"RECOVERING • STRIKE NOW":"";
    }
    public static string TimeText(float seconds)=>$"{(int)seconds/60:00}:{(int)seconds%60:00}";
    public void Toast(string text){_toast.Text=text;_toastTime=3.5f;_toast.Visible=true;}
    private void ClearModal()
    {
        CollectionOpen=false;
        if(Modal!=null){Root.RemoveChild(Modal);Modal.QueueFree();Modal=null;}
    }
    public void ShowHud(){ClearModal();_hud.Visible=true;if(GodotObject.IsInstanceValid(Run.Player))Run.Player.SuppressUiClick();}
    private Control Overlay(bool artwork=false)
    {
        ClearModal();
        Modal=new Control {MouseFilter=Control.MouseFilterEnum.Stop};Modal.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);Root.AddChild(Modal);
        Panel(Modal,new Rect2(0,0,1920,1080),new Color(.023f,.025f,.033f,artwork?.95f:.93f));
        if(artwork)Modal.AddChild(new MenuArt());
        return Modal;
    }
    public void ShowMenu()
    {
        ClearBiomeIntro();
        _hud.Visible=false;var root=Overlay(true);
        Text(root,"A  S U N L E S S  R O G U E L I T E",new Rect2(145,182,800,40),22,Amber);
        Text(root,"LAST\nEMBER",new Rect2(134,250,890,335),134,Cream);
        Panel(root,new Rect2(145,623,75,3),Amber);
        Text(root,"The sun is gone. The fire is yours.\nSpend your light. Survive the descent.",new Rect2(145,655,740,95),28,Muted);
        Button(root,"START RUN     →",new Rect2(145,795,360,80),()=>Run.StartRun(),true,"StartRun");
        Button(root,"КОЛЛЕКЦИЯ",new Rect2(525,795,280,80),ShowCollection,false,"Collection");
        Button(root,"SETTINGS",new Rect2(825,795,180,80),ShowAudio,false,"AudioSettings");
        Button(root,"QUIT",new Rect2(1100,915,380,45),()=>GetTree().Quit(),false,"Quit");
        Text(root,"WASD  MOVE    /    MOUSE  AIM    /    LMB  STRIKE\nSPACE  DASH    /    Q  ATTUNEMENT    /    E  USE    /    ESC  PAUSE",new Rect2(145,950,1000,65),18,Muted);
        for(int i=0;i<Run.Dungeons.Count;i++)
        {int selected=i;Button(root,Run.Dungeons[i].DisplayName.ToUpperInvariant(),new Rect2(1100,680+i*75,380,65),()=>Run.StartRun(dungeonIndex:selected),false,"Dungeon"+i);}
        AddDevToggle(root,new Vector2(145,891));
        Text(root,"01   /   THE FALLEN CITY",new Rect2(1370,976,420,38),18,Muted);
    }
    public void ShowCollection()
    {
        if(Run.State!=RunState.Menu)return;
        _hud.Visible=false;var root=Overlay();CollectionOpen=true;
        root.AddChild(new CollectionScreen {Run=Run,Back=ShowMenu});
    }
    public void ShowRewards()
    {
        var root=Overlay();
        Text(root,$"EMBER SHRINE    •    {Run.Player.Flame.Current:0} / {Run.Player.Flame.Maximum:0} FLAME",new Rect2(235,170,1450,42),21,Amber);
        Text(root,"Warmth or strength?",new Rect2(228,229,1460,86),62,Cream);
        Text(root,"Restore 20 Flame OR choose one Core, SubCore, talent or artifact.",new Rect2(235,326,1400,52),25,Muted);
        for(int i=0;i<Run.Offered.Count;i++)
        {
            int selected=i;var item=Run.Offered[i];float x=235+i*490;
            Panel(root,new Rect2(x,425,460,409),new Color(.067f,.063f,.068f));
            Panel(root,new Rect2(x,425,460,3),item.Rarity>=3?new Color(.78f,.46f,.71f):Amber);
            if(item.Artifact!=null)root.AddChild(new RelicGlyph {Position=new Vector2(x+52,478),Effect=item.Artifact.Effect});
            else Text(root,"✦",new Rect2(x+28,452,70,60),42,Amber);
            Text(root,item.Category,new Rect2(x+100,462,320,36),17,Muted);
            Text(root,item.DisplayName,new Rect2(x+28,532,400,64),28,Cream,true);
            Text(root,item.Description,new Rect2(x+28,605,400,130),20,Muted,true);
            Button(root,"CHOOSE   +",new Rect2(x+28,747,404,60),()=>Run.ChooseReward(selected),true,$"Reward{selected}");
        }
        Button(root,"RESTORE  +20 FLAME  •  FORGO ARTIFACT",new Rect2(545,878,830,70),()=>Run.ChooseRestore(),false,"RestoreFlame");
    }
    public void ShowAltar()
    {
        var root=Overlay();
        Text(root,"ALTAR OF SACRIFICE",new Rect2(235,170,1450,42),21,Amber);
        Text(root,"Power has a price.",new Rect2(228,229,1460,86),62,Cream);
        Text(root,"One sacrifice per altar. Bonuses and maximum Flame costs stack for this run.",new Rect2(235,326,1480,52),25,Muted);
        string[] names={"QUICKENED ASH","HOLLOW CORE","RUPTURE"};
        string[] desc={"−15 maximum Flame\n+25% attack speed","−20 maximum Flame\n+25% move speed below 15 Flame","−30 maximum Flame\n+26 dash explosion damage"};
        int[] cost={15,20,30};
        for(int i=0;i<3;i++)
        {
            int option=i;float x=235+i*490;
            Panel(root,new Rect2(x,440,460,330),new Color(.075f,.057f,.064f));
            Text(root,names[i],new Rect2(x+28,478,408,52),29,Cream);
            Text(root,desc[i],new Rect2(x+28,553,408,96),23,Muted);
            var button=Button(root,"SACRIFICE",new Rect2(x+28,680,404,61),()=>Run.ChooseAltar(option),true,$"Altar{option}");
            button.Disabled=Run.Player.Flame.Maximum-cost[i]<25;
            if(button.Disabled)button.Text="NOT ENOUGH MAX FLAME";
        }
        Button(root,"LEAVE THE ALTAR",new Rect2(730,836,460,70),()=>Run.ChooseAltar(-1),false,"LeaveAltar");
    }
    public void ShowPause()
    {
        var root=Overlay();
        AddDevToggle(root,new Vector2(70,290));
        Button(root,"DEV TOOLS  /  F1",new Rect2(70,370,430,65),()=>{Run.SetDevEnabled(true);Run.DevTools.Toggle();},false,"OpenDevTools");
        Button(root,"YOUR BUILD  /  B",new Rect2(70,455,430,65),Run.ToggleBuildView,false,"ViewBuild");
        Text(root,"TAKE A BREATH",new Rect2(640,250,900,50),22,Amber);
        Text(root,"The ember waits.",new Rect2(632,320,1020,100),65,Cream);
        Button(root,"RESUME",new Rect2(640,508,640,75),Run.TogglePause,true,"Resume");
        Button(root,"RESTART RUN",new Rect2(640,605,640,75),()=>Run.StartRun(),false,"Restart");
        Button(root,"MAIN MENU",new Rect2(640,702,640,75),Run.ShowMenu,false,"MainMenu");
        AudioSlider(root,"MUSIC",new Vector2(640,825),Run.Music.Volume,Run.Music.SetVolume,"MusicVolume");
        AudioSlider(root,"EFFECTS",new Vector2(640,902),Run.Audio.Volume,Run.Audio.SetVolume,"SfxVolume");
    }
    public void ShowBuild()
    {
        var root=Overlay();
        Text(root,"YOUR BUILD   •   GAME PAUSED",new Rect2(180,100,1450,40),22,Amber);
        Text(root,"The fire you carry.",new Rect2(173,157,1500,90),62,Cream);
        var player=Run.Player;
        Text(root,$"{player.Progression.Upgrades.Count} UPGRADES   /   {player.Build.Artifacts.Count} RELICS   /   {player.Flame.Current:0} / {player.Flame.Maximum:0} FLAME",new Rect2(180,268,1500,40),23,Muted);
        var scroll=new ScrollContainer {Name="BuildScroll",Position=new Vector2(180,340),Size=new Vector2(1560,550),HorizontalScrollMode=ScrollContainer.ScrollMode.Disabled};
        root.AddChild(scroll);
        var rows=new VBoxContainer {SizeFlagsHorizontal=Control.SizeFlags.ExpandFill};rows.AddThemeConstantOverride("separation",14);scroll.AddChild(rows);
        void Row(string category,string name,string description)
        {
            var panel=new PanelContainer {SizeFlagsHorizontal=Control.SizeFlags.ExpandFill};
            panel.AddThemeStyleboxOverride("panel",new StyleBoxFlat {BgColor=new Color(.067f,.063f,.068f),ContentMarginLeft=22,ContentMarginRight=22,ContentMarginTop=16,ContentMarginBottom=16});
            rows.AddChild(panel);
            var column=new VBoxContainer {SizeFlagsHorizontal=Control.SizeFlags.ExpandFill};column.AddThemeConstantOverride("separation",8);panel.AddChild(column);
            var title=new Label {Text=category+"  /  "+name,AutowrapMode=TextServer.AutowrapMode.WordSmart};
            title.AddThemeFontSizeOverride("font_size",23);title.AddThemeColorOverride("font_color",Amber);column.AddChild(title);
            var detail=new Label {Text=description,AutowrapMode=TextServer.AutowrapMode.WordSmart,SizeFlagsHorizontal=Control.SizeFlags.ExpandFill};
            detail.AddThemeFontSizeOverride("font_size",21);detail.AddThemeColorOverride("font_color",Cream);column.AddChild(detail);
        }
        foreach(var upgrade in player.Progression.Upgrades.OrderBy(item=>item is CoreData?0:item.Synergy?1:item is SubCoreData?2:3))
            Row(upgrade.Category,upgrade.DisplayName+(upgrade.MaxRank>1?$"   RANK {player.Progression.Rank(upgrade.Id)}/{upgrade.MaxRank}":""),upgrade.Description);
        foreach(var artifact in player.Build.Artifacts)Row("ARTIFACT",artifact.DisplayName,artifact.Description);
        if(player.Build.AltarUsed)Row("ALTAR","Sacrifice bound",$"Attack speed: {player.Build.AttackSpeed:0.00}x. Dash explosion damage: {player.Build.DashExplosionDamage:0}. Maximum Flame: {player.Flame.Maximum:0}.");
        if(player.Progression.Upgrades.Count==0&&player.Build.Artifacts.Count==0&&!player.Build.AltarUsed)
            Row("A NEW FLAME","Your build starts at the shrines.","Clear encounters and claim Cores, talents and relics. Cores combine; hybrid talents appear after you acquire both required Cores.");
        Button(root,"BACK   /   B OR ESC",new Rect2(650,934,620,70),Run.ToggleBuildView,true,"CloseBuild");
    }
    public void ShowAudio()
    {
        var root=Overlay();
        Text(root,"ATMOSPHERE & SOUND",new Rect2(500,220,1000,45),22,Amber);
        Text(root,"Shape the atmosphere.",new Rect2(493,289,1150,95),62,Cream);
        Text(root,"Set the density of ash, snow and drifting embers.\nMusic follows your Flame and the danger around you.",new Rect2(500,405,1050,85),25,Muted);
        AudioSlider(root,"MUSIC",new Vector2(500,535),Run.Music.Volume,Run.Music.SetVolume,"MusicVolume");
        AudioSlider(root,"EFFECTS",new Vector2(500,610),Run.Audio.Volume,Run.Audio.SetVolume,"SfxVolume");
        AudioSlider(root,"ATMOSPHERE",new Vector2(500,685),Run.Visuals.Atmosphere,Run.Visuals.SetAtmosphere,"AtmosphereDensity",Run.Visuals.Save);
        var flicker=new CheckButton {Name="TorchFlicker",Text="TORCH FLICKER",Position=new Vector2(500,765),Size=new Vector2(640,48),ButtonPressed=Run.Visuals.TorchFlicker};
        flicker.Toggled+=enabled=>{Run.Visuals.SetFlicker(enabled);Run.Visuals.Save();};root.AddChild(flicker);
        Button(root,"BACK",new Rect2(500,860,640,75),()=>{Run.Music.SaveSettings();Run.Visuals.Save();ShowMenu();},true,"AudioBack");
    }
    private void AudioSlider(Control parent,string title,Vector2 position,float value,Action<float> changed,string name,Action? save=null)
    {
        var label=Text(parent,$"{title}   {value*100:0}%",new Rect2(position,new Vector2(220,38)),21,Muted);
        var slider=new HSlider {Name=name,Position=position+new Vector2(240,0),Size=new Vector2(400,38),MinValue=0,MaxValue=100,Step=1,Value=value*100,MouseDefaultCursorShape=Control.CursorShape.PointingHand};
        slider.ValueChanged+=next=>{changed((float)next/100);label.Text=$"{title}   {next:0}%";};
        Action persist=save??Run.Music.SaveSettings;
        slider.DragEnded+=unused=>persist();
        slider.FocusExited+=persist;
        parent.AddChild(slider);
    }
    public void ShowEnd(bool victory)
    {
        var root=Overlay(true);
        Text(root,victory?"THE FURNACE LIVES":"YOUR LIGHT RETURNS TO ASH",new Rect2(145,215,1080,46),22,Amber);
        Text(root,victory?"DAWN\nREKINDLED.":"RUN\nOVER.",new Rect2(137,291,1150,270),104,Cream);
        Text(root,victory?"One ember was enough.":"The dark remembers. Try again.",new Rect2(145,586,1000,60),28,Muted);
        Text(root,$"{TimeText(Run.RunTime)}  TIME     /     {Run.Kills}  FALLEN     /     {Run.Player.Build.Artifacts.Count}  RELICS\n{Run.Player.Flame.Current:0} / {Run.Player.Flame.Maximum:0}  FLAME     •     SEED {Run.Seed}",new Rect2(145,677,1150,85),23,Cream);
        Button(root,victory?"NEW RUN   →":"RETRY   →",new Rect2(145,827,430,80),()=>Run.StartRun(),true,"Retry");
        Button(root,"MAIN MENU",new Rect2(600,827,310,80),Run.ShowMenu,false,"MainMenu");
    }
    private void AddDevToggle(Control parent,Vector2 position)
    {
        var toggle=new CheckButton {Name="DevModeToggle",Text="DEV MODE  •  F1 opens tools",Position=position,Size=new Vector2(490,48),ButtonPressed=Run.DevEnabled};
        toggle.AddThemeFontSizeOverride("font_size",21);toggle.Toggled+=Run.SetDevEnabled;parent.AddChild(toggle);
    }
    private static ColorRect Panel(Control parent,Rect2 rect,Color color)
    {
        var panel=new ColorRect {Position=rect.Position,Size=rect.Size,Color=color,MouseFilter=Control.MouseFilterEnum.Ignore};parent.AddChild(panel);return panel;
    }
    private static Label Text(Control parent,string text,Rect2 rect,int size,Color color,bool wrap=false)
    {
        var label=new Label {AutowrapMode=wrap?TextServer.AutowrapMode.WordSmart:TextServer.AutowrapMode.Off,Text=text,Position=rect.Position,Size=rect.Size,MouseFilter=Control.MouseFilterEnum.Ignore};
        label.AddThemeFontSizeOverride("font_size",size);label.AddThemeColorOverride("font_color",color);parent.AddChild(label);return label;
    }
    private static StyleBoxFlat Style(Color background,Color border,int width=1)
        =>new() {BgColor=background,BorderColor=border,BorderWidthBottom=width,BorderWidthTop=width,BorderWidthLeft=width,BorderWidthRight=width,ContentMarginLeft=20,ContentMarginRight=20};
    private static Button Button(Control parent,string text,Rect2 rect,Action action,bool primary,string name)
    {
        var button=new Button {Name=name,Text=text,Position=rect.Position,Size=rect.Size,MouseDefaultCursorShape=Control.CursorShape.PointingHand};
        button.AddThemeFontSizeOverride("font_size",23);
        button.AddThemeColorOverride("font_color",primary?new Color(.12f,.08f,.06f):Cream);
        button.AddThemeColorOverride("font_hover_color",primary?new Color(.12f,.08f,.06f):Colors.White);
        button.AddThemeStyleboxOverride("normal",Style(primary?Amber:new Color(.07f,.065f,.07f),primary?Amber:new Color(.29f,.25f,.24f)));
        button.AddThemeStyleboxOverride("hover",Style(primary?new Color(1,.76f,.46f):new Color(.18f,.12f,.09f),Amber,2));
        button.AddThemeStyleboxOverride("pressed",Style(new Color(.69f,.36f,.15f),Amber,2));
        button.AddThemeStyleboxOverride("focus",Style(new Color(0,0,0,0),Cream,2));
        button.AddThemeStyleboxOverride("disabled",Style(new Color(.09f,.085f,.09f),new Color(.2f,.18f,.18f)));
        button.Pressed+=action;parent.AddChild(button);return button;
    }
    private static ProgressBar Bar(Control parent,Rect2 rect,Color color)
    {
        var bar=new ProgressBar {Position=rect.Position,Size=rect.Size,ShowPercentage=false,MouseFilter=Control.MouseFilterEnum.Ignore};
        bar.AddThemeStyleboxOverride("background",new StyleBoxFlat {BgColor=new Color(.16f,.12f,.12f)});
        bar.AddThemeStyleboxOverride("fill",new StyleBoxFlat {BgColor=color});parent.AddChild(bar);return bar;
    }
}

public partial class MenuArt : Node2D
{
    private float _time;
    public override void _Process(double delta){_time+=(float)delta;QueueRedraw();}
    public override void _Draw()
    {
        var center=new Vector2(1400,528);
        for(int i=12;i>0;i--)DrawCircle(center,85+i*19,new Color(.75f,.26f,.05f,.008f*(13-i)));
        for(int i=0;i<3;i++)DrawArc(center,250+i*18,.22f,Mathf.Tau-.22f,100,new Color(.55f,.34f,.22f,.26f-i*.05f),1,true);
        for(int i=0;i<40;i++)
        {
            float angle=i*Mathf.Tau/40;
            var dir=Vector2.FromAngle(angle);
            DrawLine(center+dir*283,center+dir*(i%5==0?306:291),new Color(.55f,.37f,.25f,.4f),2,true);
        }
        DrawColoredPolygon(new[] {center+new Vector2(-135,123),center+new Vector2(-100,-13),center+new Vector2(-49,-45),center+new Vector2(-13,-184),center+new Vector2(58,-76),center+new Vector2(70,-123),center+new Vector2(132,59),center+new Vector2(96,157),center+new Vector2(0,202)},new Color(.52f,.20f,.075f));
        DrawColoredPolygon(new[] {center+new Vector2(-73,102),center+new Vector2(-59,20),center+new Vector2(-13,-139),center+new Vector2(28,-43),center+new Vector2(38,7),center+new Vector2(61,-24),center+new Vector2(83,96),center+new Vector2(35,148),center+new Vector2(-20,154)},new Color(.96f,.44f,.13f));
        DrawColoredPolygon(new[] {center+new Vector2(-32,102),center+new Vector2(-7,4),center+new Vector2(23,72),center+new Vector2(39,115),center+new Vector2(9,142),center+new Vector2(-22,132)},new Color(1,.81f,.43f));
        for(int i=0;i<25;i++)
        {
            float x=Mathf.Sin(i*73.13f)*220,y=Mathf.PosMod(i*51-_time*(15+i%4*6),600)-350;
            DrawCircle(center+new Vector2(x,y),i%3+1,new Color(1,.57f,.2f,.28f+(i%4)*.13f));
        }
        DrawLine(new Vector2(1130,865),new Vector2(1670,865),new Color(.35f,.24f,.19f),1);
    }
}

public partial class RelicGlyph : Node2D
{
    public ArtifactEffect Effect { get; set; }
    public override void _Draw()
    {
        var c=Hud.Amber;
        DrawArc(Vector2.Zero,23,0,Mathf.Tau,6,c,2,true);
        float angle=(int)Effect*.7f;
        DrawLine(Vector2.FromAngle(angle)*17,-Vector2.FromAngle(angle)*17,c,3,true);
        DrawCircle(Vector2.Zero,6,c);
        DrawLine(new Vector2(-9,30),new Vector2(9,30),c,2);
    }
}
