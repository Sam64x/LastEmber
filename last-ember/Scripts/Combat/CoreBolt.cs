using System.Collections.Generic;
using Godot;
namespace LastEmber;

// Friendly projectile, deliberately separate from enemy Projectile and melee proc hooks.
public partial class CoreBolt : Node2D
{
    public RunManager Run { get; set; } = null!;
    public Vector2 Direction { get; set; } = Vector2.Right;
    public float Damage { get; set; }
    public int Targets { get; set; } = 1;
    public bool Blue { get; set; }
    public bool ArmorBreak { get; set; }
    public bool ChargeOrbit { get; set; }
    private float _remaining = 620;
    private readonly HashSet<ulong> _hit = new();
    public override void _Ready()
    {
        ZIndex = 28;
        Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };
    }
    public override void _PhysicsProcess(double delta)
    {
        if (!Run.Playing || IsQueuedForDeletion()) return;
        if (Run.Player.Dead) { QueueFree(); return; }
        var previous = Position;
        float travel = Mathf.Min(_remaining, 620 * (float)delta);
        var next = previous + Direction * travel;
        var candidates = Run.Enemies.ToArray();
        System.Array.Sort(candidates, (a, b) => previous.DistanceSquaredTo(a.Position).CompareTo(previous.DistanceSquaredTo(b.Position)));
        foreach (var enemy in candidates)
        {
            if (enemy.Dead || _hit.Contains(enemy.GetInstanceId())) continue;
            var contact = Geometry2D.GetClosestPointToSegment(enemy.Position, previous, next);
            if (contact.DistanceTo(enemy.Position) > enemy.BodyRadius + 6 || !Run.Room.HasLineOfSight(previous, contact) ||
                !Run.Room.HasLineOfSight(previous,enemy.Position)) continue;
            _hit.Add(enemy.GetInstanceId());
            if(ArmorBreak)enemy.BreakArmor();
            if(ChargeOrbit)Run.Player.Cores.OnBoltHit();
            enemy.TakeDamage(new DamageInfo(Damage, previous, 100));
            Run.Fx.FireImpact(contact, Direction, .4f, Blue,ImpactTraits.Projectile);
            if (--Targets <= 0) { QueueFree(); return; }
        }
        if (!Run.Room.HasLineOfSight(previous, next) || !Run.Room.Bounds.HasPoint(next)) { QueueFree(); return; }
        Position = next; _remaining -= travel;
        if (_remaining <= 0) QueueFree(); else QueueRedraw();
    }
    public override void _Draw()
    {
        var color = FlamePalette.Fire(Blue);
        if(ArmorBreak)DrawLine(-Direction*30,Direction*8,new Color(.9f,.95f,1,.65f),3,true);
        DrawLine(-Direction * 24, Vector2.Zero, new Color(color, .5f), 9, true);
        DrawCircle(Vector2.Zero, 6, color); DrawCircle(Vector2.Zero, 2.5f, Colors.White);
    }
}
