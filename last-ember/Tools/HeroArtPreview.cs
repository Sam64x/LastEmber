using Godot;
namespace LastEmber;

// Production visual rig only: no Player, RunManager, level or combat execution.
public partial class HeroArtPreview : Node2D
{
    public override async void _Ready()
    {
        var viewport=new SubViewport {Size=new Vector2I(1200,900),RenderTargetUpdateMode=SubViewport.UpdateMode.Always};
        AddChild(viewport);
        viewport.AddChild(new ColorRect {Size=new Vector2(1200,900),Color=new Color(.035f,.043f,.055f)});
        void Caption(string text,Vector2 position,int size=18)
        {
            var label=new Label {Text=text,Position=position};label.AddThemeFontSizeOverride("font_size",size);viewport.AddChild(label);
        }
        Caption("LAST EMBER / RESPONSIVE HERO RIG",new Vector2(25,18),26);
        Caption("Move / settle in eight directions + continuous turning / 60fps art playback",new Vector2(25,55),17);
        string[] names={"EAST","SOUTH EAST","SOUTH","SOUTH WEST","WEST","NORTH WEST","NORTH / BLUE","NORTH EAST / BLUE","IDLE / BREATH","360 DEGREE FLOW","REVERSE / INERTIA","DASH / RECOVERY"};
        var rigs=new PlayerVisual[12];
        for(int i=0;i<12;i++)
        {
            int x=i%4,y=i/4;
            Caption(names[i],new Vector2(24+x*300,100+y*245),17);
            rigs[i]=new PlayerVisual {Position=new Vector2(150+x*300,252+y*245),Scale=new Vector2(2.1f,2.1f),ProcessMode=ProcessModeEnum.Disabled};
            viewport.AddChild(rigs[i]);
        }
        Caption("Isolated production visuals / no game scene loaded",new Vector2(25,863),16);
        string output=ProjectSettings.GlobalizePath("res://../.tools/hero-preview");DirAccess.MakeDirRecursiveAbsolute(output);
        const int frames=192;
        for(int frame=0;frame<frames;frame++)
        {
            float time=frame/60f;
            for(int i=0;i<rigs.Length;i++)
            {
                var direction=Vector2.FromAngle(i*Mathf.Tau/8);
                var velocity=time>.35f&&time<2.2f?direction*220:Vector2.Zero;
                bool dash=false;
                if(i==8){direction=Vector2.Down;velocity=Vector2.Zero;}
                if(i==9){direction=Vector2.FromAngle(time*Mathf.Tau/3.2f);velocity=direction*220;}
                if(i==10){direction=time<1.6f?Vector2.Right:Vector2.Left;velocity=direction*220;}
                if(i==11)
                {
                    direction=new Vector2(1,-.5f).Normalized();dash=time>=.6f&&time<.78f;
                    velocity=direction*(dash?850:time<2.2f?220:0);
                    if(frame==36)rigs[i].Dash(direction);
                }
                var input=HeroVisualInput.Idle with {Velocity=velocity,Aim=direction,Dashing=dash,Blue=i==6||i==7};
                rigs[i].Advance(1f/60,input);
            }
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
            using var image=viewport.GetTexture().GetImage();image.SavePng(output+$"/frame-{frame:000}.png");
        }
        viewport.QueueFree();await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        GD.Print("Hero art preview: 192 frames / 60fps / production visual rig only.");GetTree().Quit();
    }
}
