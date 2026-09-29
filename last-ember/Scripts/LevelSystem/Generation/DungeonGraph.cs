using System.Collections.Generic;
namespace LastEmber;

public enum RoomType { Start, Combat, Ability, RiskReward, Elite, Altar, Treasure, Boss }

public sealed class DungeonRoom
{
    public int Id { get; init; }
    public List<int> Exits { get; } = new();
    public RoomType Type { get; set; }
    public RoomDefinition Geometry { get; set; } = null!;
    public EncounterDefinition Encounter { get; set; } = null!;
    public ulong Seed { get; set; }
    public int PropVariant { get; set; }
    public string DisplayType => Type==RoomType.RiskReward?"CACHE":Type.ToString().ToUpperInvariant();
    public StageKind Stage => Type switch
    {
        RoomType.Elite => StageKind.Elite, RoomType.Boss => StageKind.Boss,
        RoomType.Altar => StageKind.Altar, RoomType.Treasure => StageKind.Reward,
        _ => StageKind.Combat
    };
}

public sealed class DungeonGraph
{
    public ulong Seed { get; init; }
    public DungeonDefinition Definition { get; init; } = null!;
    public DungeonBiome Biome { get; init; } = null!;
    public List<DungeonRoom> Rooms { get; } = new();
    public static DungeonGraph Linear(ulong seed, DungeonDefinition definition, int count)
    {
        var graph=new DungeonGraph {Seed=seed,Definition=definition,Biome=DungeonBiome.For(definition)};
        for(int i=0;i<count;i++)
        {
            var node=new DungeonRoom {Id=i};
            if(i+1<count)node.Exits.Add(i+1);
            graph.Rooms.Add(node);
        }
        return graph;
    }
}
