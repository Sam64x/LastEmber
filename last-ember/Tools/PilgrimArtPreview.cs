using Godot;
namespace LastEmber;

// Production sprite rig only; no enemy AI, RunManager or damage simulation.
public partial class PilgrimArtPreview : Node2D
{
    public override async void _Ready()
    {
        var viewport=new SubViewport {Size=new Vector2I(1050,640),RenderTargetUpdateMode=SubViewport.UpdateMode.Always};AddChild(viewport);
        viewport.AddChild(new ColorRect {Size=new Vector2(1050,640),Color=new Color(.045f,.048f,.055f)});
        void Label(string text,Vector2 pos,int size=18){var label=new Label {Text=text,Position=pos};label.AddThemeFontSizeOverride("font_size",size);viewport.AddChild(label);}
        Label("ASH PILGRIMS / FIRST HUMANOID SET",new Vector2(24,18),25);
        var visuals=new PilgrimVisual[6];
        string[] names={"KNIGHT / HEAVY MELEE","HUNTER / CROSSBOW","PRIEST / ASH SEAL"};
        var kinds=new[]{EnemyKind.AshKnight,EnemyKind.AshHunter,EnemyKind.AshPriest};
        for(int i=0;i<3;i++)
        {
            Label(names[i],new Vector2(24+i*350,75));Label("GAMEPLAY SCALE",new Vector2(24+i*350,450),14);
            for(int row=0;row<2;row++)
            {
                var holder=new Node2D {Position=new Vector2(165+i*350,row==0?350:590),Scale=Vector2.One*(row==0?2.4f:1)};viewport.AddChild(holder);
                visuals[row*3+i]=new PilgrimVisual {Kind=kinds[i],ProcessMode=ProcessModeEnum.Disabled};holder.AddChild(visuals[row*3+i]);
            }
        }
        var state=new Label {Position=new Vector2(24,395)};viewport.AddChild(state);
        Label("Isolated asset preview / idle, walk, windup, attack, hurt / 60fps",new Vector2(24,614),15);
        string output=ProjectSettings.GlobalizePath("res://../.tools/pilgrim-preview");DirAccess.MakeDirRecursiveAbsolute(output);
        for(int frame=0;frame<240;frame++)
        {
            float t=frame/60f;
            bool walk=t>=.5f&&t<1.5f;float wind=t>=1.5f&&t<2.35f?(t-1.5f)/.85f:0;
            float release=t>=2.35f?t-2.35f:10;float hurt=t>=3.1f&&t<3.23f?.13f-(t-3.1f):0;
            state.Text=t<.5f?"IDLE":walk?"WALK":wind>0?"WINDUP":release<.4f?"ATTACK / RECOVERY":hurt>0?"HURT":"IDLE";
            foreach(var visual in visuals){visual.UpdatePose(walk?Vector2.Right*90:Vector2.Zero,Vector2.Right,wind,release,hurt);visual.Advance(1f/60);}
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
            using var image=viewport.GetTexture().GetImage();image.SavePng(output+$"/frame-{frame:000}.png");
        }
        viewport.QueueFree();await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);GetTree().Quit();
    }
}
