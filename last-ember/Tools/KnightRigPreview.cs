using Godot;
namespace LastEmber;

// Isolated art stage: no RunManager, enemy AI, combat, or game input.
public partial class KnightRigPreview : Node2D
{
    public override async void _Ready()
    {
        var viewport=new SubViewport {Size=new Vector2I(1050,640),RenderTargetUpdateMode=SubViewport.UpdateMode.Always};AddChild(viewport);
        viewport.AddChild(new ColorRect {Size=new Vector2(1050,640),Color=new Color(.045f,.048f,.055f)});
        void Label(string text,Vector2 pos,int size=18){var label=new Label {Text=text,Position=pos};label.AddThemeFontSizeOverride("font_size",size);viewport.AddChild(label);}
        Label("ASH KNIGHT / COHESIVE SILHOUETTE / V3",new Vector2(24,18),25);
        Label("ORIGINAL DESIGN",new Vector2(24,65));Label("REBUILT / V3",new Vector2(374,65));Label("GAMEPLAY SCALE / 1x",new Vector2(740,65));
        var legacyHolder=new Node2D {Position=new Vector2(170,425),Scale=Vector2.One*2.8f};viewport.AddChild(legacyHolder);
        var atlas=GD.Load<Texture2D>(PilgrimVisual.Asset(EnemyKind.AshKnight));
        var legacyMaterial=new ShaderMaterial {Shader=GD.Load<Shader>("res://Assets/Shaders/pilgrim_pose.gdshader")};
        legacyMaterial.SetShaderParameter("idle_bottom",.89f);legacyMaterial.SetShaderParameter("windup_top",.11f);legacyMaterial.SetShaderParameter("attack_width",1.12f);
        var legacy=new Sprite2D {Texture=atlas,Position=new Vector2(0,-44.8f),Scale=new Vector2(112f/atlas.GetWidth(),112f/atlas.GetHeight()),Material=legacyMaterial};legacyHolder.AddChild(legacy);
        var rigs=new PilgrimVisual[2];
        for(int i=0;i<2;i++)
        {
            var holder=new Node2D {Position=new Vector2(i==0?535:865,i==0?425:320),Scale=Vector2.One*(i==0?2.8f:1)};viewport.AddChild(holder);
            rigs[i]=new PilgrimVisual {Kind=EnemyKind.AshKnight,ProcessMode=ProcessModeEnum.Disabled};holder.AddChild(rigs[i]);
        }
        var state=new Label {Position=new Vector2(24,450)};state.AddThemeFontSizeOverride("font_size",22);viewport.AddChild(state);
        Label("Continuous body / connected limbs / restrained movement",new Vector2(24,500),17);
        Label("No slash VFX: the movement must read on its own",new Vector2(24,530),17);
        Label("Isolated production rig / 60fps / no gameplay simulation",new Vector2(24,600),15);
        string output=ProjectSettings.GlobalizePath("res://../.tools/knight-v3-preview");DirAccess.MakeDirRecursiveAbsolute(output);
        float gait=0;
        for(int frame=0;frame<540;frame++)
        {
            float t=frame/60f;
            var velocity=t>=.8f&&t<2.8f?Vector2.Right*78:Vector2.Zero;
            var facing=t>=4.3f&&t<6.5f?Vector2.FromAngle((t-4.3f)/2.2f*Mathf.Tau):Vector2.Right;
            if(t>=4.3f&&t<6.5f)velocity=facing*40;
            float wind=t>=2.8f&&t<3.45f?(t-2.8f)/.65f:0,release=t>=3.45f?t-3.45f:10;
            if(frame==402)foreach(var rig in rigs)rig.ReactToHit(new Vector2(-1,-.3f));
            float hurt=t>=6.7f&&t<6.83f?6.83f-t:0;
            if(frame==492)foreach(var rig in rigs)rig.Die();
            foreach(var rig in rigs)if(IsInstanceValid(rig)&&!rig.IsQueuedForDeletion()){rig.UpdatePose(velocity,facing,wind,release,hurt);rig.Advance(1f/60);}
            state.Text=t<.8f?"IDLE / BREATHING":t<2.8f?"WALK / WEIGHT TRANSFER":t<3.45f?"WINDUP / COMMIT":t<4.1f?"STRIKE / FOLLOW THROUGH":t<4.3f?"RECOVERY":t<6.5f?"TURN / FRONT TO REAR":t<7.1f?"DIRECTIONAL HIT REACTION":"IDLE";
            if(t>=8.2f)state.Text="DEATH / COLLAPSE";
            gait+=velocity.Length()/38/60;
            float a=velocity.Length()>0?1:0,b=velocity.Length()>0?2:0,blend=velocity.Length()>0?Mathf.SmoothStep(.2f,.8f,(Mathf.Sin(gait*Mathf.Pi)+1)*.5f):0;
            if(wind>0){a=0;b=3;blend=Mathf.SmoothStep(0,.32f,wind);}else if(release<.38f){a=4;b=0;blend=Mathf.SmoothStep(.15f,.38f,release);}
            if(hurt>0){a=b=5;blend=0;}
            if(t>=8.2f){a=b=5;blend=0;legacy.Modulate=new Color(1,1,1,Mathf.Max(0,1-(t-8.2f)/.48f));}
            legacyMaterial.SetShaderParameter("frame_a",a);legacyMaterial.SetShaderParameter("frame_b",b);legacyMaterial.SetShaderParameter("pose_blend",blend);legacyMaterial.SetShaderParameter("clock",t);
            legacyHolder.Scale=new Vector2(facing.X<0?-2.8f:2.8f,2.8f);
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
            using var image=viewport.GetTexture().GetImage();image.SavePng(output+$"/frame-{frame:000}.png");
        }
        viewport.QueueFree();await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);legacyMaterial.Dispose();GetTree().Quit();
    }
}
