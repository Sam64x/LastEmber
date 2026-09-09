using Godot;

namespace LastEmber;

public enum EnemyKind { Ashling, Moth, Shade, Watcher, Leech, Torchbearer, Boss, Stalker, IceGuard, FireWisp }

public partial class Enemy : CharacterBody2D, IDamageable, IDungeonReactive
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
    private bool _heardReveal;
    private float _stalkTime;
    public bool IceArmored { get; set; }
    public bool FireAligned { get; set; }
    public float Weakened { get; private set; }
    public void React(DungeonImpact impact,float seconds,Vector2 origin,bool blue)
    {
        if(Dead)return;
        if(impact==DungeonImpact.Heat && IceArmored){IceArmored=false;Run.Fx.Sparks(Position,new Color(.65f,.9f,1),15);}
        if(impact==DungeonImpact.Suction && FireAligned)Weakened=Mathf.Max(Weakened,seconds);
        if(impact==DungeonImpact.Blast)TakeDamage(new DamageInfo(FireAligned && Weakened>0?18:10,origin,300));
    }
    public bool FrozenForTest { get; set; }
    public bool WindingUp => Telegraph > 0;

    public override void _Ready()
    {
        AddToGroup("dungeon_reactive");IceArmored|=Kind==EnemyKind.IceGuard;FireAligned|=Kind==EnemyKind.FireWisp;
        CollisionLayer = 4; CollisionMask = 1;
        if (Kind == EnemyKind.Moth) { MaxHealth = 25; BodyRadius = 14; }
        if (Kind == EnemyKind.Shade) MaxHealth = 60;
        if (Kind == EnemyKind.Stalker) { MaxHealth = 45; ContactDamage = 10; }
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
        _stepClock = Mathf.PosMod(Position.X * .017f + Position.Y * .013f, 1.6f);
    }
    public void HearReveal()
    {
        if(Kind != EnemyKind.Moth || Dead) return;
        _heardReveal=true; Active=true;
        Run.Audio.PlayAt("moth",Position,.8f);
    }
    public override void _PhysicsProcess(double delta)
    {
        if (Dead || !Run.Playing) return;
        float dt = (float)delta;
        Clock += dt; Flash = Mathf.Max(0, Flash - dt); Cooldown -= dt; _tetherGrace -= dt;
        if (Burn.Tick(dt)) TakeDamage(new DamageInfo(5*Run.Player.Flame.LastEmberDamageMultiplier, Position, 0, IsBurnTick:true));
        if (Dead) return;
        Weakened=Mathf.Max(0,Weakened-dt);
        if (!FrozenForTest) Behave(dt);
        QueueRedraw();
    }
    protected virtual void Behave(float dt)
    {
        var player = Run.Player;
        var offset = player.Position - Position;
        float distance = offset.Length();
        bool lit = Run.IsLit(Position);
        if (Kind == EnemyKind.Moth) Active = _heardReveal || lit;
        else if (Kind == EnemyKind.Watcher)
        {
            if(lit && !Active) { Active=true; Cooldown=0; }
            if(!lit && Telegraph<=0) Active=false;
        }
        else Active = true;
        SpeedNow = Kind switch
        {
            EnemyKind.Moth => 180,
            EnemyKind.Shade => 120 * (lit ? .6f : 1.4f),
            EnemyKind.Stalker => 145,
            EnemyKind.Watcher => 0,
            EnemyKind.Leech => 135,
            EnemyKind.Torchbearer => 72,
            _ => 112
        };
        if(Weakened>0)SpeedNow*=.4f;
        if(FireAligned && Cooldown<=0 && Weakened<=0){Run.Shoot(Position,player.Position,240,8,true);Cooldown=2;}
        Vector2 direction = Active ? offset.Normalized() : Vector2.Zero;
        if (Kind == EnemyKind.Watcher && Active)
        {
            direction = Vector2.Zero;
            if (Telegraph > 0)
            {
                Telegraph -= dt; direction = Vector2.Zero;
                if (Telegraph <= 0) { Run.Shoot(Position, Target, 300, 10); Run.Audio.PlayAt("watcher_shot",Position,.9f); Cooldown = 2.1f; }
            }
            else if (Cooldown <= 0 && Run.Room.HasLineOfSight(Position, player.Position))
            { Target = player.Position; Telegraph = 1f; Run.Audio.PlayAt("warning",Position,1); }
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
                    float damage=ContactDamage*(Weakened>0?.4f:1)*(Kind==EnemyKind.Shade&&lit?.8f:1);
                    if (distance < reach && Run.Room.HasLineOfSight(Position, player.Position)) player.TakeDamage(new DamageInfo(damage, Position));
                    if (Kind == EnemyKind.Torchbearer) Run.Fx.Ring(Position, reach, new Color(1, .48f, .1f));
                    Cooldown = 1.2f;
                }
            }
            else if (distance < BodyRadius + 48 && Cooldown <= 0) { Telegraph = Kind == EnemyKind.Torchbearer ? .65f : .4f; }
        }
        if(Kind==EnemyKind.Stalker && Telegraph<=0)
        {
            _stalkTime+=dt;
            float edge=player.Light.Radius+42;
            // The pulse exposes it in place; it follows the ordinary Flame boundary.
            float radial=distance>edge+22?1:distance<edge-18?-1:0;
            bool exposedByPulse=player.Revealing&&player.RevealLight.Contains(Position);
            bool lunge=player.Flame.Current<=20&&!exposedByPulse&&Mathf.PosMod(_stalkTime,6)>4.8f;
            direction=lunge?offset.Normalized():offset.Normalized()*radial+offset.Normalized().Orthogonal()*.32f;
            if(lit&&!exposedByPulse&&!lunge)direction=-offset.Normalized();
            direction=direction.LimitLength();
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
        Velocity = Kind==EnemyKind.Watcher?Vector2.Zero:direction * SpeedNow + Knockback + separation;
        Knockback = Knockback.MoveToward(Vector2.Zero, dt * 700);
        MoveAndSlide();
        _stepClock-=dt;
        if(Velocity.LengthSquared()>900&&_stepClock<=0)
        {
            _stepClock=Kind==EnemyKind.Stalker?1.35f:Kind==EnemyKind.Moth?1.8f:Kind==EnemyKind.Torchbearer?.85f:.7f;
            Run.Audio.PlayAt(Kind==EnemyKind.Stalker?"stalker_step":Kind==EnemyKind.Moth?"moth":Kind==EnemyKind.Torchbearer?"heavy_step":"step",Position,Kind==EnemyKind.Stalker?.95f:Kind==EnemyKind.Torchbearer?.9f:.5f);
        }
        var bounds=Run.Room.Bounds.Grow(-BodyRadius);
        Position = new Vector2(Mathf.Clamp(Position.X,bounds.Position.X,bounds.End.X), Mathf.Clamp(Position.Y,bounds.Position.Y,bounds.End.Y));
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
        Health = Mathf.Max(0, Health - hit.Amount*(IceArmored?.3f:1)*(FireAligned&&Weakened>0?1.3f:1));
        Flash = .13f; Active = true;
        Knockback = (Position - hit.Origin).Normalized() * hit.Knockback;
        Run.Fx.Sparks(Position, FlamePalette.Fire(Run.Player.Flame.LastEmber), 7);
        Run.Fx.Text(Position - new Vector2(0, BodyRadius + 12), $"{hit.Amount:0}", new Color(1, .87f, .65f));
        if(!hit.Strike)Run.Audio.Play("hit");
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
        if(IceArmored)DrawArc(Vector2.Zero,r+8,0,Mathf.Tau,12,new Color(.6f,.9f,1),5);
        if(FireAligned)DrawArc(Vector2.Zero,r+8,0,Mathf.Tau,32,Weakened>0?new Color(.6f,.9f,1):new Color(1,.3f,.05f),4);
        DrawCircle(new Vector2(0, r * .6f), r + 4, new Color(0, 0, 0, .65f));
        var color = Flash > 0 ? Colors.White : Kind switch
        {
            EnemyKind.Moth => new Color(.72f, .69f, .68f),
            EnemyKind.Shade => new Color(.43f, .36f, .61f),
            EnemyKind.Stalker => new Color(.32f,.39f,.42f),
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
        if (Burn.Active) { DrawCircle(new Vector2(-r,-r*.3f),4,FlamePalette.Fire(Run.Player.Flame.LastEmber)); DrawCircle(new Vector2(r,-r*.6f),5,FlamePalette.Shift(new Color(1,.7f,.14f),Run.Player.Flame.LastEmber)); }
        if (Health < MaxHealth)
        {
            DrawRect(new Rect2(-r, -r-18, r*2, 4), new Color(.12f,.08f,.10f));
            DrawRect(new Rect2(-r, -r-18, r*2*Health/MaxHealth, 4), new Color(.9f,.36f,.22f));
        }
    }
}
