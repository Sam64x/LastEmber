using Godot;
namespace LastEmber;

// Room-owned cosmetic layer. Independent seeded RNG; no lights, collision or damage.
public partial class RoomAtmosphere : Node2D
{
    public Room Room {get;set;}=null!;
    private RunManager Run=>Room.Run;
    private DungeonType _biome;
    private float _time;
    private struct Mote {public Vector2 Position;public float Size,Phase,Speed;}
    private readonly Mote[] _motes=new Mote[96];
    private readonly Vector2[] _scars=new Vector2[32];
    private readonly Vector2[] _mist=new Vector2[6];
    private Color Accent=>_biome switch
    {
        DungeonType.Frost=>new(.48f,.76f,.92f),DungeonType.Inferno=>new(.95f,.4f,.13f),_=>new(.48f,.43f,.42f)
    };
    public override void _Ready()
    {
        ZIndex=3; // Above the floor, below actors and warning telegraphs.
        _biome=Run.Dungeon.Definition.DungeonType;
        using var rng=new RandomNumberGenerator {Seed=Room.GeometrySeed^0xA71F04E5UL};
        for(int i=0;i<_motes.Length;i++)_motes[i]=new Mote
        {
            Position=Point(rng),Size=rng.RandfRange(1,2.7f),Phase=rng.RandfRange(0,Mathf.Tau),Speed=rng.RandfRange(.6f,1.4f)
        };
        for(int i=0;i<_scars.Length;i++)_scars[i]=FreePoint(rng);
        for(int i=0;i<_mist.Length;i++)_mist[i]=FreePoint(rng);
    }
    private Vector2 Point(RandomNumberGenerator rng)=>Room.Bounds.Position+new Vector2(rng.Randf(),rng.Randf())*Room.Bounds.Size;
    private Vector2 FreePoint(RandomNumberGenerator rng)
    {
        for(int i=0;i<80;i++){var point=Point(rng);if(Room.IsFree(point,24))return point;}
        return Room.EntrancePosition;
    }
    public override void _Process(double delta)
    {
        if(!Run.Playing)return;
        float dt=(float)delta;_time+=dt;
        for(int i=0;i<_motes.Length;i++)
        {
            var mote=_motes[i];
            var velocity=_biome switch
            {
                DungeonType.Frost=>new Vector2(16+Mathf.Sin(_time*.6f+mote.Phase)*9,19),
                DungeonType.Inferno=>new Vector2(Mathf.Sin(_time*1.4f+mote.Phase)*11,-34),
                _=>new Vector2(8+Mathf.Sin(_time*.4f+mote.Phase)*5,-5)
            };
            mote.Position+=velocity*dt*mote.Speed;
            var bounds=Room.Bounds;
            mote.Position=new Vector2(bounds.Position.X+Mathf.PosMod(mote.Position.X-bounds.Position.X,bounds.Size.X),
                bounds.Position.Y+Mathf.PosMod(mote.Position.Y-bounds.Position.Y,bounds.Size.Y));
            _motes[i]=mote;
        }
        QueueRedraw();
    }
    private bool VisibleAt(Vector2 point)=>Room.Bounds.Grow(-4).HasPoint(point)&&Room.IsFree(point,2)&&
        (_biome!=DungeonType.Darkness||Run.IsLit(point));
    public override void _Draw()
    {
        float intensity=Run.Visuals.Atmosphere;if(intensity<=0)return;
        var accent=Accent;
        for(int i=0;i<_scars.Length;i++)
        {
            var p=_scars[i];if(!VisibleAt(p))continue;
            var direction=Vector2.FromAngle(i*2.39996f);float size=14+i%5*7;
            var end=p+direction*size;
            if(!Room.IsFree(end,3))continue;
            if(_biome==DungeonType.Darkness)
            {
                DrawArc(p,9+i%3*4,direction.Angle(),direction.Angle()+2.2f,12,new Color(accent,.2f*intensity),1,true);
                DrawLine(p,end,new Color(accent,.16f*intensity),1,true);
            }
            else
            {
                DrawLine(p,end,new Color(accent,(_biome==DungeonType.Frost?.3f:.18f)*intensity),1,true);
                var branch=p+direction*size*.55f;
                var tip=branch+direction.Rotated(i%2==0?.7f:-.7f)*size*.45f;
                if(Room.IsFree(tip,3))DrawLine(branch,tip,new Color(accent,.2f*intensity),1,true);
                if(_biome==DungeonType.Frost)DrawCircle(p,3,new Color(accent,.13f*intensity));
            }
        }
        for(int i=0;i<_mist.Length;i++)
        {
            var center=_mist[i]+new Vector2(Mathf.Sin(_time*.14f+i)*25,Mathf.Cos(_time*.1f+i)*9);
            if(!VisibleAt(center))continue;
            // Soft, lit wisps stay near the ground; they never render above enemies.
            DrawSetTransform(center,0,new Vector2(2.2f,.45f));
            for(int layer=5;layer>=1;layer--)DrawCircle(Vector2.Zero,20+layer*11,new Color(accent,.009f*intensity));
            DrawSetTransform(Vector2.Zero,0,Vector2.One);
        }
        int count=Mathf.RoundToInt(_motes.Length*intensity);
        for(int i=0;i<count;i++)
        {
            var mote=_motes[i];if(!VisibleAt(mote.Position))continue;
            float opacity=(.3f+.2f*Mathf.Sin(_time*.8f+mote.Phase))*intensity;
            var color=new Color(accent,opacity);
            if(_biome==DungeonType.Frost)
            {
                for(int arm=0;arm<3;arm++)
                {
                    var offset=Vector2.FromAngle(arm*Mathf.Pi/3+mote.Phase)*mote.Size*1.5f;
                    DrawLine(mote.Position-offset,mote.Position+offset,color,1,true);
                }
            }
            else if(_biome==DungeonType.Inferno)
            {
                DrawLine(mote.Position+new Vector2(0,mote.Size*3),mote.Position,color,mote.Size,true);
                DrawCircle(mote.Position,mote.Size,new Color(1,.65f,.3f,opacity));
            }
            else DrawCircle(mote.Position,mote.Size*.7f,color);
        }
    }
}
