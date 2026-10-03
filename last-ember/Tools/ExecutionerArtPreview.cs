using Godot;
namespace LastEmber;

// Art blocking only. Loads source art outside res://; no enemy, player actor or combat scene.
public partial class ExecutionerArtPreview : Node2D
{
    private static readonly Rect2[] Regions =
    [new(18,0,421,459),new(471,0,409,449),new(881,0,409,449),new(1358,0,391,449),
     new(18,459,450,412),new(478,459,433,412),new(913,451,444,420),new(1358,451,391,420)];
    // Ground contact between the two boots, measured in source-sheet coordinates.
    private static readonly Vector2[] Anchors =
    [new(232,411),new(684,416),new(1110,420),new(1569,416),
     new(266,821),new(674,775),new(1100,811),new(1574,812)];
    private static readonly float[] Durations = [.08f,.18f,.22f,.12f,.05f,.07f,.13f,.28f];
    private static readonly string[] Names = ["GUARD","LOAD WEIGHT","LIFT","COMMIT","DOWNSWING","CONTACT","FOLLOW THROUGH","RECOVER"];

    public override async void _Ready()
    {
        var viewport=new SubViewport {Size=new Vector2I(1120,700),RenderTargetUpdateMode=SubViewport.UpdateMode.Always};AddChild(viewport);
        viewport.AddChild(new ColorRect {Size=new Vector2(1120,700),Color=new Color(.045f,.048f,.055f)});
        void Caption(string text,float x,float y,int size=18)
        {var label=new Label {Text=text,Position=new Vector2(x,y)};label.AddThemeFontSizeOverride("font_size",size);viewport.AddChild(label);}
        Caption("C / HEAVY EXECUTIONER",24,18,28);
        Caption("DESIGN + HERO / 3x",24,66);Caption("ATTACK KEY POSES / 3x",440,66);Caption("SCALE CHECK / 1x",850,66);
        using var source=Image.LoadFromFile(ProjectSettings.GlobalizePath("res://../art/executioner-master-v1.png"));
        using var sheet=Image.LoadFromFile(ProjectSettings.GlobalizePath("res://../art/executioner-attack-keys-v1.png"));
        source.GenerateMipmaps();sheet.GenerateMipmaps();
        var master=ImageTexture.CreateFromImage(source);var atlas=ImageTexture.CreateFromImage(sheet);
        Sprite2D Master(Vector2 ground,float zoom)
        {
            // Body (helmet to boot) is 90 world pixels; blade extends beyond the boots.
            var sprite=new Sprite2D {Texture=master,Centered=false,TextureFilter=TextureFilterEnum.LinearWithMipmaps,Position=ground-new Vector2(641,1065)*(.079f*zoom),Scale=Vector2.One*(.079f*zoom)};
            viewport.AddChild(sprite);return sprite;
        }
        Master(new Vector2(205,370),3);Master(new Vector2(958,275),1);
        var heroes=new PlayerVisual[2];
        for(int i=0;i<2;i++)
        {
            heroes[i]=new PlayerVisual {Position=i==0?new Vector2(365,352):new Vector2(1058,272),Scale=Vector2.One*(i==0?3f:1)};
            viewport.AddChild(heroes[i]);heroes[i].SetProcess(false);
        }
        Caption("Same relative scale in both comparisons",24,405,15);
        var pose=new Sprite2D {Texture=atlas,Centered=false,RegionEnabled=true,TextureFilter=TextureFilterEnum.LinearWithMipmaps};viewport.AddChild(pose);
        var small=new Sprite2D {Texture=atlas,Centered=false,RegionEnabled=true,TextureFilter=TextureFilterEnum.LinearWithMipmaps};viewport.AddChild(small);
        var state=new Label {Position=new Vector2(440,405)};state.AddThemeFontSizeOverride("font_size",19);viewport.AddChild(state);
        for(int i=0;i<8;i++)
        {
            var thumbnail=new Sprite2D {Texture=atlas,Centered=false,RegionEnabled=true,TextureFilter=TextureFilterEnum.LinearWithMipmaps,RegionRect=Regions[i],Scale=Vector2.One*.21f,
                Position=new Vector2(70+i*139,579)-(Anchors[i]-Regions[i].Position)*.21f};viewport.AddChild(thumbnail);
            Caption($"{i+1:00} / {Names[i]}",13+i*139,609,11);
        }
        Caption("8 authored key poses / timing study / intermediate frames and rear views still required",24,655,17);
        string output=ProjectSettings.GlobalizePath("res://../.tools/executioner-preview");DirAccess.MakeDirRecursiveAbsolute(output);
        for(int frame=0;frame<180;frame++)
        {
            float clock=frame/60f%1.8f;int index=0;float elapsed=0;
            if(clock>=.55f)
            {
                float action=clock-.55f;index=7;
                for(int i=0;i<Durations.Length;i++){elapsed+=Durations[i];if(action<elapsed){index=i;break;}}
            }
            void Pose(Sprite2D sprite,Vector2 ground,float zoom)
            {sprite.RegionRect=Regions[index];sprite.Scale=Vector2.One*(.225f*zoom);sprite.Position=ground-(Anchors[index]-Regions[index].Position)*(.225f*zoom);}
            Pose(pose,new Vector2(622,362),3);Pose(small,new Vector2(958,420),1);
            state.Text=$"{index+1:00} / {Names[index]}";
            foreach(var hero in heroes)hero.Advance(1f/60,HeroVisualInput.Idle);
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
            using var image=viewport.GetTexture().GetImage();image.SavePng(output+$"/frame-{frame:000}.png");
        }
        viewport.QueueFree();await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);GetTree().Quit();
    }
}
