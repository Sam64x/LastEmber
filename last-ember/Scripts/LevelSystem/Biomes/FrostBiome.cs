using Godot;
namespace LastEmber;
public sealed class FrostBiome : DungeonBiome
{
    public override void Apply(Room room, DungeonRoom plan)
    {
        base.Apply(room,plan);
        if(plan.Type is RoomType.Combat or RoomType.Elite)
            Place(room,plan,PropKind.FrozenArea,"FROZEN GROUND",new Vector2(100,90));
    }
    public override void ApplyRiskReward(Room room, DungeonRoom plan) =>
        Place(room,plan,PropKind.FrozenTreasure,"Q • FROZEN CACHE",new Vector2(60,60));
    public override DungeonProp CreateLesson(Room room, DungeonRoom plan) =>
        Place(room,plan,PropKind.IceWall,"Q • SHATTER THE ICE",new Vector2(70,90));
}
