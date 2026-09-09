using Godot;

namespace LastEmber;

public interface IDungeonAbility
{
    bool CanActivate();
    bool Activate();
    void EnterDungeon(Player player);
    void ExitDungeon();
}

// Resources contain configuration only. Each attunement gets its own runtime Node.
[GlobalClass]
public partial class DungeonAbility : Resource
{
    [Export] public string DisplayName { get; set; } = "Reveal";
    [Export] public string BlueName { get; set; } = "Blue Pulse";
    [Export] public float Cost { get; set; } = 5;
    [Export] public float Cooldown { get; set; } = 10;
    [Export] public float BlueCooldown { get; set; } = 3;
    [Export] public float Radius { get; set; } = 1900;
    [Export(PropertyHint.Range, "0.05,1")] public float BlueRadiusScale { get; set; } = .4f;
    [Export] public float Duration { get; set; } = 3.5f;
    [Export] public float BlueDuration { get; set; } = .8f;
    [Export] public Color Tint { get; set; } = new(1,.85f,.63f);
    [Export] public string Sound { get; set; } = "reveal";
    [Export] public string BlueSound { get; set; } = "blue_pulse";
    public virtual AbilityRuntime CreateRuntime() => new RevealAbility();
}

public abstract partial class AbilityRuntime : Node2D, IDungeonAbility
{
    public DungeonAbility Settings { get; set; } = null!;
    protected Player Player = null!;
    protected RunManager Run => Player.Run;
    public virtual bool Reveals => false;
    public bool Active { get; protected set; }
    public bool Blue { get; protected set; }
    protected float Time;
    private float _normalCooldown, _blueCooldown;
    public float Remaining => Player.Flame.LastEmber ? _blueCooldown : _normalCooldown;
    protected float Radius => Settings.Radius * (Blue ? Mathf.Clamp(Settings.BlueRadiusScale,.05f,1) : 1);
    protected float Duration => Mathf.Max(.05f,Blue ? Mathf.Min(Settings.BlueDuration,Settings.Duration) : Settings.Duration);
    protected Color Tint => Blue ? new Color(.7f,.93f,1) : Settings.Tint;
    public void EnterDungeon(Player player)
    {
        Player=player;ZIndex=22;
        Material=new CanvasItemMaterial {LightMode=CanvasItemMaterial.LightModeEnum.Unshaded};
    }
    public virtual void ExitDungeon() => Cancel();
    public virtual void Cancel() { Active=false; QueueRedraw(); }
    public bool CanActivate() => !Player.Dead && Run.Playing && !Active && Remaining<=0 &&
        (Player.Flame.LastEmber || (Settings.Cost>=0 && Player.Flame.Current>Settings.Cost));
    public bool Activate()
    {
        if(!CanActivate())return false;
        // Snapshot BEFORE paying: crossing the threshold never upgrades a paid cast.
        Blue=Player.Flame.LastEmber;
        if(!Blue && !Player.Flame.Spend(Settings.Cost))return false;
        if(Blue)_blueCooldown=Mathf.Max(.1f,Settings.BlueCooldown);else _normalCooldown=Mathf.Max(.1f,Settings.Cooldown);
        Time=0;Active=true; Run.Audio.Play(Blue?Settings.BlueSound:Settings.Sound); Begin();return true;
    }
    protected virtual void Begin() { }
    public override void _PhysicsProcess(double delta)
    {
        if(!Run.Playing || Player.Dead)return;
        float dt=(float)delta;
        _normalCooldown=Mathf.Max(0,_normalCooldown-dt);_blueCooldown=Mathf.Max(0,_blueCooldown-dt);
        if(Active){Time+=dt;Tick(dt);QueueRedraw();}
    }
    protected virtual void Tick(float dt) { if(Time>=Duration)Cancel(); }
    protected void Affect(DungeonImpact kind, float seconds=0)
    {
        foreach(var node in GetTree().GetNodesInGroup("dungeon_reactive"))
            if(node is Node2D spatial && node is IDungeonReactive target && !node.IsQueuedForDeletion() &&
               spatial.GlobalPosition.DistanceTo(Player.GlobalPosition)<=Radius)
                target.React(kind,seconds,Player.GlobalPosition,Blue);
    }
    public override void _Draw()
    {
        if(!Active)return;
        float t=Mathf.Clamp(Time/Duration,0,1);
        DrawArc(Vector2.Zero,Mathf.Max(4,Radius*Mathf.SmoothStep(0,1,t)),0,Mathf.Tau,96,new Color(Tint,1-t),4,true);
    }
}

public enum DungeonImpact { Heat, Suction, Blast }
public interface IDungeonReactive { void React(DungeonImpact impact,float seconds,Vector2 origin,bool blue); }
