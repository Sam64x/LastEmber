using Godot;
namespace LastEmber;

// Stable serialized IDs. New effects are implemented by combat modules, never by Player.
public enum MeleeEffectKind
{
    MeleeCore, ComboCore, CritChance, HeavyCore, HeatPerHit,
    Flow, Relentless, FourthFlame, Momentum,
    DeepCut, Execution, ChainReaction,
    Unstoppable, CrushingBlow, Aftershock, LastSwing,
    Wildfire, Fuel, BlueFire,
    ComboCrit, CritBurn, HeavyCrit, HeavyBurn, ComboBurn,
    BoltCore, BoltPierce, BoltVolley,
    OrbitCore, OrbitCount, OrbitSpeed,
    DashCore, DashImpact, DashTrail,
    EmberLance, StellarConduit, MeteorStep, CinderCrossfire
}

[GlobalClass]
public partial class MeleeEffectData : Resource
{
    [Export] public MeleeEffectKind Kind { get; set; }
    [Export] public float Value { get; set; } = 1;
}
