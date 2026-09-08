using Godot;

namespace LastEmber;

public partial class FlameTransitionFx : Node2D
{
    public bool Blue {get;set;}
    private float _age;
    public override void _Ready()
    {Material=new CanvasItemMaterial {LightMode=CanvasItemMaterial.LightModeEnum.Unshaded};}
    public override void _Process(double delta)
    {_age+=(float)delta;if(_age>.32f)QueueFree();else QueueRedraw();}
    public override void _Draw()
    {
        float fade=Mathf.Clamp(1-_age/.32f,0,1);
        var color=FlamePalette.Fire(Blue);color.A=fade;
        DrawArc(Vector2.Zero,12+_age*310,0,Mathf.Tau,64,color,5*fade,true);
        var core=Blue?new Color(.8f,.97f,1):new Color(1,.96f,.7f);core.A=Mathf.Max(0,1-_age/.12f);
        DrawCircle(Vector2.Zero,24*fade,core);
        for(int i=0;i<18;i++)
        {
            var d=Vector2.FromAngle(i*Mathf.Tau/18);
            DrawLine(d*(15+_age*170),d*(25+_age*(240+i%4*15)),color,2*fade,true);
        }
    }
}
