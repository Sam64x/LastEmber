using Godot;

namespace LastEmber;

public partial class Projectile : Node2D
{
    public RunManager Run { get; set; } = null!;
    public Vector2 Velocity { get; set; }
    public float Damage { get; set; } = 10;
    private float _life = 5;
    public override void _Ready() { ZIndex = 12; }
    public override void _PhysicsProcess(double delta)
    {
        if (!Run.Playing) return;
        var previous = Position;
        Position += Velocity * (float)delta; _life -= (float)delta;
        if (_life <= 0 || !Run.Room.HasLineOfSight(previous, Position) || !Room.Interior.HasPoint(Position)) { QueueFree(); return; }
        if (Geometry2D.GetClosestPointToSegment(Run.Player.Position, previous, Position).DistanceTo(Run.Player.Position) < 21)
        { Run.Player.TakeDamage(new DamageInfo(Damage, previous, 80)); QueueFree(); }
        QueueRedraw();
    }
    public override void _Draw()
    {
        DrawLine(-Velocity.Normalized()*20,Vector2.Zero,new Color(.9f,.2f,.2f,.5f),6,true);
        DrawCircle(Vector2.Zero,6,new Color(1,.45f,.32f)); DrawCircle(Vector2.Zero,2,Colors.White);
    }
}
