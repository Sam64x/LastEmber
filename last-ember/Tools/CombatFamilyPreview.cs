using Godot;
namespace LastEmber;

// Art-only timeline: production renderers, no Player, enemies or damage execution.
public partial class CombatFamilyPreview : Node2D
{
    public override async void _Ready()
    {
        var viewport=new SubViewport {Size=new Vector2I(1200,870),RenderTargetUpdateMode=SubViewport.UpdateMode.Always};AddChild(viewport);
        viewport.AddChild(new ColorRect {Size=new Vector2(1200,870),Color=new Color(.035f,.043f,.055f)});
        void Label(string text,Vector2 point,int size=17){var label=new Label {Text=text,Position=point};label.AddThemeFontSizeOverride("font_size",size);viewport.AddChild(label);}
        Label("LAST EMBER / FLAME COMBAT FAMILY",new Vector2(24,18),25);
        string[] slashNames={"OPENING CUT","RETURN CUT","FINISHER","LAST EMBER CUT"};
        string[] boltNames={"EMBER BOLT","PIERCING LANCE","LAST EMBER BOLT","HOSTILE / RED"};
        var slashes=new FireStrikeFx[4];var bolts=new EmberProjectileVisual[4];var contacts=new FireImpactFx[4];
        for(int i=0;i<4;i++)
        {
            Label(slashNames[i],new Vector2(25+i*300,75));Label(boltNames[i],new Vector2(25+i*300,325));
            slashes[i]=new FireStrikeFx {Position=new Vector2(55+i*300,208),Scale=Vector2.One*1.1f,Blue=i==3,ProcessMode=ProcessModeEnum.Disabled};viewport.AddChild(slashes[i]);
            var style=new StrikeStyleData {Windup=.09f,SweepSeconds=.13f,TailSeconds=.26f,StartAngle=i==1?1.3f:-1.3f,EndAngle=i==1?-1.3f:1.3f,Width=i>=2?30:21};
            slashes[i].Begin(Vector2.Right,1,style,1,.09f,1,i==3?1:0);
            bolts[i]=new EmberProjectileVisual {ProcessMode=ProcessModeEnum.Disabled};viewport.AddChild(bolts[i]);
            contacts[i]=new FireImpactFx {Position=new Vector2(260+i*300,430),ProcessMode=ProcessModeEnum.Disabled};viewport.AddChild(contacts[i]);
            contacts[i].Begin(contacts[i].Position,Vector2.Right,.5f,i==2,ImpactTraits.Projectile,(ulong)(i+1)*7919);
        }
        Label("SPLASH / IGNITION TO EMBERS",new Vector2(25,565),15);Label("SPLASH / BLUE",new Vector2(325,565));
        Label("DASH / DETACHED WAKE",new Vector2(625,565),15);Label("HERO / ONE FLAME",new Vector2(925,565));
        var splashes=new EmberSplashFx[2];
        for(int i=0;i<2;i++){splashes[i]=new EmberSplashFx {ProcessMode=ProcessModeEnum.Disabled};viewport.AddChild(splashes[i]);splashes[i].Begin(new Vector2(150+i*300,705),115,i==1);}
        var dash=new PlayerVisual {Position=new Vector2(650,738),Scale=Vector2.One*1.4f};viewport.AddChild(dash);
        var idle=new PlayerVisual {Position=new Vector2(1050,738),Scale=Vector2.One*2};viewport.AddChild(idle);
        Label("Production renderers / isolated 60fps art preview / no game scene",new Vector2(24,839),16);
        string output=ProjectSettings.GlobalizePath("res://../.tools/family-preview");DirAccess.MakeDirRecursiveAbsolute(output);
        for(int frame=0;frame<192;frame++)
        {
            float time=frame/60f,phase=time%1.6f;
            for(int i=0;i<4;i++)
            {
                slashes[i].SeekPreview(phase);
                bolts[i].Visible=phase<.65f;
                bolts[i].Position=new Vector2(100+i*300+Mathf.Min(phase/.65f,1)*160,430);
                bolts[i].Configure(Vector2.Right,i==2,i==1,i==3?new Color(1,.42f,.32f):Colors.White);
                bolts[i].Scale*=1.6f;bolts[i].Seek(phase);
                contacts[i].SeekPreview(phase-.65f);
            }
            for(int i=0;i<2;i++)splashes[i].Seek(phase);
            if(frame%96==0)dash.ResetForRoom();
            if(frame%96==18)dash.Dash(Vector2.Right);
            bool dashing=phase>=.3f&&phase<.48f;
            dash.Position=new Vector2(650+Mathf.Clamp(phase-.3f,0,.18f)*850,738);
            dash.Advance(1f/60,HeroVisualInput.Idle with {Velocity=dashing?Vector2.Right*850:Vector2.Zero,Aim=Vector2.Right,Dashing=dashing});
            idle.Advance(1f/60,HeroVisualInput.Idle);
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
            using var image=viewport.GetTexture().GetImage();image.SavePng(output+$"/frame-{frame:000}.png");
        }
        viewport.QueueFree();await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        GD.Print("Combat family art preview: 192 frames / no game scene.");GetTree().Quit();
    }
}
