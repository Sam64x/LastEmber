using System.Collections.Generic;
using Godot;

namespace LastEmber;

public sealed class StrikeSpec
{
    public int Step=1;
    public float DamageScale=1, Radius=104, ArcCosine=.12f, Knockback=240, Charge;
    public float WaveDamage, WaveRadius=120, WaveHeat;
    public bool FullCharge, ArmorBreak;
    public Vector2 Aim;
    public StrikeStyleData Style=null!;
    public float Windup,Tempo;
}

public sealed class StrikeHit
{
    public float Damage, Knockback, Heat, Stagger;
    public bool Burn, Critical;
}

// The player delegates LMB to this composition. Talent logic lives in independent modules.
public partial class MeleeController : Node2D
{
    [Export] public float ComboResetSeconds { get; set; } = 1.4f;
    [Export] public float ChargeSeconds { get; set; } = 1.15f;
    [Export] public MeleePresentationData Presentation { get; set; } = null!;
    public Player Player { get; set; } = null!;
    public RunManager Run => Player.Run;
    public BuildState Build => Player.Progression;
    public RandomNumberGenerator Random { get; } = new();
    public bool HasCore => Build.Has(MeleeEffectKind.MeleeCore);
    public bool Charging { get; private set; }
    public float ChargeRatio { get; private set; }
    public int LastStep { get; private set; }
    public int HitStreak => _combo.Hits;
    public float MovementMultiplier => _combo.MovementMultiplier * (Charging?.6f:1);
    public bool StableCharge => Charging && Build.Has(MeleeEffectKind.Unstoppable);
    public float IncomingDamageMultiplier => StableCharge?.5f:1;
    public float BurnDamageMultiplier => Player.Flame.LastEmber?1+Build.Value(MeleeEffectKind.BlueFire):1;
    private readonly List<MeleeModule> _modules=new();
    private ComboModule _combo=null!;
    private readonly StrikeInputBuffer _input=new();
    private MeleeAudio _audio=null!;
    private float _inputDelta,_fullChargeHeld;
    private float _cooldown, _windup, _comboClock;
    private int _sequence, _artifactHits;
    private StrikeSpec? _pending;
    public override void _Ready()
    {
        Presentation??=ResourceLoader.Load<MeleePresentationData>("res://Resources/Combat/melee_presentation.tres");
        ZIndex=26;Material=new CanvasItemMaterial {LightMode=CanvasItemMaterial.LightModeEnum.Unshaded};
        Random.Seed=Run.Seed ^ 0xB17DF1AEUL;
        _combo=new ComboModule(this);
        _modules.Add(_combo);_modules.Add(new HeavyModule(this));
        _modules.Add(new CritModule(this));_modules.Add(new HeatModule(this));
        _audio=new MeleeAudio {Player=Player,Presentation=Presentation};AddChild(_audio);
        Build.Changed+=Reset;
    }
    public override void _ExitTree()=>Build.Changed-=Reset;
    public void Tick(float dt)
    {
        _inputDelta=dt;_cooldown=Mathf.Max(0,_cooldown-dt);
        if(Charging)
        {
            float speed=Player.Flame.LastEmber?1+Build.Value(MeleeEffectKind.LastSwing):1;
            ChargeRatio=Mathf.Min(1,ChargeRatio+dt*speed/Mathf.Max(.1f,ChargeSeconds));
            if(ChargeRatio>=1)_fullChargeHeld+=dt;
            Player.UpdateChargeVisual(AimForCharge(),ChargeRatio);
            _audio.UpdateCharge(ChargeRatio,Player.Flame.LastEmber);
        }
        // An active, intentional charge can finish the sequence. Parking at full charge cannot.
        if(!Charging)_comboClock=Mathf.Max(0,_comboClock-dt);
        else if(ChargeRatio>=1&&_fullChargeHeld>=Mathf.Max(0,Presentation.ChargedComboGrace))_comboClock=0;
        if(_comboClock==0){_sequence=0;LastStep=0;}
        foreach(var module in _modules)module.Tick(dt);
        if(_windup>0)
        {
            _windup-=dt;
            if(_windup<=0 && _pending!=null){var strike=_pending;_pending=null;Release(strike);}
        }
        QueueRedraw();
    }
    public void HandleInput()
    {
        _input.Pump(this,_inputDelta,Time.GetTicksMsec()/1000.0,Input.IsActionPressed("melee"));
    }
    public void CaptureInput(bool down,Vector2 aim)=>_input.Capture(down,aim,Time.GetTicksMsec()/1000.0);
    internal bool ReadyForAttack=>!Player.Dead&&Run.Playing&&_cooldown<=0&&_windup<=0;
    private Vector2 AimForCharge()=>Player.Aim==Vector2.Zero?Vector2.Right:Player.Aim;
    internal void BeginHeldCharge()
    {
        if(!ReadyForAttack||Charging)return;
        Charging=true;ChargeRatio=0;_fullChargeHeld=0;
        _audio.StartCharge();Player.UpdateChargeVisual(AimForCharge(),0);
    }
    internal float FinishHeldCharge(bool cancelled=false)
    {
        float amount=ChargeRatio;Charging=false;ChargeRatio=0;_fullChargeHeld=0;
        Player.StopChargeVisual();_audio.StopCharge(cancelled);return amount;
    }
    public bool TryAttack(float charge=0,Vector2? aim=null)
    {
        if(!ReadyForAttack||Charging)return false;
        var strike=new StrikeSpec {Aim=(aim??Player.Aim).Normalized(),Charge=Build.Has(MeleeEffectKind.HeavyCore)?Mathf.Clamp(charge,0,1):0};
        if(strike.Aim==Vector2.Zero)strike.Aim=Vector2.Right;
        if(HasCore)
        {
            strike.Step=_sequence+1;LastStep=strike.Step;_sequence=(_sequence+1)%3;_comboClock=ComboResetSeconds;
            strike.DamageScale=strike.Step==3?1.5f:strike.Step==2?1.15f:1;
            if(strike.Step==2){strike.Radius=116;strike.ArcCosine=-.12f;}
            if(strike.Step==3){strike.Knockback=480;strike.WaveDamage=.35f;}
        }
        foreach(var module in _modules)module.Prepare(strike);
        strike.Style=strike.FullCharge?Presentation.ChargedStrike:Presentation.Style(strike.Step,HasCore);
        strike.Tempo=Mathf.Max(.1f,Player.Build.AttackSpeed*_combo.AttackSpeedMultiplier);
        strike.Windup=Mathf.Max(.025f,(strike.Style.Windup+strike.Charge*.05f)/Mathf.Sqrt(strike.Tempo));
        _cooldown=Mathf.Max(strike.Windup,(strike.Style.Interval+strike.Charge*.1f)/strike.Tempo);
        _windup=strike.Windup;_pending=strike;
        float power=Mathf.Clamp(Mathf.Max(strike.Charge,Player.Flame.Current/100),0,1);
        Player.BeginStrikeVisual(strike.Aim,strike.Radius/104,power,strike.Style,strike.Windup,strike.Tempo,strike.Charge);
        _audio.Strike(strike.Style,MeleeSoundPhase.Ignition,power,Player.Flame.LastEmber,strike.Charge);
        return true;
    }
    private void Release(StrikeSpec strike)
    {
        if(!Run.Playing||Player.Dead)return;
        Player.ReleaseStrikeVisual();
        Player.Cores.OnStrike(strike.Aim,strike.Step);
        float power=Mathf.Max(strike.Charge,Player.Flame.Ratio);
        _audio.Strike(strike.Style,MeleeSoundPhase.Swing,power,Player.Flame.LastEmber,strike.Charge);
        int hits=0;bool critical=false;
        foreach(var enemy in Run.Enemies.ToArray())
        {
            if(enemy.Dead || !Combat.InArc(Player.Position,strike.Aim,enemy.Position,strike.Radius+enemy.BodyRadius,strike.ArcCosine) ||
                !Run.Room.HasLineOfSight(Player.Position,enemy.Position))continue;
            _artifactHits++;
            var hit=new StrikeHit
            {
                Damage=Player.MeleeDamage*Player.Build.DamageMultiplier(Player.Flame)*strike.DamageScale,
                Knockback=strike.Knockback,
                Burn=Player.Build.Has(ArtifactEffect.ThirdHitBurn)&&_artifactHits%3==0
            };
            foreach(var module in _modules)module.BeforeHit(strike,enemy,hit);
            bool armorBroken=strike.ArmorBreak&&enemy.IceArmored;
            if(strike.ArmorBreak)enemy.BreakArmor();
            if(hit.Stagger>0)enemy.Stagger(hit.Stagger);
            if(hit.Heat>0)enemy.AddHeat(hit.Heat);
            var direction=(enemy.Position-Player.Position).Normalized();
            if(direction==Vector2.Zero)direction=strike.Aim;
            var contact=enemy.Position-direction*enemy.BodyRadius*.65f;
            enemy.TakeDamage(new DamageInfo(hit.Damage,Player.Position,hit.Knockback,hit.Burn,Strike:true,Critical:hit.Critical));
            var impact=(hit.Critical?ImpactTraits.Critical:ImpactTraits.None)
                |(strike.FullCharge?ImpactTraits.Charged:ImpactTraits.None)
                |(strike.Step==3?ImpactTraits.Finisher:ImpactTraits.None)
                |(armorBroken?ImpactTraits.ArmorBreak:enemy.IceArmored?ImpactTraits.Armored:ImpactTraits.None)
                |(hit.Burn?ImpactTraits.Ignite:ImpactTraits.None);
            Run.Fx.FireImpact(contact,direction,Mathf.Max(strike.Charge,Player.Flame.Ratio),Player.Flame.LastEmber,impact);
            if(hit.Critical)
            {
                critical=true;
                Run.Fx.CriticalImpact(contact,direction,Presentation.LightningIntensity,Player.Flame.LastEmber);
            }
            foreach(var module in _modules)module.AfterHit(strike,enemy,hit);
            hits++;
        }
        if(hits==0)_combo.Miss();
        foreach(var module in _modules)module.EndStrike(strike,hits);
        if(strike.WaveDamage>0)Wave(strike.Aim,strike.WaveRadius,strike.WaveDamage,strike.WaveHeat);
        _audio.Strike(strike.Style,hits>0?MeleeSoundPhase.Hit:MeleeSoundPhase.Miss,power,Player.Flame.LastEmber,strike.Charge);
        if(critical)_audio.Critical(Player.Flame.LastEmber);
        if(hits>0)
        {
            Run.Shake(Mathf.Min(6,strike.Style.Shake+strike.Charge*2+(critical?.5f:0)));
            Run.HitStop(Mathf.Max(strike.Style.HitStop,strike.FullCharge?.06f:critical?.05f:0));
        }
    }
    // Secondary waves deliberately do not dispatch melee-hit / crit hooks (no recursive procs).
    public void Wave(Vector2 aim,float radius,float damageScale,float heat=0,float delay=0)
    {
        Run.Room.AddChild(new MeleeWave
        {
            Run=Run,Position=Player.Position+aim*36,Direction=aim,
            Radius=radius*(1+Player.Build.Get(ArtifactEffect.BurstRadius)),
            Damage=Player.MeleeDamage*Player.Build.DamageMultiplier(Player.Flame)*damageScale,
            Heat=heat,Delay=delay,Blue=Player.Flame.LastEmber
        });
    }
    public void OnEnemyKilled(Enemy enemy,bool burning)
    {
        foreach(var module in _modules)module.OnKill(enemy,burning);
    }
    public void OnDash()
    {
        CancelCharge();
        if(!Build.Has(MeleeEffectKind.Relentless)){_sequence=0;_comboClock=0;_combo.Reset();}
    }
    public void OnDamaged(){if(Charging&&!StableCharge)CancelCharge();}
    public void CancelCharge(){_input.Clear();FinishHeldCharge(true);QueueRedraw();}
    public void ResetArtifactCounter()=>_artifactHits=0;
    public void Reset()
    {
        Player.CancelStrikeVisual();
        _audio.StopAll();
        CancelCharge();_pending=null;_windup=0;_cooldown=0;_sequence=0;LastStep=0;_comboClock=0;
        foreach(var module in _modules)module.Reset();
    }
}
