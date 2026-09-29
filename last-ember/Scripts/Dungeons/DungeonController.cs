using Godot;
namespace LastEmber;
public partial class DungeonController : Node
{
    public DungeonDefinition Definition { get; private set; } = null!;
    public RunManager Run { get; set; } = null!;
    private readonly AudioStreamPlayer _ambient = new();
    public override void _Ready()=>AddChild(_ambient);
    public void Enter(DungeonDefinition definition)
    {
        Definition=definition;
        Run.Player.Attune(definition.Ability);
        _ambient.Stop();_ambient.Stream=definition.AmbientSound;
        _ambient.VolumeDb=-24;if(_ambient.Stream!=null)_ambient.Play();
    }
    public void Populate(Room room)
    {
        if(Run.Graph!=null)Run.Graph.Biome.Apply(room,Run.CurrentRoomPlan!);
    }
    public bool IsSlippery(Vector2 position)
    {
        foreach(var node in GetTree().GetNodesInGroup("dungeon_reactive"))
            if(node is DungeonProp { Kind: PropKind.FrozenArea, Open: false } prop && prop.Covers(position))return true;
        return false;
    }
}
