using Godot;
namespace LastEmber;

// Continuous plasma sampled across an artist-shaped crescent. Reused per stroke.
public partial class FlameSlashRibbon : Node2D
{
    private ShaderMaterial _material=null!;
    private readonly Vector2[] _quad=new Vector2[4],_uv=new Vector2[4];
    private readonly Color[] _colors={Colors.White,Colors.White,Colors.White,Colors.White};
    private Vector2 _aim;
    private float _start,_head,_size,_width,_fade,_age,_seed,_charge;
    public override void _Ready()
    {
        _material=new ShaderMaterial {Shader=ResourceLoader.Load<Shader>("res://Assets/Shaders/flame_slash.gdshader")};
        Material=_material;Hide();
    }
    public void Configure(Vector2 origin,Vector2 aim,float start,float head,float size,float width,float fade,float age,float seed,float charge,bool blue,float power)
    {
        Position=origin;_aim=aim;_start=start;_head=head;_size=size;_width=width;
        _fade=fade;_age=age;_seed=seed;_charge=charge;
        Visible=fade>.005f&&Mathf.Abs(head-start)>.01f;
        _material.SetShaderParameter("effect_age",age);_material.SetShaderParameter("seed",seed);
        _material.SetShaderParameter("opacity",fade);_material.SetShaderParameter("power",Mathf.Lerp(.6f,1,power));
        _material.SetShaderParameter("blue_blend",blue?1f:0f);QueueRedraw();
    }
    private Vector2 Point(float t,float side)
    {
        float taper=Mathf.Pow(Mathf.Max(.015f,Mathf.Sin(t*Mathf.Pi)),.55f);
        float radius=(76+Mathf.Sin(t*7+_seed)*3)*_size;
        float width=_width*_size*taper*(1+_charge*.4f)*(.75f+.25f*_fade);
        float waviness=Mathf.Sin(t*23-_age*20+_seed)*width*.045f;
        return _aim.Rotated(Mathf.Lerp(_start,_head,t))*(radius+(side==0?width: -width*.62f)+waviness);
    }
    public override void _Draw()
    {
        for(int i=0;i<40;i++)
        {
            float a=i/40f,b=(i+1)/40f;
            _quad[0]=Point(a,0);_quad[1]=Point(b,0);_quad[2]=Point(b,1);_quad[3]=Point(a,1);
            _uv[0]=new Vector2(a,0);_uv[1]=new Vector2(b,0);_uv[2]=new Vector2(b,1);_uv[3]=new Vector2(a,1);
            DrawPolygon(_quad,_colors,_uv);
        }
    }
}
