using Godot;

namespace LastEmber;

public partial class Player : CharacterBody2D, IDamageable
{
    [Export] public float MoveSpeed { get; set; } = 220;
    [Export] public float MeleeDamage { get; set; } = 20;
    [Export] public float BurstDamage { get; set; } = 48;
    [Export] public float DashDuration { get; set; } = .18f;
    [Export] public float RevealCost {get;set;}=5;
    [Export] public float RevealCooldownSeconds {get;set;}=10;
    [Export] public float RevealRadius {get;set;}=1900;
    [Export] public float RevealExpandSeconds {get;set;}=.6f;
    [Export] public float RevealHoldSeconds {get;set;}=.9f;
    [Export] public float RevealFadeSeconds {get;set;}=2f;
    public FlamePool Flame { get; } = new();
    public BuildStats Build { get; } = new();
    public EmberLight Light { get; private set; } = null!;
    public EmberLight RevealLight {get;private set;}=null!;
    public RunManager Run { get; set; } = null!;
    public bool Dead => Flame.Dead;
    public bool Dashing => _dashTime > 0;
    public float DashCooldown { get; private set; }
    public float BurstCooldown { get; private set; }
    public float RevealCooldown {get;private set;}
    public bool Revealing {get;private set;}
    private float _revealTime,_revealStart;
    public Vector2 Aim { get; set; } = Vector2.Right;
    public Vector2 TestMovement { get; set; }
    public bool Automated { get; set; }
    private bool _waitForMouseRelease;
    private float _dashTime, _invulnerable, _attackCooldown, _swing, _trailClock;
    private Vector2 _dashDirection, _knockback;
    private int _meleeHits;
    private float _lastFlame=100, _stepClock;

    public override void _Ready()
    {
        CollisionLayer = 2; CollisionMask = 1;
        AddChild(new CollisionShape2D { Shape = new CircleShape2D { Radius = 15 } });
        Light = new EmberLight(); AddChild(Light);
        var listener = new AudioListener2D(); AddChild(listener); listener.MakeCurrent();
        RevealLight=new EmberLight {Name="RevealLight",Lit=false,Tint=new Color(1,.85f,.63f),Intensity=.95f};AddChild(RevealLight);
        Flame.Emptied += () => Run.EndRun(false);
        Flame.Changed+=OnFlameChanged;
        ZIndex = 8;
    }
    public override void _PhysicsProcess(double delta)
    {
        if (Dead || !Run.Playing) return;
        var dt = (float)delta;
        DashCooldown = Mathf.Max(0, DashCooldown - dt);
        BurstCooldown = Mathf.Max(0, BurstCooldown - dt);
        RevealCooldown=Mathf.Max(0,RevealCooldown-dt);
        _invulnerable = Mathf.Max(0, _invulnerable - dt);
        _attackCooldown -= dt; _swing = Mathf.Max(0, _swing - dt);
        var input = Automated ? TestMovement.LimitLength() : Input.GetVector("left", "right", "up", "down");
        if (!Automated)
        {
            var aim = GetGlobalMousePosition() - GlobalPosition;
            if (aim.LengthSquared() > 1) Aim = aim.Normalized();
            if (Input.IsActionJustPressed("dash")) TryDash(input);
            if (Input.IsActionJustPressed("reveal")) TryReveal();
            if(_waitForMouseRelease)_waitForMouseRelease=Input.IsActionPressed("melee");
            else
            {
                if (Input.IsActionPressed("melee")) TryMelee();
            }
        }
        if (Dashing)
        {
            _dashTime -= dt;
            Velocity = _dashDirection * 850;
            _trailClock -= dt;
            if (_trailClock <= 0)
            {
                _trailClock = .025f;
                Run.Fx.Ghost(Position);
                if (Build.Has(ArtifactEffect.DashTrail)) Run.AddFire(Position);
            }
        }
        else Velocity = input * MoveSpeed * Build.SpeedMultiplier(Flame) + _knockback;
        _knockback = _knockback.MoveToward(Vector2.Zero, dt * 750);
        MoveAndSlide();
        _stepClock-=dt;
        if(!Dashing&&Velocity.LengthSquared()>1000&&_stepClock<=0){_stepClock=.42f;Run.Audio.PlayAt("step",Position,.65f);}
        var bounds=Run.Room.Bounds.Grow(-16);
        Position = new Vector2(Mathf.Clamp(Position.X, bounds.Position.X, bounds.End.X), Mathf.Clamp(Position.Y, bounds.Position.Y, bounds.End.Y));
        Light.TargetRadius = FlameLight.Radius(Flame.Current,Build.LightMultiplier);
        UpdateReveal(dt);
        QueueRedraw();
    }
    public bool TryDash(Vector2 direction)
    {
        if (Dead || !Run.Playing || DashCooldown > 0) return false;
        DashCooldown = 1; _dashTime = DashDuration; _invulnerable = .2f;
        _dashDirection = direction.LengthSquared() > .01f ? direction.Normalized() : Aim;
        Run.BreakTethers();
        if (Build.DashExplosion) Run.Explode(Position, 115, 26, false);
        Run.Audio.Play("dash");
        return true;
    }
    public void SuppressUiClick()=>_waitForMouseRelease=true;
    public bool TryReveal()
    {
        if(Dead||!Run.Playing||RevealCooldown>0||!Flame.Spend(RevealCost))return false;
        RevealCooldown=RevealCooldownSeconds;Revealing=true;_revealTime=0;_revealStart=Light.Radius;
        RevealLight.ResetRadius(_revealStart);RevealLight.Energy=RevealLight.Intensity;RevealLight.Lit=true;
        if(!Run.Lights.Contains(RevealLight))Run.Lights.Add(RevealLight);
        foreach(var enemy in Run.Enemies) enemy.HearReveal();
        Run.Audio.Play("reveal");Run.Fx.Sparks(Position,new Color(1,.85f,.55f),14);
        return true;
    }
    private void UpdateReveal(float dt)
    {
        if(!Revealing)return;
        _revealTime+=dt;
        float fadeStart=RevealExpandSeconds+RevealHoldSeconds;
        if(_revealTime<RevealExpandSeconds)
            RevealLight.ResetRadius(Mathf.Lerp(_revealStart,RevealRadius,Mathf.SmoothStep(0,1,_revealTime/RevealExpandSeconds)));
        else if(_revealTime<fadeStart)RevealLight.ResetRadius(RevealRadius);
        else
        {
            float fade=Mathf.SmoothStep(0,1,Mathf.Clamp((_revealTime-fadeStart)/RevealFadeSeconds,0,1));
            RevealLight.ResetRadius(Mathf.Lerp(RevealRadius,Light.TargetRadius,fade));
            RevealLight.Energy=RevealLight.Intensity*(1-fade);
            if(_revealTime>=fadeStart+RevealFadeSeconds)ResetForRoom();
        }
    }
    public void ResetForRoom()
    {
        Revealing=false;_revealTime=0;RevealLight.Lit=false;RevealLight.Energy=0;
        Run.Lights.Remove(RevealLight);
    }
    private void OnFlameChanged()
    {
        if(Flame.Current>_lastFlame+.5f)Run.Audio.Play("ignite");
        else if(Flame.Current<_lastFlame&&Flame.Ratio<.1f)Run.Audio.Play("ember_loss");
        _lastFlame=Flame.Current;
    }
    public bool TryMelee()
    {
        if (Dead || !Run.Playing || _attackCooldown > 0) return false;
        _attackCooldown = .5f / Build.AttackSpeed; _swing = .22f;
        Run.Audio.Play("swing");
        // Snapshot protects iteration against chained Kindling explosions/removals.
        foreach (var enemy in Run.Enemies.ToArray())
        {
            if (enemy.Dead || !Combat.InArc(Position, Aim, enemy.Position, 104 + enemy.BodyRadius)) continue;
            if (!Run.Room.HasLineOfSight(Position, enemy.Position)) continue;
            _meleeHits++;
            var burn = Build.Has(ArtifactEffect.ThirdHitBurn) && _meleeHits % 3 == 0;
            enemy.TakeDamage(new DamageInfo(MeleeDamage * Build.DamageMultiplier(Flame), Position, 180, burn));
        }
        return true;
    }
    public bool TryBurst()
    {
        // Retained for older development tools; Burst is disabled in this MVP.
        return false;
    }
    public void TakeDamage(DamageInfo hit)
    {
        if (Dead || !Run.Playing || _invulnerable > 0) return;
        _invulnerable = .65f;
        _knockback = (Position - hit.Origin).Normalized() * hit.Knockback;
        Flame.Damage(hit.Amount);
        Run.Fx.Sparks(Position, new Color(1, .3f, .1f), 14);
        Run.Fx.Text(Position - new Vector2(0, 28), $"−{hit.Amount:0}", new Color(1, .4f, .25f));
        Run.Shake(hit.Amount >= 12 ? 9 : 4); Run.Audio.Play("hurt");
    }
    public override void _Draw()
    {
        if(Revealing&&_revealTime<1.1f)
            DrawArc(Vector2.Zero,Mathf.Max(20,RevealLight.Radius*.88f),0,Mathf.Tau,128,new Color(1,.86f,.56f,.65f*(1-_revealTime/1.1f)),4,true);
        DrawCircle(new Vector2(0, 13), 21, new Color(0, 0, 0, .65f));
        var white = _invulnerable > .2f && ((int)(_invulnerable * 25) % 2 == 0);
        var c = white ? Colors.White : new Color(1, .56f, .19f);
        DrawColoredPolygon(new[] { new Vector2(-15, 12), new Vector2(-11, -12), new Vector2(0, -24), new Vector2(12, -10), new Vector2(17, 14), new Vector2(0, 21) }, new Color(.25f, .13f, .1f));
        DrawColoredPolygon(new[] { new Vector2(-9, 7), new Vector2(-7, -7), new Vector2(-1, -20), new Vector2(4, -8), new Vector2(10, -3), new Vector2(7, 10), new Vector2(0, 14) }, c);
        DrawCircle(new Vector2(0, 0), 5, new Color(1, .92f, .67f));
        DrawLine(Aim * 18, Aim * 37, new Color(.95f, .87f, .70f), 4, true);
        if (_swing > 0)
        {
            float angle = Aim.Angle(), progress = 1 - _swing / .22f;
            DrawArc(Vector2.Zero, 88, angle - 1.2f + progress * .5f, angle + 1.1f, 28, new Color(1, .76f, .35f, _swing / .22f), 8, true);
            DrawLine(Vector2.FromAngle(angle - 1.2f + progress * 2.4f) * 25, Vector2.FromAngle(angle - 1.2f + progress * 2.4f) * 101, Colors.White, 3, true);
        }
    }
}
