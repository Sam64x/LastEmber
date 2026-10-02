using Godot;

namespace LastEmber;

public partial class Player : CharacterBody2D, IDamageable
{
    [Export] public float MoveSpeed { get; set; } = 220;
    [Export] public float MeleeDamage { get; set; } = 20;
    [Export] public float BurstDamage { get; set; } = 48;
    [Export] public float DashDuration { get; set; } = .18f;
    public float RevealCost => Attunement?.Settings.Cost ?? 5;
    public AbilityRuntime? Attunement { get; private set; }
    public string AbilityName => Attunement == null ? "" : Flame.LastEmber ? Attunement.Settings.BlueName : Attunement.Settings.DisplayName;
    public void Attune(DungeonAbility definition)
    {
        if(Attunement!=null){Attunement.ExitDungeon();RemoveChild(Attunement);Attunement.QueueFree();}
        Attunement=definition.CreateRuntime();Attunement.Settings=definition;Attunement.EnterDungeon(this);AddChild(Attunement);
    }
    public FlamePool Flame { get; } = new();
    public BuildStats Build { get; private set; } = new();
    public BuildState Progression { get; } = new();
    public MeleeController Melee { get; private set; } = null!;
    public CoreCombatController Cores { get; private set; } = null!;
    public EmberLight Light { get; private set; } = null!;
    public EmberLight RevealLight {get;private set;}=null!;
    public RunManager Run { get; set; } = null!;
    public bool Dead => Flame.Dead;
    public bool Dashing => _dashTime > 0;
    public float DashCooldown { get; private set; }
    public float BurstCooldown { get; private set; }
    public float RevealCooldown => Attunement?.Remaining ?? 0;
    public bool BluePulseActive => Revealing && Attunement!.Blue;
    private float _coldClock, _blueBlend;
    public bool Revealing => Attunement is { Reveals: true, Active: true };
    public Vector2 Aim { get; set; } = Vector2.Right;
    public Vector2 TestMovement { get; set; }
    public bool Automated { get; set; }
    private bool _waitForMouseRelease;
    private float _dashTime, _invulnerable, _trailClock;
    private float _strikeStrength, _strikeLightTime;
    private Vector2 _strikeAim;
    private FireStrikeFx _strikeFx=null!;
    private EmberLight _strikeLight=null!;
    private Vector2 _dashDirection, _knockback;

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
        Melee=new MeleeController {Player=this};AddChild(Melee);
        Cores=new CoreCombatController {Player=this};AddChild(Cores);
        ZIndex = 8;
    }
    public override void _PhysicsProcess(double delta)
    {
        if (Dead || !Run.Playing) return;
        var dt = (float)delta;
        DashCooldown = Mathf.Max(0, DashCooldown - dt);
        BurstCooldown = Mathf.Max(0, BurstCooldown - dt);


        _blueBlend=Mathf.Lerp(_blueBlend,Flame.LastEmber?1:0,1-Mathf.Exp(-dt*12));
        Light.Tint=FlamePalette.Light(Flame.LastEmber);

        _strikeLight.Tint=FlamePalette.Light(Flame.LastEmber);
        _strikeFx.Blue=Flame.LastEmber;
        _coldClock-=dt;
        if(Flame.LastEmber&&_coldClock<=0){_coldClock=.65f;Run.Audio.Play("blue_crackle");}
        _invulnerable = Mathf.Max(0, _invulnerable - dt);
        Melee.Tick(dt);
        UpdateStrike(dt);
        var input = Automated ? TestMovement.LimitLength() : Input.GetVector("left", "right", "up", "down");
        if (!Automated)
        {
            var aim = GetGlobalMousePosition() - GlobalPosition;
            if (aim.LengthSquared() > 1) Aim = aim.Normalized();
            if (Input.IsActionJustPressed("dash")) TryDash(input);
            if (Input.IsActionJustPressed("reveal")) TryDungeonAbility();
            if(_waitForMouseRelease)_waitForMouseRelease=Input.IsActionPressed("melee");
            else
            {
                Melee.HandleInput();
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
        else
        {
            var desired=input * MoveSpeed * Build.SpeedMultiplier(Flame) * Melee.MovementMultiplier;
            Velocity=Run.Dungeon.IsSlippery(Position)?Velocity.Lerp(desired,1-Mathf.Exp(-dt*Mathf.Max(.1f,Run.Dungeon.Definition.SurfaceDrag))):desired;
            Velocity+=_knockback;
        }
        _knockback = _knockback.MoveToward(Vector2.Zero, dt * 750);
        MoveAndSlide();
        _stepClock-=dt;
        if(!Dashing&&Velocity.LengthSquared()>1000&&_stepClock<=0){_stepClock=.42f;Run.Audio.PlayAt("step",Position,.65f);}
        var bounds=Run.Room.Bounds.Grow(-16);
        Position = new Vector2(Mathf.Clamp(Position.X, bounds.Position.X, bounds.End.X), Mathf.Clamp(Position.Y, bounds.Position.Y, bounds.End.Y));
        Cores.Tick(dt);
        Light.TargetRadius = FlameLight.Radius(Flame.Current,Build.LightMultiplier);

        QueueRedraw();
    }
    public bool TryDash(Vector2 direction)
    {
        if (Dead || !Run.Playing || DashCooldown > 0) return false;
        Melee.OnDash();
        DashCooldown = 1; _dashTime = DashDuration; _invulnerable = .2f;
        _dashDirection = direction.LengthSquared() > .01f ? direction.Normalized() : Aim;
        Cores.OnDash();
        Run.BreakTethers();
        if (Build.DashExplosion) Run.Explode(Position, 115, Build.DashExplosionDamage, false);
        Run.Audio.Play("dash");
        return true;
    }
    public void SuppressUiClick(){_waitForMouseRelease=true;Melee.CancelCharge();}
    public bool TryDungeonAbility()
    {
        bool activated=Attunement?.Activate()??false;
        if(activated)Run.Encounter?.AbilityUsed();
        return activated;
    }
    public bool TryReveal() => TryDungeonAbility(); // Compatibility for existing development tools.
    public void ResetForRoom()
    {
        Melee.Reset();
        Cores.Reset();
        CancelReveal();
    }
    public void CancelStrikeVisual()
    {
        _strikeLightTime=0;_strikeFx.Clear();_strikeLight.Lit=false;Run.Lights.Remove(_strikeLight);
    }
    private void CancelReveal() => Attunement?.Cancel();
    private void OnLastEmberChanged(bool blue)
    {
        CancelReveal();
        Light.Tint=RevealLight.Tint=_strikeLight.Tint=FlamePalette.Light(blue);
        _strikeFx.Blue=blue;_coldClock=.2f;
        Run.Fx.FlameTransition(Position,blue);
        Run.Audio.Play(blue?"last_ember":"ember_return");
        if(Run.Playing){Run.Shake(3);Run.HitStop(.05f);}
        Run.Hud.Toast(blue?"LAST EMBER • ONE LAST CHANCE • Q DUNGEON ABILITY":"FLAME RESTORED • THE LAST CHANCE IS RENEWED");
    }
    private void OnFlameChanged()
    {
        if(Flame.Current>_lastFlame+.5f)Run.Audio.Play("ignite");
        else if(Flame.Current<_lastFlame&&Flame.Ratio<.1f)Run.Audio.Play(Flame.LastEmber?"blue_crackle":"ember_loss");
        _lastFlame=Flame.Current;
    }
    public void CaptureMeleeInput(bool down)
    {
        if(_waitForMouseRelease){if(!down)_waitForMouseRelease=false;return;}
        if(Dead)return;
        var offset=GetGlobalMousePosition()-GlobalPosition;
        Melee.CaptureInput(down,offset.LengthSquared()>1?offset.Normalized():Aim);
    }
    public bool TryMelee() => Melee.TryAttack();
    public void BeginStrikeVisual(Vector2 aim,float size,float strength,StrikeStyleData style,float windup,float tempo,float charge)
    {
        _strikeAim=aim;_strikeStrength=Mathf.Clamp(strength,0,1);

        _strikeFx.Begin(aim,_strikeStrength,style,size,windup,tempo,charge);

    }
    public void UpdateChargeVisual(Vector2 aim,float ratio)=>_strikeFx.ShowCharge(aim,ratio);
    public void StopChargeVisual()=>_strikeFx.StopCharge();
    private void UpdateStrike(float dt)
    {
        if(_strikeLightTime<=0)return;
        _strikeLightTime=Mathf.Max(0,_strikeLightTime-dt);
        _strikeLight.Energy=Mathf.Lerp(.45f,1.65f,_strikeStrength)*Mathf.Clamp(_strikeLightTime/.1f,0,1);
        if(_strikeLightTime<=0){_strikeLight.Lit=false;Run.Lights.Remove(_strikeLight);}
    }
    public void ReleaseStrikeVisual()
    {
        _strikeFx.Release();
        _strikeLight.Position=_strikeAim*48;_strikeLight.ResetRadius(Mathf.Lerp(105,215,_strikeStrength));
        _strikeLight.Energy=Mathf.Lerp(.45f,1.65f,_strikeStrength);_strikeLight.Lit=true;_strikeLightTime=.15f;
        if(!Run.Lights.Contains(_strikeLight))Run.Lights.Add(_strikeLight);

    }
    public void ResetDevBuild()
    {
        if(!Run.DevEnabled || Dead)return;
        ResetForRoom();Melee.ResetArtifactCounter();Build=new BuildStats();Progression.Clear();
        Flame.ChangeMaximum(100-Flame.Maximum);Flame.Heal(100);
    }
    public bool TryBurst()
    {
        // Retained for older development tools; Burst is disabled in this MVP.
        return false;
    }
    public void TakeDamage(DamageInfo hit)
    {
        if (Dead || !Run.Playing || _invulnerable > 0 || Run.DevEnabled && Run.DevInvulnerable) return;
        hit=hit with {Amount=hit.Amount*Melee.IncomingDamageMultiplier};
        bool lastChance=!Flame.LastEmber&&hit.Amount>=Flame.Current;
        _invulnerable = lastChance?.5f:.65f;
        _knockback = Melee.StableCharge?Vector2.Zero:(Position - hit.Origin).Normalized() * hit.Knockback;
        Melee.OnDamaged();
        Flame.Damage(hit.Amount);
        Run.Fx.Sparks(Position, FlamePalette.Fire(Flame.LastEmber), 14);
        Run.Fx.Text(Position - new Vector2(0, 28), $"−{hit.Amount:0}", new Color(1, .4f, .25f));
        Run.Shake(lastChance?3:hit.Amount >= 12 ? 9 : 4);
        if(!lastChance)Run.Audio.Play("hurt");
    }
    public override void _Draw()
    {
        DrawCircle(new Vector2(0, 13), 21, new Color(0, 0, 0, .65f));
        DrawSetTransform(_strikeFx.BodyOffset,_strikeFx.BodyRotation,_strikeFx.BodyScale);
        var white = _invulnerable > .2f && ((int)(_invulnerable * 25) % 2 == 0);
        var c = white ? Colors.White : new Color(1,.56f,.19f).Lerp(new Color(.15f,.65f,1),_blueBlend);
        DrawColoredPolygon(new[] { new Vector2(-15, 12), new Vector2(-11, -12), new Vector2(0, -24), new Vector2(12, -10), new Vector2(17, 14), new Vector2(0, 21) }, new Color(.25f, .13f, .1f));
        DrawColoredPolygon(new[] { new Vector2(-9, 7), new Vector2(-7, -7), new Vector2(-1, -20), new Vector2(4, -8), new Vector2(10, -3), new Vector2(7, 10), new Vector2(0, 14) }, c);
        DrawCircle(new Vector2(0, 0), 5, new Color(1,.92f,.67f).Lerp(new Color(.8f,.96f,1),_blueBlend));
        DrawSetTransform(Vector2.Zero,0,Vector2.One);
        DrawLine(Aim * 18, Aim * 37, new Color(.95f, .87f, .70f), 4, true);
    }
}
