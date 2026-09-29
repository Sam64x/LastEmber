using Godot;
namespace LastEmber;
public sealed class InfernoBiome : DungeonBiome
{
    public override void Apply(Room room, DungeonRoom plan)
    {
        base.Apply(room,plan);
        if(plan.Type is RoomType.Combat or RoomType.Elite)
            Place(room,plan,plan.PropVariant==2?PropKind.FireTrap:PropKind.BurningFloor,"Q • QUENCH",new Vector2(90,90));
    }
    public override void ApplyRiskReward(Room room, DungeonRoom plan) =>
        Place(room,plan,PropKind.BurningFloor,"Q • QUENCH",new Vector2(90,90));
    public override DungeonProp CreateLesson(Room room, DungeonRoom plan) =>
        Place(room,plan,PropKind.BurningFloor,"Q • EXTINGUISH THE FIRE",new Vector2(90,90));
}
