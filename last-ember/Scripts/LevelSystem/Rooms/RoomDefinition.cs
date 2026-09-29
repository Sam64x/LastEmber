using Godot;
namespace LastEmber;

// Geometry only. Coordinates are in the existing 1920 x 1080 world space.
[GlobalClass]
public partial class RoomDefinition : Resource
{
    [Export] public string Id { get; set; } = "hall";
    [Export] public Rect2 Bounds { get; set; } = new(300, 260, 1320, 600);
    [Export] public Godot.Collections.Array<Rect2> Solids { get; set; } = new();
    public static RoomDefinition[] CreateCatalog() => new[]
    {
        Make("long-gallery", new(240,340,1440,440), new(690,340,90,110),new(1140,670,90,110)),
        Make("crossroads", new(300,230,1320,660), new(300,230,340,200),new(1280,230,340,200),new(300,690,340,200),new(1280,690,340,200)),
        Make("split-court", new(270,240,1380,640), new Rect2(855,425,210,270)),
        Make("staggered-halls", new(300,230,1320,660), new(650,230,140,340),new(1120,550,140,340)),
        Make("sunken-chapel", new(340,210,1240,700), new(340,210,220,210),new(1360,210,220,210),new(760,700,100,100),new(1060,700,100,100)),
        Make("twin-courts", new(240,260,1440,600), new(880,260,160,200),new(880,660,160,200)),
        Make("broken-ring", new(280,220,1360,680), new(740,390,100,260),new(1080,470,100,260)),
        Make("pillared-vault", new(300,250,1320,620), new(630,340,95,110),new(1195,340,95,110),new(630,670,95,110),new(1195,670,95,110))
    };
    private static RoomDefinition Make(string id, Rect2 bounds, params Rect2[] solids)
    {
        var room = new RoomDefinition { Id=id, Bounds=bounds };
        foreach(var solid in solids)room.Solids.Add(solid);
        return room;
    }
}
