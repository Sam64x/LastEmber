using Godot;
namespace LastEmber;
public sealed class DarkBiome : DungeonBiome
{
    public override void Apply(Room room, DungeonRoom plan)
    {
        base.Apply(room,plan);
        if(plan.Type is not (RoomType.Combat or RoomType.Elite)||!room.Run.Dungeon.Definition.AshTraps)return;
        var point=room.FindFreePosition(new Vector2(.65f,.3f+plan.PropVariant*.2f),65);
        room.AddChild(new AshTrap {Run=room.Run,Position=point,ZIndex=12});
    }
}
