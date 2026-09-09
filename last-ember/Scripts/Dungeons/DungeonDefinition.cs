using Godot;
namespace LastEmber;
public enum DungeonType { Darkness, Frost, Inferno }
[GlobalClass]
public partial class DungeonDefinition : Resource
{
    [Export] public DungeonType DungeonType { get; set; }
    [Export] public string DisplayName { get; set; } = "Darkness";
    [Export] public string Lesson { get; set; } = "Q reveals what waits in darkness";
    [Export] public DungeonAbility Ability { get; set; } = null!;
    [Export] public Color Ambient { get; set; } = Colors.Black;
    [Export] public Color FloorTint { get; set; } = Colors.White;
    [Export] public AudioStream? AmbientSound { get; set; }
    [Export] public Godot.Collections.Array<EnemyKind> Enemies { get; set; } = new();
    [Export] public Godot.Collections.Array<PackedScene> Rooms { get; set; } = new();
    [Export] public PackedScene? Environment { get; set; }
    [Export] public PackedScene? Boss { get; set; }
    [Export] public string BossName { get; set; } = "The Extinguisher";
    [Export] public float EnemyDamageMultiplier { get; set; } = 1;
    [Export] public float SurfaceDrag { get; set; } = 5;
    [Export] public bool AshTraps { get; set; } = true;
}
