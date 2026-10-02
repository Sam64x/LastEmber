using Godot;
namespace LastEmber;

// Artwork only; collision radius, travel and hit selection belong to the owner.
public partial class EmberProjectileVisual : Node2D
{
    private static Texture2D? _atlas;
    private ShaderMaterial _material=null!;
    private float _age;
    public override void _Ready()
    {
        _atlas??=GD.Load<Texture2D>("res://Assets/Characters/ember-projectile-atlas-v1.png");
        _material=new ShaderMaterial {Shader=GD.Load<Shader>("res://Assets/Shaders/flame_slash.gdshader")};
        Material=_material;TextureFilter=TextureFilterEnum.Linear;
        _material.SetShaderParameter("loop_frames",true);
        // Register the measured hot-core centroid of each authored phase.
        _material.SetShaderParameter("frame_offset_0",new Vector2(.0302f,.1499f));
        _material.SetShaderParameter("frame_offset_1",new Vector2(-.0534f,.1495f));
        _material.SetShaderParameter("frame_offset_2",new Vector2(.0005f,-.0257f));
        _material.SetShaderParameter("frame_offset_3",new Vector2(-.0366f,-.0197f));
    }
    public void Configure(Vector2 direction,bool blue,bool lance=false,Color? tint=null,float opacity=1)
    {
        Rotation=direction.Angle();Scale=new Vector2(lance?1.35f:1,lance?1.1f:1);
        _material.SetShaderParameter("blue_blend",blue?1f:0f);
        _material.SetShaderParameter("color_tint",tint??Colors.White);
        _material.SetShaderParameter("opacity",opacity);
    }
    public void Seek(float age)
    {
        _age=age;
        _material.SetShaderParameter("phase",Mathf.PosMod(age*14,4));
        _material.SetShaderParameter("effect_age",age);
        _material.SetShaderParameter("power",1f+Mathf.Exp(-age*30)*.5f);
    }
    public override void _Process(double delta)=>Seek(_age+(float)delta);
    public override void _Draw()=>DrawTextureRect(_atlas!,new Rect2(-60,-22,80,44),false);
    public override void _ExitTree()=>_material.Dispose();
}
