namespace LastEmber;

// Deliberate authored pacing, randomized content. No runtime combat RNG here.
public sealed class DungeonDirector
{
    public static readonly RoomType[] MvpRoute =
    {
        RoomType.Start, RoomType.Combat, RoomType.Ability, RoomType.RiskReward,
        RoomType.Combat, RoomType.Elite, RoomType.Altar, RoomType.Treasure, RoomType.Boss
    };
    public void Assign(DungeonGraph graph)
    {
        for(int i=0;i<graph.Rooms.Count;i++)graph.Rooms[i].Type=MvpRoute[i];
    }
    public EncounterDefinition Choose(DungeonRoom room, Godot.RandomNumberGenerator rng) => room.Type switch
    {
        RoomType.Start => new() {Id="arrival"},
        RoomType.Ability => new() {Id="attunement",Kind=EncounterKind.Ability},
        RoomType.RiskReward => new() {Id="guarded-cache",Kind=EncounterKind.RiskReward,EnemyCount=2},
        RoomType.Elite => new() {Id="torchbearer-escort",Kind=EncounterKind.Elite,EnemyCount=2},
        RoomType.Altar => new() {Id="sacrifice",Kind=EncounterKind.Altar},
        RoomType.Treasure => new() {Id="sanctuary",Kind=EncounterKind.Reward},
        RoomType.Boss => new() {Id="guardian",Kind=EncounterKind.Boss},
        _ => Combat(room.Id, rng)
    };
    private static EncounterDefinition Combat(int depth, Godot.RandomNumberGenerator rng)
    {
        var kind=(EncounterKind)rng.RandiRange((int)EncounterKind.Skirmish,(int)EncounterKind.Ambush);
        return new() {Id=kind.ToString().ToLowerInvariant(),Kind=kind,EnemyCount=depth<3?3:5,Waves=kind==EncounterKind.Reinforcements?2:1};
    }
}
