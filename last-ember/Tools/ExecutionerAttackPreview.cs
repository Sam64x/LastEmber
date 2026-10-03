using Godot;
namespace LastEmber;

// Isolated animation review. Deliberately samples discrete drawings without crossfade or mesh warping.
public partial class ExecutionerAttackPreview : Node2D
{
    private static readonly float[] Durations=[.04f,.055f,.065f,.08f,.08f,.08f,.09f,.085f,.04f,.035f,.04f,.06f,.075f,.09f,.10f,.10f];
    private static readonly Rect2[] OldRegions=
    [new(18,0,421,459),new(471,0,409,449),new(881,0,409,449),new(1358,0,391,449),
     new(18,459,450,412),new(478,459,433,412),new(913,451,444,420),new(1358,451,391,420)];
    private static readonly Vector2[] OldAnchors=
    [new(232,411),new(684,416),new(1110,420),new(1569,416),new(266,821),new(674,775),new(1100,811),new(1574,812)];
    private static readonly float[] OldDurations=[.08f,.18f,.22f,.12f,.05f,.07f,.13f,.28f];
    // Source cells do not share a perfect baseline. Translation-only registration from boot contacts;
    // never normalize individual frame height, which would hide changing anatomy with scale pops.
    private static readonly Vector2[] Anchors=
    [new(174,258),new(172.5f,258),new(170,258),new(170,258),
     new(176.5f,262),new(175.5f,264.5f),new(171.5f,261),new(170,263),
     new(169.5f,238.5f),new(164.5f,240),new(142.5f,237),new(137,238.5f),
     new(177.5f,209),new(175,213.5f),new(163.5f,218),new(160.5f,216.5f)];

    private static int Sample(float time,float[] durations)
    {
        if(time<0)return 0;
        for(int i=0;i<durations.Length;i++){if(time<durations[i])return i;time-=durations[i];}
        return 0;
    }
    public override async void _Ready()
    {
        var viewport=new SubViewport {Size=new Vector2I(1120,650),RenderTargetUpdateMode=SubViewport.UpdateMode.Always};AddChild(viewport);
        viewport.AddChild(new ColorRect {Size=new Vector2(1120,650),Color=new Color(.045f,.048f,.055f)});
        Label Caption(string text,float x,float y,int size=18)
        {var label=new Label {Text=text,Position=new Vector2(x,y)};label.AddThemeFontSizeOverride("font_size",size);viewport.AddChild(label);return label;}
        Caption("EXECUTIONER / ATTACK CONTINUITY",24,18,26);
        Caption("V1 / 8 KEY POSES",30,68);Caption("V2 / 16 DRAWINGS",433,68);Caption("SCALE / 1x",870,68);
        ImageTexture Load(string name)
        {using var image=Image.LoadFromFile(ProjectSettings.GlobalizePath("res://../art/"+name));image.GenerateMipmaps();return ImageTexture.CreateFromImage(image);}
        var oldTexture=Load("executioner-attack-keys-v1.png");var texture=Load("executioner-attack-v2.png");
        var swingTexture=Load("executioner-swing-v2.png");
        Sprite2D Sprite(Texture2D tex)
        {var s=new Sprite2D {Texture=tex,Centered=false,RegionEnabled=true,RegionFilterClipEnabled=true,TextureFilter=TextureFilterEnum.LinearWithMipmaps};viewport.AddChild(s);return s;}
        var old=Sprite(oldTexture);var current=Sprite(texture);var small=Sprite(texture);
        Vector2 cell=new(texture.GetWidth()/4f,texture.GetHeight()/4f);
        var hero=new PlayerVisual {Position=new Vector2(1050,280)};viewport.AddChild(hero);hero.SetProcess(false);
        var status=Caption("",433,432,20);
        Caption("Same strike timing / contact at 650 ms",24,480,18);
        Caption("Source drawings only / no crossfade / no whole-body stretching",24,514,17);
        Caption("Art review only / no game actors or combat simulation",24,610,15);
        var bars=new ColorRect[16];
        for(int i=0;i<16;i++)
        {bars[i]=new ColorRect {Position=new Vector2(24+i*67,556),Size=new Vector2(59,14)};viewport.AddChild(bars[i]);Caption((i+1).ToString("00"),24+i*67,577,11);}
        void Ground(float x,float y,float width)
        {viewport.AddChild(new Line2D {Points=[new(x-width/2,y),new(x+width/2,y)],Width=1,DefaultColor=new Color(.24f,.26f,.29f)});}
        Ground(227,387,285);Ground(622,387,285);Ground(955,279,95);
        string output=ProjectSettings.GlobalizePath("res://../.tools/executioner-v2-preview");DirAccess.MakeDirRecursiveAbsolute(output);
        for(int frame=0;frame<108;frame++)
        {
            float action=frame/60f-.4f;
            int index=Sample(action,Durations),previous=Sample(action,OldDurations);
            old.RegionRect=OldRegions[previous];old.Scale=Vector2.One*.675f;
            old.Position=new Vector2(227,387)-(OldAnchors[previous]-OldRegions[previous].Position)*.675f;
            var region=new Rect2(new Vector2(index%4,index/4)*cell,cell);
            void Pose(Sprite2D s,Vector2 ground,float zoom)
            {
                bool swing=index is 8 or 9;
                s.Texture=swing?swingTexture:texture;
                s.RegionRect=swing?new Rect2((index-8)*887,0,887,887):region;
                s.Scale=Vector2.One*(swing?.205f:.46f)*zoom;
                var anchor=swing?(index==8?new Vector2(516,700.5f):new Vector2(358.5f,694)):Anchors[index];
                s.Position=ground-anchor*s.Scale;
            }
            Pose(current,new Vector2(622,387),3);Pose(small,new Vector2(955,279),1);
            status.Text=$"FRAME {index+1:00} / "+(index<2?"GUARD":index<8?"WINDUP":index<10?"SWING":index<12?"CONTACT":"RECOVERY");
            for(int i=0;i<bars.Length;i++)bars[i].Color=i==index?new Color(1,.48f,.15f):new Color(.22f,.23f,.26f);
            hero.Advance(1f/60,HeroVisualInput.Idle);
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
            using var image=viewport.GetTexture().GetImage();image.SavePng(output+$"/frame-{frame:000}.png");
        }
        viewport.QueueFree();await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);GetTree().Quit();
    }
}
