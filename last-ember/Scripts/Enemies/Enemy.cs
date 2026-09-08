using Godot;

namespace LastEmber;

public enum EnemyKind { Ashling, Moth, Shade, Watcher, Leech, Torchbearer, Boss }

public partial class Enemy : CharacterBody2D, IDamageable
{
    [Export] public EnemyKind Kind { get; set; }
    [Export] public float MaxHealth { get; set; } = 40;
    [Export] public float ContactDamage { get; set; } = 8;
    public RunManager Run { get; set; } = null!;
    public float Health { get; protected set; }
    public bool Dead { get; protected set; }
    public float BodyRadius { get; protected set; } = 18;
    public bool Active { get; protected set; }
    public bool Tethered { get; private set; }
    public EmberLight? Light { get; private set; }
    public BurnStatus Burn { get; } = new();
    public float SpeedNow { get; protected set; }
    protected float Flash, Clock, Cooldown = .8f, Telegraph;
    protected Vector2 Knockback, Target;
    private float _drain, _tetherGrace;
    private float _pathClock;
    private Vector2 _waypoint;
    private float _stepClock;
    private Line2D? _tether;
    public bool FrozenForTest { get; set; }
    public bool WindingUp => Telegraph > 0;

    public override void _Ready()
    {
        CollisionLayer = 4; CollisionMask = 1;
        if (Kind == EnemyKind.Moth) { MaxHealth = 25; BodyRadius = 14; }
        if (Kind == EnemyKind.Shade) MaxHealth = 60;
        if (Kind == EnemyKind.Watcher) MaxHealth = 45;
        if (Kind == EnemyKind.Leech) MaxHealth = 35;
        if (Kind == EnemyKind.Torchbearer) { MaxHealth = 340; BodyRadius = 32; ContactDamage = 15; }
        Health = MaxHealth;
        AddChild(new CollisionShape2D { Shape = new CircleShape2D { Radius = BodyRadius } });
        if (Kind == EnemyKind.Torchbearer)
        {
            Light = new EmberLight { TargetRadius = 310 };
            AddChild(Light); Run.Lights.Add(Light);
        }
        if (Kind == EnemyKind.Leech)
        {
            _tether = new Line2D { Width = 3, DefaultColor = new Color(.94f, .27f, .26f), ZIndex = -1 };
            _tether.AddPoint(Vector2.Zero);_tether.AddPoint(Vector2.Zero);
            AddChild(_tether);
        }
        ZIndex = 7;
    }
    public override void _PhysicsProcess(double delta)
    {
        if (Dead || !Run.Playing) return;
        float dt = (float)delta;
        Clock += dt; Flash = Mathf.Max(0, Flash - dt); Cooldown -= dt; _tetherGrace -= dt;
        if (Burn.Tick(dt)) TakeDamage(new DamageInfo(5, Position, 0, IsBurnTick:true));
        if (Dead) return;
        if (!FrozenForTest) Behave(dt);
        QueueRedraw();
    }
    protected virtual void Behave(float dt)
    {
        var player = Run.Player;
        var offset = player.Position - Position;
        float distance = offset.Length();
        bool lit = Run.IsLit(Position);
        if (Kind == EnemyKind.Moth || Kind == EnemyKind.Watcher)
        {
            if (lit || distance < 105) Active = true;
        }
        else Active = true;
        SpeedNow = Kind switch
        {
            EnemyKind.Moth => 180,
            EnemyKind.Shade => lit ? 72 : player.Flame.Ratio < .3f ? 185 : 105,
            EnemyKind.Watcher => 88,
            EnemyKind.Leech => 135,
            EnemyKind.Torchbearer => 72,
            _ => 112
        };
        Vector2 direction = Active ? offset.Normalized() : Vector2.Zero;
        if (Kind == EnemyKind.Watcher && Active)
        {
            direction *= distance < 255 ? -1 : distance < 360 ? 0 : 1;
            if (Telegraph > 0)
            {
                Telegraph -= dt; direction = Vector2.Zero;
                if (Telegraph <= 0) { Run.Shoot(Position, Target, 260, 10); Cooldown = 2.1f; }
            }
            else if (Cooldown <= 0 && Run.Room.HasLineOfSight(Position, player.Position))
            { Target = player.Position; Telegraph = .65f; Run.Audio.Play("warning"); }
        }
        else if (Kind == EnemyKind.Leech)
        {
            if (Tethered)
            {
                direction = Vector2.Zero;
                if (distance > 260 || player.Dashing || !Run.Room.HasLineOfSight(Position, player.Position)) BreakTether();
                else
                {
                    _drain -= dt;
                    if (_drain <= 0) { _drain = .8f; player.TakeDamage(new DamageInfo(2, Position, 0));Run.Audio.PlayAt("siphon",Position,.5f); }
                }
            }
            else if (distance < 75 && _tetherGrace <= 0 && !player.Dashing && Run.Room.HasLineOfSight(Position, player.Position))
            { Tethered = true; _drain = .5f; }
            if (_tether != null) { _tether.Visible = Tethered; _tether.SetPointPosition(1,offset); }
        }
        else if (Active)
        {
            if (Telegraph > 0)
            {
                direction = Vector2.Zero; Telegraph -= dt;
                if (Telegraph <= 0)
                {
                    float reach = Kind == EnemyKind.Torchbearer ? 112 : 65;
                    if (distance < reach && Run.Room.HasLineOfSight(Position, player.Position)) player.TakeDamage(new DamageInfo(ContactDamage, Position));
                    if (Kind == EnemyKind.Torchbearer) Run.Fx.Ring(Position, reach, new Color(1, .48f, .1f));
                    Cooldown = Kind == EnemyKind.Shade && player.Flame.Ratio < .3f ? .65f : 1.2f;
                }
            }
            else if (distance < BodyRadius + 48 && Cooldown <= 0) { Telegraph = Kind == EnemyKind.Torchbearer ? .65f : .4f; }
        }
        if (direction != Vector2.Zero)
        {
            _pathClock-=dt;
            if(direction.Dot(offset.Normalized())>.7f&&!Run.Room.HasLineOfSight(Position,player.Position))
            {
                if(_pathClock<=0||Position.DistanceTo(_waypoint)<22)
                { _pathClock=.4f;_waypoint=Run.Room.Navigate(Position,player.Position); }
                direction=(_waypoint-Position).Normalized();
            }
            direction = Run.Room.Steer(Position, direction, BodyRadius);
        }
        // Local separation keeps groups readable without expensive physics queries.
        Vector2 separation = Vector2.Zero;
        foreach (var other in Run.Enemies)
        {
            if (other == this || other.Dead) continue;
            var away = Position - other.Position;
            float d2 = away.LengthSquared(), minimum = BodyRadius + other.BodyRadius + 6;
            if (d2 > .01f && d2 < minimum * minimum) separation += away.Normalized() * 55;
        }
        Velocity = direction * SpeedNow + Knockback + separation;
        Knockback = Knockback.MoveToward(Vector2.Zero, dt * 700);
        MoveAndSlide();
        _stepClock-=dt;
        if(Velocity.LengthSquared()>900&&_stepClock<=0)
        {
            _stepClock=Kind==EnemyKind.Moth?1.8f:Kind==EnemyKind.Torchbearer?.85f:.7f;
            Run.Audio.PlayAt(Kind==EnemyKind.Moth?"moth":Kind==EnemyKind.Torchbearer?"heavy_step":"step",Position,Kind==EnemyKind.Torchbearer?.9f:.35f);
        }
        Position = new Vector2(Mathf.Clamp(Position.X, 120, 1800), Mathf.Clamp(Position.Y, 184, 926));
    }
    public void BreakTether()
    {
        Tethered = false; _tetherGrace = 1.1f;
        if (_tether != null) _tether.Visible = false;
    }
    public virtual void TakeDamage(DamageInfo hit)
    {
        if (Dead) return;
        if (hit.Burn) Burn.Apply();
        Health = Mathf.Max(0, Health - hit.Amount);
        Flash = .13f; Active = true;
        Knockback = (Position - hit.Origin).Normalized() * hit.Knockback;
        Run.Fx.Sparks(Position, new Color(1, .62f, .26f), 7);
        Run.Fx.Text(Position - new Vector2(0, BodyRadius + 12), $"{hit.Amount:0}", new Color(1, .87f, .65f));
        Run.Audio.Play("hit");
        if (Health > 0) return;
        Dead = true;
        if (Light != null) { Light.Lit = false; Run.Lights.Remove(Light); }
        BreakTether();
        Run.OnEnemyKilled(this, Burn.Active || hit.IsBurnTick);
        QueueFree();
    }
    public override void _Draw()
    {
        float r = BodyRadius;
        DrawCircle(new Vector2(0, r * .6f), r + 4, new Color(0, 0, 0, .65f));
        var color = Flash > 0 ? Colors.White : Kind switch
        {
            EnemyKind.Moth => new Color(.72f, .69f, .68f),
            EnemyKind.Shade => new Color(.43f, .36f, .61f),
            EnemyKind.Watcher => new Color(.70f, .34f, .35f),
            EnemyKind.Leech => new Color(.57f, .25f, .37f),
            EnemyKind.Torchbearer => new Color(.71f, .40f, .17f),
            _ => new Color(.50f, .47f, .43f)
        };
        if (Kind == EnemyKind.Moth)
        {
            float wing = 15 + Mathf.Sin(Clock * 18) * 6;
            DrawColoredPolygon(new[] { new Vector2(0,-6), new Vector2(-wing,-18),new Vector2(-wing,13),Vector2.Zero,new Vector2(wing,13),new Vector2(wing,-18) }, color);
            DrawCircle(Vector2.Zero, 6, new Color(.2f,.16f,.19f));
        }
        else if (Kind == EnemyKind.Watcher)
        {
            DrawColoredPolygon(new[] { new Vector2(-r,0),new Vector2(0,-r),new Vector2(r,0),new Vector2(0,r) }, color);
            DrawArc(Vector2.Zero, r + 7, 0, Mathf.Tau, 24, new Color(.5f,.22f,.22f), 2);
            DrawCircle(Vector2.Zero, 6, Active ? new Color(1,.38f,.25f) : new Color(.24f,.19f,.21f));
            if (Telegraph > 0) DrawLine(Vector2.Zero, Target - Position, new Color(1,.2f,.15f,.5f), 2, true);
        }
        else if (Kind == EnemyKind.Leech)
        {
            for (int i = 3; i >= 0; i--) DrawCircle(new Vector2(-i * 8, Mathf.Sin(Clock * 8 + i) * 5), 11 - i, color.Darkened(i * .13f));
            DrawCircle(new Vector2(4, -3), 3, new Color(1,.5f,.45f));
        }
        else
        {
            DrawColoredPolygon(new[] { new Vector2(-r,r), new Vector2(-r*.7f,-r*.8f),new Vector2(-r*.35f,-r*1.3f),new Vector2(0,-r),new Vector2(r*.6f,-r*1.15f),new Vector2(r,r),new Vector2(0,r*.7f) }, color);
            DrawLine(new Vector2(-7,-5),new Vector2(-2,-4),new Color(1,.42f,.2f),3);
            DrawLine(new Vector2(3,-4),new Vector2(8,-5),new Color(1,.42f,.2f),3);
            if (Kind == EnemyKind.Torchbearer)
            { DrawLine(new Vector2(30,20),new Vector2(36,-43),new Color(.6f,.39f,.22f),5); DrawCircle(new Vector2(36,-45),11,new Color(1,.6f,.15f)); }
        }
        if (Telegraph > 0 && Kind != EnemyKind.Watcher) DrawArc(Vector2.Zero, r + 16, 0, Mathf.Tau, 32, new Color(1,.27f,.16f,.9f), 3);
        if (Burn.Active) { DrawCircle(new Vector2(-r,-r*.3f),4,new Color(1,.45f,.08f)); DrawCircle(new Vector2(r,-r*.6f),5,new Color(1,.7f,.14f)); }
        if (Health < MaxHealth)
        {
            DrawRect(new Rect2(-r, -r-18, r*2, 4), new Color(.12f,.08f,.10f));
            DrawRect(new Rect2(-r, -r-18, r*2*Health/MaxHealth, 4), new Color(.9f,.36f,.22f));
        }
    }
}
