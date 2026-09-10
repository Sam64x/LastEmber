using Godot;

namespace LastEmber;

public enum ArtifactEffect { MaxFlame, LowDamage, ThirdHitBurn, DashTrail, BurstRadius, Glass, LowSpeed, BurnExplosion, BurnHeal, DarkRewards }

[GlobalClass]
public partial class ArtifactData : Resource
{
    [Export] public string Id { get; set; } = "";
    [Export] public string DisplayName { get; set; } = "";
    [Export(PropertyHint.MultilineText)] public string Description { get; set; } = "";
    [Export] public int Rarity { get; set; } = 1;
    [Export] public Texture2D? Icon { get; set; }
    [Export] public ArtifactEffect Effect { get; set; }
    [Export] public float Value { get; set; }
    [Export] public string[] Tags { get; set; } = System.Array.Empty<string>();
    [Export] public bool Stackable { get; set; }
}
