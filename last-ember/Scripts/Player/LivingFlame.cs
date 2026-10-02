using Godot;
namespace LastEmber;

// GPU fire field runs at render cadence; its clock is supplied by the paused rig.
public partial class LivingFlame : Node2D
{
    private ShaderMaterial _flame=null!;
    public override void _Ready()
    {
        _flame=new ShaderMaterial {Shader=ResourceLoader.Load<Shader>("res://Assets/Shaders/living_ember.gdshader")};
        Material=_flame;
    }
    public void Update(float time,float blue,float life,Vector2 airflow,float surge)
    {
        _flame.SetShaderParameter("flame_time",time);
        _flame.SetShaderParameter("blue_blend",blue);
        _flame.SetShaderParameter("life",life);
        _flame.SetShaderParameter("airflow",airflow);
        _flame.SetShaderParameter("surge",surge);
    }
    public override void _Draw()=>DrawRect(new Rect2(-40,-58,80,86),Colors.White);
}
