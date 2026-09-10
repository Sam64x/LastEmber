using Godot;

namespace LastEmber;

[GlobalClass]
public partial class BuildUpgradeData : Resource
{
    [Export] public string Id { get; set; } = "";
    [Export] public string DisplayName { get; set; } = "";
    [Export(PropertyHint.MultilineText)] public string Description { get; set; } = "";
    [Export] public string[] Tags { get; set; } = System.Array.Empty<string>();
    [Export] public string[] RequiredTags { get; set; } = System.Array.Empty<string>();
    [Export] public string[] RequiredIds { get; set; } = System.Array.Empty<string>();
    [Export] public int MaxRank { get; set; } = 1;
    [Export] public int Rarity { get; set; } = 2;
    [Export] public float Weight { get; set; } = 1;
    [Export] public bool Synergy { get; set; }
    [Export] public Godot.Collections.Array<MeleeEffectData> Effects { get; set; } = new();
    public string Category => this is CoreData ? "CORE" : this is SubCoreData ? "SUBCORE" : Synergy ? "SYNERGY" : "TALENT";
}
