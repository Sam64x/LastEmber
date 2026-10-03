using Godot;
namespace LastEmber;

// Progress comes from Heavy Core; the full-charge loop never changes combat state.
public partial class HeavyChargeFx : Node2D
{
    private static Texture2D? _atlas;
    private ShaderMaterial _material=null!;
    public override void _Ready()
    {
        _atlas??=GD.Load<Texture2D>("res://Assets/Characters/heavy-charge-atlas-v1.png");
        _material=new ShaderMaterial {Shader=GD.Load<Shader>("res://Assets/Shaders/flame_slash.gdshader")};
        Material=_material;TextureFilter=TextureFilterEnum.Linear;Hide();
    }
    public void UpdateCharge(Vector2 aim,float ratio,float age,float fullAge,bool blue)
    {
        ratio=Mathf.Clamp(ratio,0,1);
        bool ready=ratio>=1;
        float pulse=ready?Mathf.Sin(fullAge*8)*.035f:0;
        float flash=ready?Mathf.Exp(-fullAge*18)*.12f:0;
        Position=aim.Normalized()*Mathf.Lerp(29,37,ratio);
        Rotation=aim.Angle()+age*.55f;
        Scale=Vector2.One*(Mathf.Lerp(.72f,1,ratio)+pulse+flash);
        _material.SetShaderParameter("phase",ratio*3);
        _material.SetShaderParameter("effect_age",age);
        _material.SetShaderParameter("blue_blend",blue?1f:0f);
        _material.SetShaderParameter("opacity",Mathf.Lerp(.55f,.95f,ratio)*Mathf.SmoothStep(0,.06f,age));
        _material.SetShaderParameter("power",1+flash*4);
        Show();
    }
    public override void _Draw()=>DrawTextureRect(_atlas!,new Rect2(-36,-36,72,72),false);
    public override void _ExitTree()=>_material.Dispose();
}
