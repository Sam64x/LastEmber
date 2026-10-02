using Godot;
namespace LastEmber;

// Fixed target sockets, explicit wind-up and recovery. Attacks never track during wind-up.
public partial class ElementalBoss : Enemy
{
    [Export] public bool Frost { get; set; }
    public int Phase { get; private set; } = 1;
    public int AttackIndex { get; private set; } = -1;
    public float Warning { get; private set; }
    public override bool WindingUp => Warning > 0;
    public string AttackName => Frost
        ? (AttackIndex % 3) switch { 0 => "ICEFALL", 1 => "SHARD FAN", _ => "FROZEN RING" }
        : (AttackIndex % 3) switch { 0 => "CINDER SALVO", 1 => "FIRE FAN", _ => "ERUPTION" };
    private float _recovery = 2, _windup;
    private bool _interrupted;
    private GroundAttackTelegraph _markings = null!;
    private Color ElementTint => Frost ? new Color(.55f, .85f, 1) : new Color(1, .43f, .16f);

    public override void _Ready()
    {
        Kind = EnemyKind.Boss; MaxHealth = 950; BodyRadius = 44; ContactDamage = 14;
        IceArmored = Frost; FireAligned = !Frost; base._Ready();
        _markings = new GroundAttackTelegraph { Tint = ElementTint }; AddChild(_markings);
    }
    protected override void Behave(float dt)
    {
        Active = true; Velocity = Vector2.Zero;
        if (Weakened > 0)
        {
            if (!_interrupted)
            {
                InterruptAttack();
            }
            return;
        }
        _interrupted = false;
        if (Warning > 0)
        {
            Warning = Mathf.Max(0, Warning - dt);
            _markings.Progress = 1 - Warning / _windup; _markings.QueueRedraw();
            if (Warning <= 0) ResolveAttack();
            return;
        }
        _recovery -= dt;
        if (_recovery <= 0) BeginAttack();
    }
    public override void React(DungeonImpact impact, float seconds, Vector2 origin, bool blue)
    {
        base.React(impact, seconds, origin, blue);
        // Cancel immediately even when stagger prevents Behave from ticking.
        if (!Dead && impact == DungeonImpact.Suction && Weakened > 0)
        {
            if (!_interrupted) InterruptAttack();
            OpenWeakPoint(Weakened);
        }
    }
    private void InterruptAttack()
    {
        _interrupted = true; Warning = 0; _markings.Clear(); _recovery = 1.4f;
        OpenWeakPoint(Weakened);
        Run.Fx.Text(Position - new Vector2(0, 70), "INTERRUPTED", new Color(.6f, .9f, 1));
    }
    public void BeginAttack()
    {
        if (Dead || Weakened > 0 || Warning > 0) return;
        AttackIndex++; Target = Run.Player.Position;
        _markings.Clear(); _markings.ShotOrigin = Position;
        int move = AttackIndex % 3;
        _windup = Phase == 3 ? .85f : Phase == 2 ? 1.05f : 1.2f;
        if ((Frost && move == 0) || (!Frost && move == 2))
        {
            float radius = Frost ? 105 : 90;
            _markings.Areas.Add(new(Target, radius));
            // Later phases add flank sockets, fixed before the warning begins.
            if (Phase >= 2)
            {
                var side = (Target - Position).Normalized().Orthogonal();
                if (side == Vector2.Zero) side = Vector2.Up;
                AddArea(Target + side * (radius * 2 + 35), radius);
                AddArea(Target - side * (radius * 2 + 35), radius);
            }
        }
        else if (Frost && move == 2)
            _markings.Areas.Add(new(Position, Phase == 3 ? 290 : 245, 100));
        else if (move == 1)
        {
            int count = Phase == 3 ? 7 : 5;
            float aim = (Target - Position).Angle();
            for (int i = 0; i < count; i++)
                _markings.ShotDirections.Add(Vector2.FromAngle(aim + (i - (count - 1) * .5f) * .19f));
        }
        else
        {
            // Two opposite gaps remain traversable in every phase.
            int count = Phase == 3 ? 20 : 16;
            float angle = (Target - Position).Angle();
            for (int i = 0; i < count; i++)
                if (i != 0 && i != count / 2)
                    _markings.ShotDirections.Add(Vector2.FromAngle(angle + i * Mathf.Tau / count));
        }
        Warning = _windup; _markings.Show(); _markings.QueueRedraw();
        Run.Audio.Play("warning");
    }
    private void AddArea(Vector2 center, float radius)
    {
        var safe = Run.Room.Bounds.Grow(-radius);
        center = new Vector2(Mathf.Clamp(center.X, safe.Position.X, safe.End.X), Mathf.Clamp(center.Y, safe.Position.Y, safe.End.Y));
        _markings.Areas.Add(new(center, radius));
    }
    private void ResolveAttack()
    {
        int move = AttackIndex % 3;
        bool hit = false;
        foreach (var area in _markings.Areas)
        {
            Run.Fx.Ring(area.Center, area.Radius, ElementTint);
            hit |= area.Contains(Run.Player.Position) && Run.Room.HasLineOfSight(area.Center, Run.Player.Position);
        }
        // Overlapping sockets deal damage once per attack.
        if (hit) Run.Player.TakeDamage(new DamageInfo(ContactDamage * (Frost && IceArmored ? 1.3f : 1), Target, 160));
        foreach (var direction in _markings.ShotDirections)
            Run.Shoot(Position, Position + direction, Phase == 3 ? 275 : 235, ContactDamage * .7f, !Frost, ElementTint);
        if (Frost && move == 0) IceArmored = true;
        Run.Audio.Play(Frost ? "ice_crack" : "backdraft_blast");
        if (_markings.Areas.Count > 0) Run.Shake(4);
        _markings.Clear(); OpenWeakPoint(1.25f);
        _recovery = Phase == 3 ? 1.65f : Phase == 2 ? 2.2f : 2.8f;
    }
    public override void TakeDamage(DamageInfo hit)
    {
        base.TakeDamage(hit with { Knockback = 0 });
        if (Dead) { _markings.Clear(); return; }
        int next = Health / MaxHealth <= .3f ? 3 : Health / MaxHealth <= .65f ? 2 : 1;
        if (next == Phase) return;
        Phase = next; Warning = 0; _markings.Clear(); _recovery = 1.8f;
        OpenWeakPoint(1.8f); Run.Fx.Ring(Position, 210, ElementTint);
        Run.Hud.Toast(Frost ? $"RIME WARDEN • PHASE {Phase} • Q BREAKS ARMOR" : $"CINDER HEART • PHASE {Phase} • Q INTERRUPTS FIRE");
    }
    public override void _Draw()
    {
        base._Draw();
        var tint = Flash > 0 ? Colors.White : ElementTint;
        DrawArc(Vector2.Zero, BodyRadius + 15, Clock * .25f, Clock * .25f + Mathf.Tau * .8f, 48, tint, 3, true);
        for (int i = 0; i < Phase; i++) DrawCircle(new Vector2((i - (Phase - 1) * .5f) * 14, -65), 4, tint);
    }
}
