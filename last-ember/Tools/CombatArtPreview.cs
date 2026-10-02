using Godot;
namespace LastEmber;

// Standalone art viewport: no RunManager, player input, enemies or damage code.
public partial class CombatArtPreview : Node2D
{
    public override async void _Ready()
    {
        var viewport=new SubViewport {Size=new Vector2I(1280,760),RenderTargetUpdateMode=SubViewport.UpdateMode.Always};
        AddChild(viewport);
        var background=new ColorRect {Size=new Vector2(1280,760),Color=new Color(.045f,.052f,.07f)};viewport.AddChild(background);
        void Label(string text,Vector2 position,int size=18)
        {
            var label=new Label {Text=text,Position=position};label.AddThemeFontSizeOverride("font_size",size);viewport.AddChild(label);
        }
        Label("LAST EMBER / CONTINUOUS SLASHES + CONTACT IMPACTS",new Vector2(28,18),24);
        var ribbons=new FireStrikeFx[4];
        string[] cuts={"CUT / OPENING","RETURN CUT","FINISHER","CHARGED / BLUE"};
        for(int i=0;i<4;i++)
        {
            Label(cuts[i],new Vector2(30+i*320,80));
            ribbons[i]=new FireStrikeFx {Position=new Vector2(75+i*320,245),Scale=new Vector2(1.35f,1.35f),Blue=i==3,ProcessMode=ProcessModeEnum.Disabled};viewport.AddChild(ribbons[i]);
            var style=new StrikeStyleData {Windup=.1f,SweepSeconds=.13f,TailSeconds=.26f,StartAngle=i==1?1.3f:i==2?-1.7f:-1.25f,EndAngle=i==1?-1.2f:i==2?1.6f:1.25f,Width=i>=2?29:21,Cleave=i>=2};
            ribbons[i].Begin(Vector2.Right,1,style,1,.1f,1,i==3?1:0);
        }
        var traits=new[]{ImpactTraits.None,ImpactTraits.Critical,ImpactTraits.Charged,ImpactTraits.Critical|ImpactTraits.Charged,ImpactTraits.ArmorBreak,ImpactTraits.Ignite};
        string[] titles={"NORMAL","CRITICAL","CHARGED","CRIT + CHARGE","ARMOR BREAK","IGNITE"};
        var impacts=new FireImpactFx[12];
        for(int row=0;row<2;row++)for(int col=0;col<6;col++)
        {
            int index=row*6+col;
            Label(titles[col]+(row==0?" / WARM":" / BLUE"),new Vector2(17+col*212,355+row*180),15);
            impacts[index]=new FireImpactFx {Scale=new Vector2(1.25f,1.25f),ProcessMode=ProcessModeEnum.Disabled};
            viewport.AddChild(impacts[index]);
            impacts[index].Begin(new Vector2(100+col*212,437+row*180),Vector2.Right,.9f,row==1,traits[col],(ulong)(index+1)*7919);
        }
        Label("Isolated art preview / 60fps playback / no game scene loaded",new Vector2(28,723),16);
        string output=ProjectSettings.GlobalizePath("res://../.tools/combat-preview");DirAccess.MakeDirRecursiveAbsolute(output);
        for(int frame=0;frame<96;frame++)
        {
            float phase=frame/60f% .8f;
            for(int i=0;i<4;i++)
            {
                ribbons[i].SeekPreview(phase);
            }
            foreach(var impact in impacts)impact.SeekPreview(phase-.1f);
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
            using var image=viewport.GetTexture().GetImage();image.SavePng(output+$"/frame-{frame:000}.png");
        }
        viewport.QueueFree();await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        GD.Print("Rendered combat art only: 96 frames / 60fps playback / no game scene.");GetTree().Quit();
    }
}
