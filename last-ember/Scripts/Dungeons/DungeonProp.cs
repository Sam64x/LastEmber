using Godot;
namespace LastEmber;
public enum PropKind { FrozenDoor, IceWall, FrozenTreasure, TrappedEnemy, FrozenArea, FireTrap, BurningFloor, IceHazard }

// Place these reusable props in any room or environment PackedScene.
public partial class DungeonProp : Node2D, IDungeonReactive
{
    [Export] public PropKind Kind { get; set; }
    [Export] public Vector2 Size { get; set; } = new(72,72);
    [Export] public EnemyKind Captive { get; set; } = EnemyKind.Shade;
    [Export] public float ReturnSeconds { get; set; } = 5;
    [Export] public float Period { get; set; } = 3;
    [Export] public string Label { get; set; } = "";
    public bool Open { get; private set; }
    public bool Burning => !Open && (Kind==PropKind.BurningFloor || Kind==PropKind.FireTrap && _clock%Period>1);
    private RunManager _run=null!;
    private CollisionShape2D? _shape;
    private Rect2 _obstacle;
    private float _remaining,_clock,_tick;
    public bool Covers(Vector2 point)=>new Rect2(GlobalPosition-Size/2,Size).HasPoint(point);
    public override void _Ready()
    {
        Node? ancestor=GetParent();while(ancestor!=null && ancestor is not Room)ancestor=ancestor.GetParent();
        _run=((Room)ancestor!).Run;AddToGroup("dungeon_reactive");ZIndex=14;
        Material=new CanvasItemMaterial {LightMode=CanvasItemMaterial.LightModeEnum.Unshaded};
        if(Kind is PropKind.FrozenDoor or PropKind.IceWall or PropKind.FrozenTreasure or PropKind.TrappedEnemy or PropKind.IceHazard)
        {
            var body=new StaticBody2D {CollisionLayer=1,CollisionMask=6};
            _shape=new CollisionShape2D {Shape=new RectangleShape2D {Size=Size}};body.AddChild(_shape);AddChild(body);
            _obstacle=new Rect2(GlobalPosition-Size/2,Size);_run.Room.Obstacles.Add(_obstacle);_run.Room.RefreshNavigation();
        }
    }
    public void React(DungeonImpact impact,float seconds,Vector2 origin,bool blue)
    {
        if(impact==DungeonImpact.Heat && Kind<=PropKind.FrozenArea || impact==DungeonImpact.Heat && Kind==PropKind.IceHazard)
        {
            if(Open)return;Open=true;
            if(_shape!=null){_shape.SetDeferred(CollisionShape2D.PropertyName.Disabled,true);_run.Room.Obstacles.Remove(_obstacle);_run.Room.RefreshNavigation();}
            if(Kind==PropKind.FrozenTreasure)_run.Room.AddChild(new EmberPickup {Run=_run,Position=GlobalPosition,Amount=12});
            if(Kind==PropKind.TrappedEnemy)_run.Spawn(Captive,GlobalPosition);
            if(Kind==PropKind.IceHazard){_remaining=.7f;_run.Audio.Play("warning");}
            if(Kind==PropKind.FrozenArea)_remaining=seconds;
            _run.Fx.Sparks(GlobalPosition,new Color(.65f,.9f,1),16);
        }
        if(impact==DungeonImpact.Suction && Kind is PropKind.FireTrap or PropKind.BurningFloor)
        {Open=true;_remaining=Mathf.Max(_remaining,seconds);}
        QueueRedraw();
    }
    public override void _PhysicsProcess(double delta)
    {
        if(!_run.Playing)return;float dt=(float)delta;_clock+=dt;_tick-=dt;
        if(_remaining>0)
        {
            _remaining-=dt;
            if(_remaining<=0)
            {
                if(Kind==PropKind.IceHazard)
                {
                    _run.Fx.Ring(GlobalPosition,125,new Color(.7f,.9f,1));
                    if(_run.Player.Position.DistanceTo(GlobalPosition)<125)_run.Player.TakeDamage(new DamageInfo(12,GlobalPosition));
                }
                else Open=false;
            }
        }
        if(Burning && _tick<=0)
        {
            _tick=.65f;
            if(Covers(_run.Player.Position))_run.Player.TakeDamage(new DamageInfo(8,GlobalPosition));
            if(Kind==PropKind.FireTrap)_run.Shoot(GlobalPosition,_run.Player.Position,260,8,true);
        }
        QueueRedraw();
    }
    public override void _Draw()
    {
        var rect=new Rect2(-Size/2,Size);
        bool fire=Kind is PropKind.FireTrap or PropKind.BurningFloor;
        var color=fire?(Burning?new Color(1,.32f,.06f,.6f):Open?new Color(.25f,.7f,.9f,.32f):new Color(.28f,.17f,.15f,.65f)):new Color(.45f,.8f,1,Open?.08f:.55f);
        if(Open&&!fire&&Kind!=PropKind.FrozenArea)
        {
            for(int i=0;i<6;i++)DrawLine(new Vector2(-Size.X/2+i*Size.X/6,Size.Y/3),new Vector2(-Size.X/2+i*Size.X/6+6,Size.Y/3+5),new Color(.6f,.85f,1,.5f),2);
            return;
        }
        DrawRect(rect,color);DrawRect(rect,color.Lightened(.25f),false,2);
        if(Burning)
            for(int i=0;i<7;i++)
            {
                var p=new Vector2(-Size.X*.4f+i*Size.X*.13f,Mathf.Sin(_clock*5+i)*Size.Y*.3f);
                DrawColoredPolygon(new[]{p+new Vector2(-6,7),p+new Vector2(0,-14-Mathf.Sin(_clock*9+i)*7),p+new Vector2(6,7)},new Color(1,.65f,.15f,.85f));
            }
        if(!fire&&!Open)
        {
            DrawLine(rect.Position,Vector2.Zero,Colors.White,2);DrawLine(Vector2.Zero,new Vector2(Size.X/2,-Size.Y/3),Colors.White,2);
            if(Kind==PropKind.TrappedEnemy)DrawCircle(Vector2.Zero,16,new Color(.3f,.15f,.4f));
            if(Kind==PropKind.FrozenTreasure)DrawRect(new Rect2(-16,-12,32,24),new Color(1,.7f,.2f));
        }
        if(fire && Open)DrawArc(Vector2.Zero,Size.X*.4f,0,Mathf.Tau*Mathf.Clamp(_remaining/3,0,1),32,new Color(.6f,.9f,1),3);
        DrawString(ThemeDB.FallbackFont,new Vector2(-Size.X/2,-Size.Y/2-8),Label,HorizontalAlignment.Left,-1,14,Colors.White);
    }
}
