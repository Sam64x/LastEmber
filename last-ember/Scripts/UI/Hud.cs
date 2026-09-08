using System;
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
        Text(_hud,"WASD  MOVE     LMB  STRIKE     SPACE  DASH\nQ  REVEAL     E  USE     ESC  PAUSE",new Rect2(1390,1004,490,56),17,Muted);
        _toast=Text(_hud,"",new Rect2(350,150,1220,52),27,Cream);_toast.HorizontalAlignment=HorizontalAlignment.Center;
        _bossName=Text(_hud,"",new Rect2(620,861,680,40),24,Cream);_bossName.HorizontalAlignment=HorizontalAlignment.Center;
        _bossBar=Bar(_hud,new Rect2(620,908,680,9),new Color(.82f,.25f,.18f));
        _prompt=Text(_hud,"",new Rect2(570,936,780,36),20,Amber);_prompt.HorizontalAlignment=HorizontalAlignment.Center;
    }
    public override void _Process(double delta)
    {
        if(Run.State==RunState.Menu)return;
        var player=Run.Player;if(!GodotObject.IsInstanceValid(player))return;
        _flame.Text=$"FLAME   {Mathf.CeilToInt(player.Flame.Current)} / {player.Flame.Maximum:0}";
        _flame.Modulate=player.Flame.Ratio<.3f?new Color(1,.43f,.29f):Colors.White;
        _flameBar.Value=player.Flame.Ratio*100;
        _room.Text=$"{Run.StageIndex+1:00} / 10   ·   {Run.CurrentStage.ToString().ToUpperInvariant()}\n{Run.Kills} FALLEN     {TimeText(Run.RunTime)}";
        _dash.Text=player.DashCooldown<=0?"SPACE   DASH READY":$"SPACE   {player.DashCooldown:0.0}s";
        _dashBar.Value=(1-player.DashCooldown)*100;
        _reveal.Text=$"Q   REVEAL  −{player.RevealCost:0}\n"+(player.RevealCooldown<=0?"LIGHT THE DARK":$"RECHARGING  {player.RevealCooldown:0.0}s");
        string relics="";foreach(var artifact in player.Build.Artifacts)relics+=(relics.Length>0?"  /  ":"")+artifact.DisplayName;
        _relics.Text=$"RELICS  {player.Build.Artifacts.Count:00}"+(player.Build.AltarUsed?"    •    SACRIFICE BOUND":"")+"\n"+(relics.Length==0?"Carry a little light into the dark.":relics);
        Extinguisher? boss=null;foreach(var enemy in Run.Enemies)if(enemy is Extinguisher found)boss=found;
        _bossBar.Visible=boss!=null;_bossName.Visible=boss!=null;
        if(boss!=null){_bossBar.Value=boss.Health/boss.MaxHealth*100;_bossName.Text=$"THE EXTINGUISHER    /    PHASE {boss.Phase}";}
        if(Run.Playing){_toastTime-=(float)delta;_toast.Visible=_toastTime>0;}
        bool nearShrine=Run.ShrineAvailable&&player.Position.DistanceTo(Run.Room.ShrinePosition)<90;
        bool nearAltar=Run.CurrentStage==StageKind.Altar&&player.Position.DistanceTo(Run.Room.Bounds.GetCenter())<110;
        _prompt.Text=nearShrine?"E • EMBER SHRINE • RESTORE OR ARTIFACT":Run.Room.Cleared?"EASTERN GATE OPEN   →":nearAltar?"E • ALTAR OF SACRIFICE":"";
    }
    public static string TimeText(float seconds)=>$"{(int)seconds/60:00}:{(int)seconds%60:00}";
    public void Toast(string text){_toast.Text=text;_toastTime=3.5f;_toast.Visible=true;}
    private void ClearModal()
    {
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
        _hud.Visible=false;var root=Overlay(true);
        Text(root,"A  S U N L E S S  R O G U E L I T E",new Rect2(145,182,800,40),22,Amber);
        Text(root,"LAST\nEMBER",new Rect2(134,250,890,335),134,Cream);
        Panel(root,new Rect2(145,623,75,3),Amber);
        Text(root,"The sun is gone. The fire is yours.\nSpend your light. Survive the descent.",new Rect2(145,655,740,95),28,Muted);
        Button(root,"START RUN     →",new Rect2(145,795,425,80),()=>Run.StartRun(),true,"StartRun");
        Button(root,"QUIT",new Rect2(590,795,190,80),()=>GetTree().Quit(),false,"Quit");
        Button(root,"AUDIO",new Rect2(805,795,220,80),ShowAudio,false,"AudioSettings");
        Text(root,"WASD  MOVE    /    MOUSE  AIM    /    LMB  STRIKE\nSPACE  DASH    /    Q  REVEAL    /    E  USE    /    ESC  PAUSE",new Rect2(145,950,1000,65),18,Muted);
        Text(root,"01   /   THE FALLEN CITY",new Rect2(1370,976,420,38),18,Muted);
    }
    public void ShowRewards()
    {
        var root=Overlay();
        Text(root,$"EMBER SHRINE    •    {Run.Player.Flame.Current:0} / {Run.Player.Flame.Maximum:0} FLAME",new Rect2(235,170,1450,42),21,Amber);
        Text(root,"Warmth or strength?",new Rect2(228,229,1460,86),62,Cream);
        Text(root,"Restore 20 Flame OR bind one artifact. You can choose only once.",new Rect2(235,326,1400,52),25,Muted);
        for(int i=0;i<Run.Offered.Count;i++)
        {
            int selected=i;var item=Run.Offered[i];float x=235+i*490;
            Panel(root,new Rect2(x,425,460,409),new Color(.067f,.063f,.068f));
            Panel(root,new Rect2(x,425,460,3),item.Rarity>=3?new Color(.78f,.46f,.71f):Amber);
            root.AddChild(new RelicGlyph {Position=new Vector2(x+52,478),Effect=item.Effect});
            Text(root,item.Rarity>=3?"RARE RELIC":"EMBER RELIC",new Rect2(x+100,462,320,36),17,Muted);
            Text(root,item.DisplayName,new Rect2(x+28,538,400,58),31,Cream);
            Text(root,item.Description,new Rect2(x+28,611,400,109),23,Muted,true);
            Button(root,"BIND RELIC   +",new Rect2(x+28,747,404,60),()=>Run.ChooseReward(selected),true,$"Reward{selected}");
        }
        Button(root,"RESTORE  +20 FLAME  •  FORGO ARTIFACT",new Rect2(545,878,830,70),()=>Run.ChooseRestore(),false,"RestoreFlame");
    }
    public void ShowAltar()
    {
        var root=Overlay();
        Text(root,"ALTAR OF SACRIFICE",new Rect2(235,170,1450,42),21,Amber);
        Text(root,"Power has a price.",new Rect2(228,229,1460,86),62,Cream);
        Text(root,"Give up maximum Flame for the rest of this run. Choose once, or walk away.",new Rect2(235,326,1480,52),25,Muted);
        string[] names={"QUICKENED ASH","HOLLOW CORE","RUPTURE"};
        string[] desc={"−15 maximum Flame\n+25% attack speed","−20 maximum Flame\n+25% move speed below 15 Flame","−30 maximum Flame\nEvery dash creates an explosion"};
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
        Text(root,"TAKE A BREATH",new Rect2(640,250,900,50),22,Amber);
        Text(root,"The ember waits.",new Rect2(632,320,1020,100),65,Cream);
        Button(root,"RESUME",new Rect2(640,508,640,75),Run.TogglePause,true,"Resume");
        Button(root,"RESTART RUN",new Rect2(640,605,640,75),()=>Run.StartRun(),false,"Restart");
        Button(root,"MAIN MENU",new Rect2(640,702,640,75),Run.ShowMenu,false,"MainMenu");
        AudioSlider(root,"MUSIC",new Vector2(640,825),Run.Music.Volume,Run.Music.SetVolume,"MusicVolume");
        AudioSlider(root,"EFFECTS",new Vector2(640,902),Run.Audio.Volume,Run.Audio.SetVolume,"SfxVolume");
    }
    public void ShowAudio()
    {
        var root=Overlay();
        Text(root,"SOUND & SILENCE",new Rect2(500,220,1000,45),22,Amber);
        Text(root,"Listen to your flame.",new Rect2(493,289,1150,95),62,Cream);
        Text(root,"Music follows Flame, nearby danger and the light of the arena.\nLower a slider to zero to mute that channel.",new Rect2(500,405,1050,85),25,Muted);
        AudioSlider(root,"MUSIC",new Vector2(500,575),Run.Music.Volume,Run.Music.SetVolume,"MusicVolume");
        AudioSlider(root,"EFFECTS",new Vector2(500,670),Run.Audio.Volume,Run.Audio.SetVolume,"SfxVolume");
        Button(root,"BACK",new Rect2(500,820,640,75),()=>{Run.Music.SaveSettings();ShowMenu();},true,"AudioBack");
    }
    private void AudioSlider(Control parent,string title,Vector2 position,float value,Action<float> changed,string name)
    {
        var label=Text(parent,$"{title}   {value*100:0}%",new Rect2(position,new Vector2(220,38)),21,Muted);
        var slider=new HSlider {Name=name,Position=position+new Vector2(240,0),Size=new Vector2(400,38),MinValue=0,MaxValue=100,Step=1,Value=value*100,MouseDefaultCursorShape=Control.CursorShape.PointingHand};
        slider.ValueChanged+=next=>{changed((float)next/100);label.Text=$"{title}   {next:0}%";};
        slider.DragEnded+=unused=>Run.Music.SaveSettings();
        slider.FocusExited+=Run.Music.SaveSettings;
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
