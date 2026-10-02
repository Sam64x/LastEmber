using Godot;
namespace LastEmber;

// Illustrated fire silhouette, four reference-matched phases, render-frame blend.
public partial class FlameSlashRibbon : Node2D
{
    private static Texture2D? _atlas;
    private ShaderMaterial _material=null!;
    private float _size,_width,_charge;
    private bool _reverse;
    public override void _Ready()
    {
        _atlas??=ResourceLoader.Load<Texture2D>("res://Assets/Characters/slash-crescent-atlas-v2.png");
        _material=new ShaderMaterial {Shader=ResourceLoader.Load<Shader>("res://Assets/Shaders/flame_slash.gdshader")};
        Material=_material;TextureFilter=TextureFilterEnum.Linear;Hide();
    }
    public void Configure(Vector2 origin,Vector2 aim,float start,float head,float size,float width,float fade,float age,float seed,float charge,bool blue,float power,float lifeProgress=0)
    {
        Position=origin;_size=size;_width=width;_charge=charge;_reverse=head<start;
        Scale=new Vector2(1,_reverse?-1:1);
        Rotation=aim.Angle()+Mathf.Lerp(-.16f,.1f,lifeProgress)*(_reverse?-1:1);
        Visible=fade>.005f&&Mathf.Abs(head-start)>.01f;
        _material.SetShaderParameter("effect_age",age);_material.SetShaderParameter("seed",seed);
        _material.SetShaderParameter("opacity",fade);_material.SetShaderParameter("power",power);
        _material.SetShaderParameter("phase",Mathf.Clamp(lifeProgress*3,0,3));
        _material.SetShaderParameter("blue_blend",blue?1f:0f);QueueRedraw();
    }
    public override void _Draw()
    {
        float broad=Mathf.Lerp(1,1.18f,Mathf.Clamp((_width-19)/14,0,1))*(1+_charge*.08f);
        var rect=new Rect2(new Vector2(-34,-94)*_size,new Vector2(172*broad,188)*_size);
        DrawTextureRect(_atlas!,rect,false);
    }
}
