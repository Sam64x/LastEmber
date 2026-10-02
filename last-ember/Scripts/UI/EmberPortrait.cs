using Godot;
namespace LastEmber;

public partial class EmberPortrait : Control
{
    public RunManager Run {get;set;}=null!;
    public override void _Ready(){MouseFilter=MouseFilterEnum.Ignore;TextureFilter=TextureFilterEnum.Linear;}
    public override void _Process(double delta){if(Run.Playing)QueueRedraw();}
    public override void _Draw()
    {
        if(!GodotObject.IsInstanceValid(Run.Player))return;
        var texture=PlayerVisual.Atlas;var cell=texture.GetSize()/new Vector2(3,2);
        int row=Run.Player.Flame.LastEmber?1:0;
        DrawTextureRectRegion(texture,new Rect2(Vector2.Zero,Size),new Rect2(new Vector2(0,row)*cell,cell));
    }
}
