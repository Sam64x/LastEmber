using System.Collections.Generic;
using Godot;

namespace LastEmber;

public partial class Room : Node2D
{
    public static readonly Rect2 Interior = new(96, 160, 1728, 792);
    public RunManager Run { get; set; } = null!;
    [Export] public bool LayoutGeometry { get; set; } = true;
    public int Layout { get; set; }
    public RoomDefinition? Definition { get; set; }
    public ulong GeometrySeed { get; set; }
    public bool BossArena { get; set; }
    public bool Cleared { get; set; }
    public Rect2 Bounds => Definition?.Bounds ?? (BossArena ? Interior : new Rect2(360,296,1200,520));
    public Vector2 EntrancePosition => new(Bounds.Position.X+90,Bounds.GetCenter().Y);
    public Vector2 ExitPosition => new(Bounds.End.X-24,Bounds.GetCenter().Y);
    public Vector2 ShrinePosition => new(Bounds.End.X-130,Bounds.GetCenter().Y);
    public Vector2 MapPoint(Vector2 point) => BossArena?point:Bounds.Position+(point-Interior.Position)*Bounds.Size/Interior.Size;
    public List<Rect2> Obstacles { get; } = new();
    public List<EmberLight> Torches { get; } = new();
    private readonly List<Vector2> _chips = new();
    private readonly RandomNumberGenerator _rng = new();
    private readonly AStarGrid2D _navigation = new();
    private static readonly float[] AvoidanceAngles = {.65f,-.65f,1.2f,-1.2f,1.57f,-1.57f,2.1f,-2.1f};
    private int _torchTarget=4;
    private float _torchDelay;
    private static Rect2 R(float x, float y, float w, float h) => new(x,y,w,h);
    public static Rect2[] LayoutObstacles(int index) => (index % 8) switch
    {
        0 => new[] {R(560,340,95,110),R(1265,340,95,110),R(560,700,95,100),R(1265,700,95,100)},
        1 => new[] {R(600,390,110,310),R(1210,390,110,310)},
        2 => new[] {R(850,390,220,95),R(850,655,220,95)},
        3 => new[] {R(470,380,220,85),R(1230,640,220,85),R(850,520,130,100)},
        4 => new[] {R(530,290,85,220),R(1305,610,85,220),R(930,450,85,180)},
        5 => new[] {R(470,470,210,100),R(1230,470,210,100)},
        6 => new[] {R(660,340,90,100),R(1170,340,90,100),R(660,730,90,100),R(1170,730,90,100)},
        _ => new[] {R(760,430,100,230),R(1090,430,100,230)}
    };
    public override void _Ready()
    {
        ZIndex = -10;
        _rng.Seed = Definition!=null?GeometrySeed:(ulong)(Layout + 991);
        for (int i = 0; i < 180; i++) _chips.Add(new Vector2(_rng.RandfRange(Bounds.Position.X+10,Bounds.End.X-10),_rng.RandfRange(Bounds.Position.Y+10,Bounds.End.Y-10)));
        AddWall(R(Bounds.Position.X-26,Bounds.Position.Y-26,Bounds.Size.X+52,26));
        AddWall(R(Bounds.Position.X-26,Bounds.End.Y,Bounds.Size.X+52,26));
        AddWall(R(Bounds.Position.X-26,Bounds.Position.Y,26,Bounds.Size.Y));
        AddWall(R(Bounds.End.X,Bounds.Position.Y,26,Bounds.Size.Y));
        if(Definition!=null && !BossArena)
            foreach(var rect in Definition.Solids){Obstacles.Add(rect);AddWall(rect);}
        else if (LayoutGeometry && !BossArena && Run.StageIndex>0 && Run.CurrentStage is not (StageKind.Altar or StageKind.Reward)) foreach (var rect in LayoutObstacles(Layout))
        { var compact=new Rect2(MapPoint(rect.Position),rect.Size*Bounds.Size/Interior.Size);Obstacles.Add(compact);AddWall(compact); }
        AddChild(new WallMemory {Room=this,ZIndex=1});
        if(Definition==null && !BossArena && Run.StageIndex>=3 && Run.CurrentStage==StageKind.Combat && Run.Dungeon.Definition.AshTraps)
        {
            foreach(var point in new[]{MapPoint(new Vector2(780,330)),MapPoint(new Vector2(1160,780))})
                if(IsFree(point,35))AddChild(new AshTrap {Run=Run,Position=point,ZIndex=12});
        }
        _navigation.Region = new Rect2I(0,0,36,16);
        _navigation.CellSize = new Vector2(48,48);
        _navigation.Offset = new Vector2(120,184);
        _navigation.DiagonalMode = AStarGrid2D.DiagonalModeEnum.OnlyIfNoObstacles;
        _navigation.Update();
        for(int y=0;y<16;y++)for(int x=0;x<36;x++)
            _navigation.SetPointSolid(new Vector2I(x,y),!IsFree(new Vector2(120+x*48,184+y*48),36));
        if (BossArena)
        {
            foreach (var position in new[] {new Vector2(340,320),new Vector2(1580,320),new Vector2(340,795),new Vector2(1580,795)})
            {
                var light = new EmberLight { Position = position, TargetRadius = 340, Run=Run, Flicker=true };
                AddChild(light); Torches.Add(light); Run.Lights.Add(light);
            }
        }
        if(Run.CurrentRoomPlan!=null)AddChild(new RoomAtmosphere {Room=this});
    }
    public void RefreshNavigation()
    {
        for(int y=0;y<16;y++)for(int x=0;x<36;x++)
            _navigation.SetPointSolid(new Vector2I(x,y),!IsFree(new Vector2(120+x*48,184+y*48),36));
        QueueRedraw();
    }
    private void AddWall(Rect2 rect)
    {
        var body = new StaticBody2D { Position = rect.GetCenter(), CollisionLayer = 1, CollisionMask = 6 };
        body.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = rect.Size } });
        AddChild(body);
    }
    public void SetTorchPhase(int phase)
    {
        _torchTarget=phase==1?4:phase==2?2:0;
        ExtinguishOne();_torchDelay=1.5f;
    }
    private void ExtinguishOne()
    {
        int lit=0;foreach(var torch in Torches)if(torch.Lit)lit++;
        if(lit<=_torchTarget)return;
        foreach(var torch in Torches)if(torch.Lit)
        {
            torch.Lit=false;Run.Audio.PlayAt("extinguish",torch.Position,.85f);
            Run.Fx.Sparks(torch.Position,new Color(.40f,.38f,.40f),12);break;
        }
        QueueRedraw();
    }
    public override void _Process(double delta)
    {
        _torchDelay-=(float)delta;
        if(BossArena&&_torchDelay<=0){ExtinguishOne();_torchDelay=1.5f;}
    }
    public bool IsFree(Vector2 position, float margin = 45)
    {
        if (!Bounds.Grow(-margin).HasPoint(position)) return false;
        foreach (var obstacle in Obstacles) if (obstacle.Grow(margin).HasPoint(position)) return false;
        return true;
    }
    public IEnumerable<Vector2> FreePositions(float margin)
    {
        for(float y=Bounds.Position.Y+margin;y<Bounds.End.Y-margin;y+=24)
            for(float x=Bounds.Position.X+margin;x<Bounds.End.X-margin;x+=24)
                if(IsFree(new Vector2(x,y),margin))yield return new Vector2(x,y);
    }
    public Vector2 FindFreePosition(Vector2 normalized, float margin=48)
    {
        var target=Bounds.Position+normalized*Bounds.Size;
        if(IsFree(target,margin))return target;
        Vector2? best=null;float distance=float.MaxValue;
        foreach(var p in FreePositions(margin))
        {
            float d=p.DistanceSquaredTo(target);
            if(d<distance){best=p;distance=d;}
        }
        return best??throw new System.InvalidOperationException($"No free socket in {Definition?.Id}");
    }
    public bool HasLineOfSight(Vector2 a, Vector2 b)
    {
        foreach (var rect in Obstacles)
        {
            if (rect.HasPoint(a) || rect.HasPoint(b)) return false;
            var p = rect.Position; var e = rect.End;
            if (Geometry2D.SegmentIntersectsSegment(a,b,p,new Vector2(e.X,p.Y)).VariantType != Variant.Type.Nil ||
                Geometry2D.SegmentIntersectsSegment(a,b,new Vector2(e.X,p.Y),e).VariantType != Variant.Type.Nil ||
                Geometry2D.SegmentIntersectsSegment(a,b,e,new Vector2(p.X,e.Y)).VariantType != Variant.Type.Nil ||
                Geometry2D.SegmentIntersectsSegment(a,b,new Vector2(p.X,e.Y),p).VariantType != Variant.Type.Nil) return false;
        }
        return true;
    }
    public Vector2 Steer(Vector2 position, Vector2 direction, float radius)
    {
        if (IsFree(position + direction * 58, radius + 8)) return direction;
        // Sample both sides to flow around handcrafted pillars. All layouts have wide connected lanes.
        foreach (float angle in AvoidanceAngles)
        {
            var candidate = direction.Rotated(angle);
            if (IsFree(position + candidate * 68, radius + 6)) return candidate;
        }
        return Vector2.Zero;
    }
    public Vector2 Navigate(Vector2 from,Vector2 to)
    {
        var start=NearestCell(from);var goal=NearestCell(to);
        var path=_navigation.GetPointPath(start,goal);
        if(path.Length<2)return to;
        return path[1];
    }
    private Vector2I NearestCell(Vector2 position)
    {
        var cell=new Vector2I(Mathf.Clamp(Mathf.RoundToInt((position.X-120)/48),0,35),Mathf.Clamp(Mathf.RoundToInt((position.Y-184)/48),0,15));
        if(!_navigation.IsPointSolid(cell))return cell;
        Vector2I best=cell;float distance=float.MaxValue;
        for(int y=0;y<16;y++)for(int x=0;x<36;x++)
        {
            var candidate=new Vector2I(x,y);if(_navigation.IsPointSolid(candidate))continue;
            float d=(new Vector2(120+x*48,184+y*48)-position).LengthSquared();
            if(d<distance){distance=d;best=candidate;}
        }
        return best;
    }
    public override void _ExitTree(){_navigation.Dispose();}
    public override void _Draw()
    {
        DrawRect(new Rect2(0,0,1920,1080),Colors.Black);
        DrawRect(Bounds.Grow(20),new Color(.18f,.16f,.16f));
        DrawRect(Bounds,new Color(.105f,.112f,.128f));
        for (int y = (int)Bounds.Position.Y; y < Bounds.End.Y; y += 66)
            for (int x = (int)Bounds.Position.X; x < Bounds.End.X; x += 96)
            {
                float tone = ((x / 96 + y / 66 + Layout) % 4) * .007f;
                DrawRect(new Rect2(x+2,y+2,Mathf.Min(92,Bounds.End.X-x-2),Mathf.Min(62,Bounds.End.Y-y-2)),new Color(.12f+tone,.126f+tone,.141f+tone));
                DrawLine(new Vector2(x+3,y+3),new Vector2(Mathf.Min(x+90,Bounds.End.X-2),y+3),new Color(.17f,.17f,.18f),1);
            }
        DrawRect(Bounds.Grow(-21),new Color(.27f,.23f,.20f),false,2);
        DrawRect(Bounds.Grow(-27),new Color(.17f,.16f,.17f),false,1);
        foreach (var chip in _chips) DrawLine(chip,chip+new Vector2(7,3),new Color(.22f,.21f,.20f,.5f),1);
        var center = Bounds.GetCenter();
        DrawArc(center,230,0,Mathf.Tau,96,new Color(.24f,.21f,.20f,.48f),3,true);
        DrawArc(center,218,0,Mathf.Tau,96,new Color(.21f,.19f,.19f,.48f),1,true);
        for(int i=0;i<12;i++)
        {
            var dir=Vector2.FromAngle(i*Mathf.Tau/12);
            DrawLine(center+dir*205,center+dir*222,new Color(.31f,.25f,.20f,.55f),3);
        }
        foreach (var rect in Obstacles)
        {
            DrawRect(new Rect2(rect.Position+new Vector2(10,18),rect.Size),new Color(0,0,0,.65f));
            DrawRect(rect,new Color(.18f,.19f,.22f));
            DrawRect(new Rect2(rect.Position-new Vector2(0,12),rect.Size),new Color(.28f,.28f,.30f));
            DrawRect(new Rect2(rect.Position+new Vector2(7,-5),rect.Size-new Vector2(14,14)),new Color(.22f,.22f,.24f));
            DrawLine(rect.Position-new Vector2(0,12),rect.Position+new Vector2(rect.Size.X,-12),new Color(.4f,.37f,.32f),3);
        }
        var gate = new Rect2(Bounds.End.X-19,center.Y-66,35,132);
        DrawRect(gate,new Color(.12f,.09f,.10f));
        for(int i=0;i<5;i++) DrawLine(new Vector2(gate.Position.X+5+i*6,gate.Position.Y+2),new Vector2(gate.Position.X+5+i*6,gate.End.Y-2),Cleared?new Color(1,.58f,.2f):new Color(.36f,.25f,.24f),3);
        if (Cleared) { DrawArc(ExitPosition-new Vector2(18,0),37,-1.2f,1.2f,24,new Color(1,.62f,.25f),3,true); }
        if(Run.ShrineAvailable || Run.CurrentRoomPlan?.Type==RoomType.RiskReward)
        {
            var p=ShrinePosition;
            DrawRect(new Rect2(p-new Vector2(28,15),new Vector2(56,38)),new Color(.35f,.26f,.2f));
            DrawRect(new Rect2(p-new Vector2(30,24),new Vector2(60,14)),new Color(.55f,.39f,.24f));
            DrawLine(p-new Vector2(0,24),p+new Vector2(0,22),new Color(.8f,.58f,.3f),4);
            DrawCircle(p-new Vector2(0,34),8,new Color(1,.65f,.3f));
        }
        foreach(var torch in Torches)
        {
            var p=torch.Position;
            DrawCircle(p+new Vector2(0,14),24,new Color(0,0,0,.6f));
            DrawRect(new Rect2(p-new Vector2(9,5),new Vector2(18,32)),new Color(.36f,.29f,.23f));
            DrawCircle(p,16,torch.Lit?new Color(1,.52f,.12f):new Color(.22f,.20f,.23f));
            if(torch.Lit) DrawCircle(p-new Vector2(0,5),7,new Color(1,.9f,.55f));
        }
        if (Run.CurrentStage == StageKind.Altar)
        {
            DrawColoredPolygon(new[] {center+new Vector2(0,-70),center+new Vector2(65,25),center+new Vector2(0,65),center+new Vector2(-65,25)},new Color(.33f,.24f,.28f));
            DrawArc(center,90,0,Mathf.Tau,64,new Color(.75f,.31f,.2f),3);
            DrawCircle(center-new Vector2(0,12),15,new Color(1,.55f,.23f));
        }
    }
}
