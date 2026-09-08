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
    public float RevealCooldown => Flame.LastEmber?_bluePulseCooldown:_normalRevealCooldown;
    public bool BluePulseActive {get;private set;}
    private float _bluePulseCooldown,_normalRevealCooldown;
    private float _pulseRadius,_pulseExpand,_pulseHold,_pulseFade,_coldClock;
    private float _blueBlend;
    public bool Revealing {get;private set;}
    private float _revealTime,_revealStart;
    public Vector2 Aim { get; set; } = Vector2.Right;
    public Vector2 TestMovement { get; set; }
    public bool Automated { get; set; }
    private bool _waitForMouseRelease;
    private float _dashTime, _invulnerable, _attackCooldown, _trailClock;
    private float _strikeWindup, _strikeStrength, _strikeLightTime;
    private Vector2 _strikeAim;
    private FireStrikeFx _strikeFx=null!;
    private EmberLight _strikeLight=null!;
    private Vector2 _dashDirection, _knockback;
    private int _meleeHits;
    private float _lastFlame=100, _stepClock;

    public override void _Ready()
    {
        CollisionLayer = 2; CollisionMask = 1;
        AddChild(new CollisionShape2D { Shape = new CircleShape2D { Radius = 15 } });
        Light = new EmberLight(); AddChild(Light);
        _strikeFx=new FireStrikeFx();AddChild(_strikeFx);
        _strikeLight=new EmberLight {Name="StrikeLight",Lit=false,Tint=new Color(1,.73f,.36f)};
        AddChild(_strikeLight);
        var listener = new AudioListener2D(); AddChild(listener); listener.MakeCurrent();
        RevealLight=new EmberLight {Name="RevealLight",Lit=false,Tint=new Color(1,.85f,.63f),Intensity=.95f};AddChild(RevealLight);
        Flame.Emptied += () => Run.EndRun(false);
        Flame.Changed+=OnFlameChanged;
        Flame.LastEmberChanged+=OnLastEmberChanged;
        Flame.DeathPrevented+=()=>_invulnerable=Mathf.Max(_invulnerable,.5f);
        ZIndex = 8;
    }
    public override void _PhysicsProcess(double delta)
    {
        if (Dead || !Run.Playing) return;
        var dt = (float)delta;
        DashCooldown = Mathf.Max(0, DashCooldown - dt);
        BurstCooldown = Mathf.Max(0, BurstCooldown - dt);
        _normalRevealCooldown=Mathf.Max(0,_normalRevealCooldown-dt);
        _bluePulseCooldown=Mathf.Max(0,_bluePulseCooldown-dt);
        _blueBlend=Mathf.Lerp(_blueBlend,Flame.LastEmber?1:0,1-Mathf.Exp(-dt*12));
        Light.Tint=FlamePalette.Light(Flame.LastEmber);
        RevealLight.Tint=FlamePalette.Light(Flame.LastEmber);
        _strikeLight.Tint=FlamePalette.Light(Flame.LastEmber);
        _strikeFx.Blue=Flame.LastEmber;
        _coldClock-=dt;
        if(Flame.LastEmber&&_coldClock<=0){_coldClock=.65f;Run.Audio.Play("blue_crackle");}
        _invulnerable = Mathf.Max(0, _invulnerable - dt);
        _attackCooldown -= dt;
        UpdateStrike(dt);
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
                Run.Fx.Ghost(Position,Flame.LastEmber);
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
        if(Dead||!Run.Playing||RevealCooldown>0)return false;
        if(!Flame.LastEmber&&Flame.Current-RevealCost<=FlamePool.LastEmberThreshold&&_bluePulseCooldown>0)return false;
        if(!Flame.LastEmber&&!Flame.Spend(RevealCost))return false;
        BluePulseActive=Flame.LastEmber;
        if(BluePulseActive)_bluePulseCooldown=3;else _normalRevealCooldown=RevealCooldownSeconds;
        _pulseRadius=RevealRadius*(BluePulseActive?.4f:1);
        _pulseExpand=BluePulseActive?.18f:RevealExpandSeconds;
        _pulseHold=BluePulseActive?.22f:RevealHoldSeconds;
        _pulseFade=BluePulseActive?.6f:RevealFadeSeconds;
        Revealing=true;_revealTime=0;_revealStart=Light.Radius;
        RevealLight.ResetRadius(_revealStart);RevealLight.Energy=RevealLight.Intensity;RevealLight.Lit=true;
        if(!Run.Lights.Contains(RevealLight))Run.Lights.Add(RevealLight);
        foreach(var enemy in Run.Enemies) enemy.HearReveal();
        Run.Audio.Play(BluePulseActive?"blue_pulse":"reveal");Run.Fx.Sparks(Position,FlamePalette.Fire(Flame.LastEmber),14);
        return true;
    }
    private void UpdateReveal(float dt)
    {
        if(!Revealing)return;
        _revealTime+=dt;
        float fadeStart=_pulseExpand+_pulseHold;
        if(_revealTime<_pulseExpand)
            RevealLight.ResetRadius(Mathf.Lerp(_revealStart,_pulseRadius,Mathf.SmoothStep(0,1,_revealTime/_pulseExpand)));
        else if(_revealTime<fadeStart)RevealLight.ResetRadius(_pulseRadius);
        else
        {
            float fade=Mathf.SmoothStep(0,1,Mathf.Clamp((_revealTime-fadeStart)/_pulseFade,0,1));
            RevealLight.ResetRadius(Mathf.Lerp(_pulseRadius,Light.TargetRadius,fade));
            RevealLight.Energy=RevealLight.Intensity*(1-fade);
            if(_revealTime>=fadeStart+_pulseFade)CancelReveal();
        }
    }
    public void ResetForRoom()
    {
        _strikeWindup=0;_strikeLightTime=0;_strikeFx.Clear();_strikeLight.Lit=false;
        Run.Lights.Remove(_strikeLight);
        CancelReveal();
    }
    private void CancelReveal()
    {
        Revealing=false;_revealTime=0;RevealLight.Lit=false;RevealLight.Energy=0;
        BluePulseActive=false;
        Run.Lights.Remove(RevealLight);
    }
    private void OnLastEmberChanged(bool blue)
    {
        CancelReveal();
        Light.Tint=RevealLight.Tint=_strikeLight.Tint=FlamePalette.Light(blue);
        _strikeFx.Blue=blue;_coldClock=.2f;
        Run.Fx.FlameTransition(Position,blue);
        Run.Audio.Play(blue?"last_ember":"ember_return");
        if(Run.Playing){Run.Shake(3);Run.HitStop(.05f);}
        Run.Hud.Toast(blue?"LAST EMBER • ONE LAST CHANCE • Q BLUE PULSE":"FLAME RESTORED • THE LAST CHANCE IS RENEWED");
    }
    private void OnFlameChanged()
    {
        if(Flame.Current>_lastFlame+.5f)Run.Audio.Play("ignite");
        else if(Flame.Current<_lastFlame&&Flame.Ratio<.1f)Run.Audio.Play(Flame.LastEmber?"blue_crackle":"ember_loss");
        _lastFlame=Flame.Current;
    }
    public bool TryMelee()
    {
        if (Dead || !Run.Playing || _attackCooldown > 0) return false;
        _attackCooldown = .5f / Build.AttackSpeed;
        _strikeWindup=.06f;_strikeAim=Aim.Normalized();
        _strikeStrength=Mathf.Clamp(Flame.Current/100,0,1);
        _strikeFx.Begin(_strikeAim,_strikeStrength);
        Run.Audio.PlayStrike("strike_ignition",_strikeStrength);
        return true;
    }
    private void UpdateStrike(float dt)
    {
        if(_strikeLightTime>0)
        {
            _strikeLightTime=Mathf.Max(0,_strikeLightTime-dt);
            _strikeLight.Energy=Mathf.Lerp(.45f,1.65f,_strikeStrength)*Mathf.Clamp(_strikeLightTime/.1f,0,1);
            if(_strikeLightTime<=0){_strikeLight.Lit=false;Run.Lights.Remove(_strikeLight);}
        }
        if(_strikeWindup<=0)return;
        _strikeWindup-=dt;
        if(_strikeWindup<=0)ReleaseStrike();
    }
    private void ReleaseStrike()
    {
        _strikeFx.Release();
        _strikeLight.Position=_strikeAim*48;_strikeLight.ResetRadius(Mathf.Lerp(105,215,_strikeStrength));
        _strikeLight.Energy=Mathf.Lerp(.45f,1.65f,_strikeStrength);_strikeLight.Lit=true;_strikeLightTime=.15f;
        if(!Run.Lights.Contains(_strikeLight))Run.Lights.Add(_strikeLight);
        Run.Audio.PlayStrike("strike_whoosh",_strikeStrength);
        int hits=0;
        // Snapshot protects iteration against chained Kindling explosions/removals.
        foreach (var enemy in Run.Enemies.ToArray())
        {
            if (enemy.Dead || !Combat.InArc(Position, _strikeAim, enemy.Position, 104 + enemy.BodyRadius)) continue;
            if (!Run.Room.HasLineOfSight(Position, enemy.Position)) continue;
            _meleeHits++;
            var burn = Build.Has(ArtifactEffect.ThirdHitBurn) && _meleeHits % 3 == 0;
            var direction=(enemy.Position-Position).Normalized();
            if(direction==Vector2.Zero)direction=_strikeAim;
            var contact=enemy.Position-direction*enemy.BodyRadius*.65f;
            enemy.TakeDamage(new DamageInfo(MeleeDamage * Build.DamageMultiplier(Flame), Position, 240, burn, Strike:true));
            Run.Fx.FireImpact(contact,direction,_strikeStrength,Flame.LastEmber);
            hits++;
        }
        Run.Audio.PlayStrike(hits>0?"strike_impact":"strike_miss",_strikeStrength);
        if(hits>0)
        {
            Run.Audio.PlayStrike("strike_sparks",_strikeStrength);
            Run.Shake(2.6f);Run.HitStop(.045f);
        }
    }
    public bool TryBurst()
    {
        // Retained for older development tools; Burst is disabled in this MVP.
        return false;
    }
    public void TakeDamage(DamageInfo hit)
    {
        if (Dead || !Run.Playing || _invulnerable > 0) return;
        bool lastChance=!Flame.LastEmber&&hit.Amount>=Flame.Current;
        _invulnerable = lastChance?.5f:.65f;
        _knockback = (Position - hit.Origin).Normalized() * hit.Knockback;
        Flame.Damage(hit.Amount);
        Run.Fx.Sparks(Position, FlamePalette.Fire(Flame.LastEmber), 14);
        Run.Fx.Text(Position - new Vector2(0, 28), $"−{hit.Amount:0}", new Color(1, .4f, .25f));
        Run.Shake(lastChance?3:hit.Amount >= 12 ? 9 : 4);
        if(!lastChance)Run.Audio.Play("hurt");
    }
    public override void _Draw()
    {
        if(Revealing&&_revealTime<1.1f)
            DrawArc(Vector2.Zero,Mathf.Max(20,RevealLight.Radius*.88f),0,Mathf.Tau,128,FlamePalette.Shift(new Color(1,.86f,.56f,.65f*(1-_revealTime/1.1f)),Flame.LastEmber),4,true);
        DrawCircle(new Vector2(0, 13), 21, new Color(0, 0, 0, .65f));
        var white = _invulnerable > .2f && ((int)(_invulnerable * 25) % 2 == 0);
        var c = white ? Colors.White : new Color(1,.56f,.19f).Lerp(new Color(.15f,.65f,1),_blueBlend);
        DrawColoredPolygon(new[] { new Vector2(-15, 12), new Vector2(-11, -12), new Vector2(0, -24), new Vector2(12, -10), new Vector2(17, 14), new Vector2(0, 21) }, new Color(.25f, .13f, .1f));
        DrawColoredPolygon(new[] { new Vector2(-9, 7), new Vector2(-7, -7), new Vector2(-1, -20), new Vector2(4, -8), new Vector2(10, -3), new Vector2(7, 10), new Vector2(0, 14) }, c);
        DrawCircle(new Vector2(0, 0), 5, new Color(1,.92f,.67f).Lerp(new Color(.8f,.96f,1),_blueBlend));
        DrawLine(Aim * 18, Aim * 37, new Color(.95f, .87f, .70f), 4, true);
    }
}
