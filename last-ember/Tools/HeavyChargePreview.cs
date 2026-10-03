using Godot;
namespace LastEmber;
public partial class HeavyChargePreview : Node2D
{
    public override async void _Ready()
    {
        var viewport=new SubViewport {Size=new Vector2I(960,430),RenderTargetUpdateMode=SubViewport.UpdateMode.Always};AddChild(viewport);
        viewport.AddChild(new ColorRect {Size=new Vector2(960,430),Color=new Color(.035f,.043f,.055f)});
        var effects=new FireStrikeFx[2];
        var heroes=new PlayerVisual[2];
        for(int i=0;i<2;i++)
        {
            viewport.AddChild(new Label {Text=i==0?"HEAVY CORE / WARM":"HEAVY CORE / LAST EMBER",Position=new Vector2(40+i*480,30)});
            var hero=new PlayerVisual {Position=new Vector2(155+i*480,265),Scale=Vector2.One*2};viewport.AddChild(hero);
            heroes[i]=hero;
            hero.Advance(1f/60,HeroVisualInput.Idle with {Aim=Vector2.Right,Blue=i==1});
            effects[i]=new FireStrikeFx {Position=hero.Position,Scale=Vector2.One*2,Blue=i==1,ProcessMode=ProcessModeEnum.Disabled};viewport.AddChild(effects[i]);
        }
        viewport.AddChild(new Label {Text="Gather > full charge > release / isolated production visuals / 60fps",Position=new Vector2(40,390)});
        string output=ProjectSettings.GlobalizePath("res://../.tools/charge-preview");DirAccess.MakeDirRecursiveAbsolute(output);
        for(int frame=0;frame<180;frame++)
        {
            float t=frame/60f;
            foreach(var effect in effects)
            {
                if(t<2.25f){effect.ShowCharge(Vector2.Right,Mathf.Min(t/1.1f,1));effect._Process(1.0/60);}
                else if(frame==135)
                {
                    var style=new StrikeStyleData {StartAngle=-1.3f,EndAngle=1.3f,Width=30,SweepSeconds=.13f,TailSeconds=.26f};
                    effect.Begin(Vector2.Right,1,style,1,.07f,1,1);
                }
                if(t>=2.25f)effect.SeekPreview(t-2.25f);
            }
            for(int i=0;i<2;i++)heroes[i].Advance(1f/60,HeroVisualInput.Idle with {Aim=Vector2.Right,Blue=i==1,StrikeOffset=effects[i].BodyOffset,StrikeRotation=effects[i].BodyRotation,StrikeScale=effects[i].BodyScale});
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
            using var image=viewport.GetTexture().GetImage();image.SavePng(output+$"/frame-{frame:000}.png");
        }
        viewport.QueueFree();await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);GetTree().Quit();
    }
}
