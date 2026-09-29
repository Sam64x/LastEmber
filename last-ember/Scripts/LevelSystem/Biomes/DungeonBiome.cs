using Godot;
namespace LastEmber;

public abstract class DungeonBiome
{
    public static DungeonBiome For(DungeonDefinition definition) => definition.DungeonType switch
    {
        DungeonType.Frost => new FrostBiome(), DungeonType.Inferno => new InfernoBiome(), _ => new DarkBiome()
    };
    public virtual void Apply(Room room, DungeonRoom plan) => room.Modulate=room.Run.Dungeon.Definition.FloorTint;
    public virtual DungeonProp? CreateLesson(Room room, DungeonRoom plan) => null;
    public virtual void ApplyRiskReward(Room room, DungeonRoom plan) { }
    protected static DungeonProp Place(Room room, DungeonRoom plan, PropKind kind, string label, Vector2 size)
    {
        // Validated free-space placement keeps geometry, LOS and collision in agreement.
        var point=room.FindFreePosition(new Vector2(.55f,.3f+plan.PropVariant*.2f),Mathf.Max(size.X,size.Y)*.5f+100);
        var prop=new DungeonProp {Kind=kind,Position=point,Size=size,Label=label};
        room.AddChild(prop);return prop;
    }
}
