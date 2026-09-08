using System.Collections.Generic;
using Godot;

namespace LastEmber;

// Only static masonry is remembered. Actors, traps, loot and gate state never enter this layer.
public partial class WallMemory : Node2D
{
    public Room Room {get;set;}=null!;
    private sealed class Segment { public Vector2 A,B; public bool Seen; }
    private readonly List<Segment> _segments=new();
    private float _clock;
    public override void _Ready()
    {
        Material=new CanvasItemMaterial {LightMode=CanvasItemMaterial.LightModeEnum.Unshaded};
        AddOutline(Room.Bounds);
        foreach(var rect in Room.Obstacles)AddOutline(rect);
    }
    private void AddOutline(Rect2 rect)
    {
        var a=rect.Position;var b=new Vector2(rect.End.X,rect.Position.Y);
        var c=rect.End;var d=new Vector2(rect.Position.X,rect.End.Y);
        AddEdge(a,b);AddEdge(b,c);AddEdge(c,d);AddEdge(d,a);
    }
    private void AddEdge(Vector2 a,Vector2 b)
    {
        int count=Mathf.CeilToInt(a.DistanceTo(b)/16);
        for(int i=0;i<count;i++)_segments.Add(new Segment {A=a.Lerp(b,(float)i/count),B=a.Lerp(b,(float)(i+1)/count)});
    }
    public override void _Process(double delta)
    {
        if(!Room.Run.Playing)return;
        _clock-=(float)delta;if(_clock>0)return;_clock=.06f;
        foreach(var segment in _segments)
            if(!segment.Seen&&Room.Run.IsLit((segment.A+segment.B)*.5f))segment.Seen=true;
        QueueRedraw();
    }
    public override void _Draw()
    {
        foreach(var segment in _segments)
            if(segment.Seen&&!Room.Run.IsLit((segment.A+segment.B)*.5f))
                DrawLine(segment.A,segment.B,new Color(.045f,.047f,.053f),2);
    }
}
