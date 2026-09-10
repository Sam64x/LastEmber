using Godot;

namespace LastEmber;

public abstract class MeleeModule
{
    protected MeleeController Owner { get; }
    protected BuildState Build=>Owner.Build;
    protected MeleeModule(MeleeController owner){Owner=owner;}
    protected float Value(MeleeEffectKind kind)=>Build.Value(kind);
    protected bool Has(MeleeEffectKind kind)=>Build.Has(kind);
    public virtual void Tick(float dt) { }
    public virtual void Prepare(StrikeSpec strike) { }
    public virtual void BeforeHit(StrikeSpec strike,Enemy enemy,StrikeHit hit) { }
    public virtual void AfterHit(StrikeSpec strike,Enemy enemy,StrikeHit hit) { }
    public virtual void EndStrike(StrikeSpec strike,int hits) { }
    public virtual void OnKill(Enemy enemy,bool burning) { }
    public virtual void Reset() { }
}

public sealed class ComboModule : MeleeModule
{
    public int Hits { get; private set; }
    private float _time;
    public ComboModule(MeleeController owner):base(owner) { }
    public float AttackSpeedMultiplier=>1+Mathf.Min(Hits,10)*Value(MeleeEffectKind.ComboCore)+Mathf.Min(Hits,6)*Value(MeleeEffectKind.Flow);
    public float MovementMultiplier=>1+(Hits>=5?Mathf.Min(Hits,10)*Value(MeleeEffectKind.Momentum):0);
    public override void Tick(float dt){_time=Mathf.Max(0,_time-dt);if(_time==0)Hits=0;}
    public override void AfterHit(StrikeSpec strike,Enemy enemy,StrikeHit hit)
    {
        if(!Owner.HasCore)return;
        Hits++;_time=2;
        if(Hits%4==0 && (Has(MeleeEffectKind.FourthFlame)||Has(MeleeEffectKind.ComboBurn)))
            Owner.Wave(strike.Aim,200,Has(MeleeEffectKind.FourthFlame)?.8f:.3f,Value(MeleeEffectKind.ComboBurn));
    }
    public void Miss()=>Reset();
    public override void Reset(){Hits=0;_time=0;}
}

public sealed class CritModule : MeleeModule
{
    private float _chain,_chainTime;
    public CritModule(MeleeController owner):base(owner) { }
    public override void Tick(float dt){_chainTime=Mathf.Max(0,_chainTime-dt);if(_chainTime==0)_chain=0;}
    public override void BeforeHit(StrikeSpec strike,Enemy enemy,StrikeHit hit)
    {
        if(!Has(MeleeEffectKind.CritChance))return;
        float chance=Value(MeleeEffectKind.CritChance)+_chain;
        _chain=0;_chainTime=0;
        if(enemy.Health/enemy.MaxHealth<=.3f)chance+=Value(MeleeEffectKind.Execution);
        bool precise=enemy.WeakPointRemaining>0 || strike.FullCharge&&Has(MeleeEffectKind.HeavyCrit) ||
            strike.Step==3&&Owner.HitStreak>=5&&Has(MeleeEffectKind.ComboCrit);
        hit.Critical=precise || Owner.Random.Randf()<Mathf.Clamp(chance,0,1);
        if(!hit.Critical)return;
        enemy.ConsumeWeakPoint();hit.Damage*=1.75f;
        hit.Burn|=Has(MeleeEffectKind.DeepCut);
        hit.Heat+=Value(MeleeEffectKind.CritBurn);
    }
    public override void AfterHit(StrikeSpec strike,Enemy enemy,StrikeHit hit)
    {
        if(hit.Critical){_chain=Value(MeleeEffectKind.ChainReaction);_chainTime=2.5f;}
    }
    public override void Reset(){_chain=0;_chainTime=0;}
}

public sealed class HeavyModule : MeleeModule
{
    public HeavyModule(MeleeController owner):base(owner) { }
    public override void Prepare(StrikeSpec strike)
    {
        if(!Has(MeleeEffectKind.HeavyCore)||strike.Charge<=0)return;
        strike.DamageScale*=Mathf.Lerp(1,3,strike.Charge);
        strike.Radius+=32*strike.Charge;strike.Knockback+=300*strike.Charge;
        strike.FullCharge=strike.Charge>=.999f;
        if(!strike.FullCharge)return;
        strike.ArmorBreak=true;strike.WaveRadius=230;strike.WaveDamage=1.2f;
        strike.WaveHeat=Has(MeleeEffectKind.HeavyBurn)?50:0;
    }
    public override void BeforeHit(StrikeSpec strike,Enemy enemy,StrikeHit hit)
    {
        if(!strike.FullCharge)return;
        hit.Stagger=Value(MeleeEffectKind.CrushingBlow);
        hit.Heat+=Value(MeleeEffectKind.HeavyBurn);
    }
    public override void EndStrike(StrikeSpec strike,int hits)
    {
        if(strike.FullCharge&&Has(MeleeEffectKind.Aftershock))Owner.Wave(strike.Aim,200,.7f,strike.WaveHeat,.4f);
    }
}

public sealed class HeatModule : MeleeModule
{
    public HeatModule(MeleeController owner):base(owner) { }
    public override void BeforeHit(StrikeSpec strike,Enemy enemy,StrikeHit hit)
        =>hit.Heat+=Value(MeleeEffectKind.HeatPerHit);
    public override void OnKill(Enemy enemy,bool burning)
    {
        if(!burning)return;
        float spread=Value(MeleeEffectKind.Wildfire);
        if(spread>0)
        {
            foreach(var other in Owner.Run.Enemies.ToArray())
                if(!other.Dead&&other!=enemy&&other.Position.DistanceTo(enemy.Position)<190&&Owner.Run.Room.HasLineOfSight(enemy.Position,other.Position))other.AddHeat(spread);
            Owner.Run.Fx.Ring(enemy.Position,190,FlamePalette.Fire(Owner.Player.Flame.LastEmber));
        }
        if(Owner.Random.Randf()<Value(MeleeEffectKind.Fuel))Owner.Player.Flame.Heal(3);
    }
}
