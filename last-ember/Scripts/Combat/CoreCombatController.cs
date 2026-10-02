using System.Collections.Generic;
using Godot;
namespace LastEmber;

// Additive Cores share input and damage scaling, but never recurse into melee on-hit effects.
public partial class CoreCombatController : Node2D
{
    public Player Player { get; set; } = null!;
    private RunManager Run => Player.Run;
    private BuildState Build => Player.Progression;
    private float Damage => Player.MeleeDamage * Player.Build.DamageMultiplier(Player.Flame);
    private float _angle, _clock, _trailClock;
    public float OrbitSurge { get; private set; }
    public float OrbitHaste { get; private set; }
    private bool _dashActive;
    private Vector2 _dashPrevious;
    private readonly HashSet<ulong> _dashHit = new();
    private readonly Dictionary<(int Orb, ulong Enemy), float> _orbitHits = new();
    private readonly List<Vector2> _previousOrbits = new();
    private readonly List<(int Orb, ulong Enemy)> _expired = new();
    private int OrbitCount => 2 + Mathf.Clamp(Mathf.RoundToInt(Build.Value(MeleeEffectKind.OrbitCount)), 0, 4);
    private const float OrbitRadius = 88;
    public override void _Ready()
    {
        ZIndex = 25;
        Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };
        Build.Changed += Reset;
    }
    public override void _ExitTree() => Build.Changed -= Reset;
    public void OnStrike(Vector2 aim,int step)
    {
        if (!Build.Has(MeleeEffectKind.BoltCore)) return;
        int count = Build.Has(MeleeEffectKind.BoltVolley) ? 3 : 1;
        bool lance=step==3 && Build.Has(MeleeEffectKind.EmberLance);
        for (int i = 0; i < count; i++)
        {
            var direction = aim.Rotated((i - (count - 1) * .5f) * .2f);
            FireBolt(direction,(count>1?.7f:1)*(lance?1.5f:1),lance);
        }
        Run.Audio.PlayAt("watcher_shot", Player.Position, .6f);
    }
    private void FireBolt(Vector2 direction,float scale,bool armorBreak=false)
    {
        Run.Room.AddChild(new CoreBolt
        {
            Run=Run,Position=Player.Position,Direction=direction,Damage=Damage*.65f*scale,
            Targets=1+Mathf.Clamp(Mathf.RoundToInt(Build.Value(MeleeEffectKind.BoltPierce)),0,5),
            Blue=Player.Flame.LastEmber,ArmorBreak=armorBreak,ChargeOrbit=Build.Has(MeleeEffectKind.StellarConduit)
        });
    }
    public void OnBoltHit()
    {
        if(!Build.Has(MeleeEffectKind.StellarConduit)||!Build.Has(MeleeEffectKind.OrbitCore))return;
        if(OrbitSurge<=0)Run.Fx.Ring(Player.Position,OrbitRadius,new Color(.8f,.7f,1));
        OrbitSurge=4;
    }
    public void OnDash()
    {
        if (!Build.Has(MeleeEffectKind.DashCore)) return;
        _dashActive = true; _dashPrevious = Player.Position; _dashHit.Clear(); _trailClock = 0;
        if(Build.Has(MeleeEffectKind.MeteorStep))OrbitHaste=3;
    }
    // Called after movement so sweep damage uses this frame's real, collision-limited path.
    public void Tick(float dt)
    {
        if (!Run.Playing || Player.Dead) return;
        _clock += dt;
        OrbitSurge=Mathf.Max(0,OrbitSurge-dt);OrbitHaste=Mathf.Max(0,OrbitHaste-dt);
        if (Build.Has(MeleeEffectKind.OrbitCore)) TickOrbits(dt);
        if (_dashActive) TickDash(dt);
        QueueRedraw();
    }
    private void TickOrbits(float dt)
    {
        _angle = Mathf.PosMod(_angle + dt * 2.4f * (1 + Build.Value(MeleeEffectKind.OrbitSpeed))*(OrbitHaste>0?2:1), Mathf.Tau);
        _expired.Clear();
        foreach (var pair in _orbitHits) if (pair.Value <= _clock) _expired.Add(pair.Key);
        foreach (var key in _expired) _orbitHits.Remove(key);
        bool first = _previousOrbits.Count != OrbitCount;
        if (first) { _previousOrbits.Clear(); for (int i = 0; i < OrbitCount; i++) _previousOrbits.Add(OrbPosition(i)); }
        for (int i = 0; i < OrbitCount; i++)
        {
            var point = OrbPosition(i); var previous = _previousOrbits[i]; _previousOrbits[i] = point;
            foreach (var enemy in Run.Enemies.ToArray())
            {
                var key = (i, enemy.GetInstanceId());
                if (enemy.Dead || _orbitHits.ContainsKey(key)) continue;
                var contact = Geometry2D.GetClosestPointToSegment(enemy.Position, previous, point);
                if (contact.DistanceTo(enemy.Position) > enemy.BodyRadius + 9 ||
                    !Run.Room.HasLineOfSight(Player.Position, enemy.Position) || !Run.Room.HasLineOfSight(previous, contact)) continue;
                _orbitHits[key] = _clock + .65f;
                enemy.TakeDamage(new DamageInfo(Damage * .4f*(OrbitSurge>0?1.5f:1), Player.Position, 45));
                Run.Fx.Sparks(contact, FlamePalette.Fire(Player.Flame.LastEmber), 3);
            }
        }
    }
    private Vector2 OrbPosition(int i) => Player.Position + Vector2.FromAngle(_angle + i * Mathf.Tau / OrbitCount) * OrbitRadius;
    private void TickDash(float dt)
    {
        var current = Player.Position;
        bool finished = !Player.Dashing;
        float radius = 105 * (1 + Build.Value(MeleeEffectKind.DashImpact));
        foreach (var enemy in Run.Enemies.ToArray())
        {
            if (enemy.Dead || _dashHit.Contains(enemy.GetInstanceId())) continue;
            var contact = Geometry2D.GetClosestPointToSegment(enemy.Position, _dashPrevious, current);
            bool crossed = contact.DistanceTo(enemy.Position) < 32 + enemy.BodyRadius &&
                Run.Room.HasLineOfSight(_dashPrevious, contact) && Run.Room.HasLineOfSight(contact, enemy.Position);
            bool landing = finished && current.DistanceTo(enemy.Position) < radius + enemy.BodyRadius && Run.Room.HasLineOfSight(current, enemy.Position);
            if (!crossed && !landing) continue;
            _dashHit.Add(enemy.GetInstanceId());
            enemy.TakeDamage(new DamageInfo(Damage * (1 + Build.Value(MeleeEffectKind.DashImpact)), current, 300));
            Run.Fx.FireImpact(contact, (enemy.Position - current).Normalized(), .6f, Player.Flame.LastEmber);
        }
        _trailClock -= dt;
        if (Build.Has(MeleeEffectKind.DashTrail) && _trailClock <= 0)
        {
            _trailClock = .05f; Run.AddFire(current);
        }
        _dashPrevious = current;
        if (finished)
        {
            _dashActive = false; _dashHit.Clear();
            Run.Fx.Ring(current, radius, FlamePalette.Fire(Player.Flame.LastEmber));
            Run.Audio.Play("burst"); Run.Shake(3);
            if(Build.Has(MeleeEffectKind.CinderCrossfire))
                for(int i=0;i<8;i++)FireBolt(Vector2.FromAngle(i*Mathf.Tau/8),.65f);
        }
    }
    public void Reset()
    {
        _dashActive = false; _dashHit.Clear(); _orbitHits.Clear(); _previousOrbits.Clear(); _angle = 0; _clock = 0;
        OrbitSurge=OrbitHaste=0;
        if (GodotObject.IsInstanceValid(Run.Room))
            foreach (var child in Run.Room.GetChildren()) if (child is CoreBolt bolt) { bolt.Hide(); bolt.QueueFree(); }
        QueueRedraw();
    }
    public override void _Draw()
    {
        if (Player.Dead || !Build.Has(MeleeEffectKind.OrbitCore)) return;
        var color = FlamePalette.Fire(Player.Flame.LastEmber);
        for (int i = 0; i < OrbitCount; i++)
        {
            float angle = _angle + i * Mathf.Tau / OrbitCount;
            var point = Vector2.FromAngle(angle) * OrbitRadius;
            DrawArc(Vector2.Zero, OrbitRadius, angle - .3f, angle, 12, new Color(color, .4f), 4, true);
            DrawCircle(point, OrbitSurge>0?12:9, color); DrawCircle(point, 3, Colors.White);
            if(OrbitHaste>0)DrawArc(point,14,0,Mathf.Tau,16,new Color(color,.65f),2,true);
        }
    }
}
