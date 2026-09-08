using Godot;

namespace LastEmber;

public partial class EmberPickup : Node2D
{
    public RunManager Run {get;set;}=null!;
    public float Amount {get;set;}=4;
    private float _life=5;
    public override void _Ready(){ZIndex=5;}
    public override void _PhysicsProcess(double delta)
    {
        if(!Run.Playing)return;
        _life-=(float)delta;
        if(_life<=0){QueueFree();return;}
        if(Position.DistanceTo(Run.Player.Position)<29)
        {
            float before=Run.Player.Flame.Current;Run.Player.Flame.Heal(Amount);
            Run.Fx.Text(Position-new Vector2(0,20),$"+{Run.Player.Flame.Current-before:0} FLAME",new Color(1,.68f,.3f));
            QueueFree();return;
        }
        QueueRedraw();
    }
    public override void _Draw()
    {
        float fade=Mathf.Clamp(_life,0,1);
        DrawCircle(Vector2.Zero,9,new Color(.7f,.3f,.08f,fade));
        DrawColoredPolygon(new[]{new Vector2(-5,3),new Vector2(-2,-11),new Vector2(5,-3),new Vector2(4,5)},new Color(1,.68f,.24f,fade));
        DrawCircle(Vector2.Zero,3,new Color(1,.93f,.62f,fade));
    }
}
