using Godot;

namespace LastEmber;

public partial class Projectile : Node2D, IDungeonReactive
{
    public RunManager Run { get; set; } = null!;
    public Vector2 Velocity { get; set; }
    public float Damage { get; set; } = 10;
    public bool Fire {get;set;}
    public Color Tint {get;set;}=new(1,.45f,.32f);
    public void React(DungeonImpact impact,float seconds,Vector2 origin,bool blue)
    {
        if(Fire && impact==DungeonImpact.Suction && !blue){Hide();QueueFree();}
    }
    private float _life = 5;
    public override void _Ready() { ZIndex = 12; AddToGroup("dungeon_reactive"); }
    public override void _PhysicsProcess(double delta)
    {
        if (!Run.Playing || IsQueuedForDeletion()) return;
        var previous = Position;
        Position += Velocity * (float)delta; _life -= (float)delta;
        if (_life <= 0 || !Run.Room.HasLineOfSight(previous, Position) || !Run.Room.Bounds.HasPoint(Position)) { QueueFree(); return; }
        if (Geometry2D.GetClosestPointToSegment(Run.Player.Position, previous, Position).DistanceTo(Run.Player.Position) < 21)
        { Run.Player.TakeDamage(new DamageInfo(Damage, previous, 80)); QueueFree(); }
        QueueRedraw();
    }
    public override void _Draw()
    {
        DrawLine(-Velocity.Normalized()*20,Vector2.Zero,new Color(Tint,.5f),6,true);
        DrawCircle(Vector2.Zero,6,Tint); DrawCircle(Vector2.Zero,2,Colors.White);
    }
}
