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
    private EmberProjectileVisual _visual=null!;
    public override void _Ready()
    {
        ZIndex=12;AddToGroup("dungeon_reactive");
        _visual=new EmberProjectileVisual();AddChild(_visual);
        _visual.Configure(Velocity,false,tint:Tint);
    }
    public override void _PhysicsProcess(double delta)
    {
        if (!Run.Playing || IsQueuedForDeletion()) return;
        var previous = Position;
        Position += Velocity * (float)delta; _life -= (float)delta;
        if (_life <= 0 || !Run.Room.HasLineOfSight(previous, Position) || !Run.Room.Bounds.HasPoint(Position)) { QueueFree(); return; }
        if (Geometry2D.GetClosestPointToSegment(Run.Player.Position, previous, Position).DistanceTo(Run.Player.Position) < 21)
        { Run.Player.TakeDamage(new DamageInfo(Damage, previous, 80)); QueueFree(); }
        _visual.Configure(Velocity,false,tint:Tint,opacity:Mathf.Clamp(_life/.1f,0,1));
    }
}
