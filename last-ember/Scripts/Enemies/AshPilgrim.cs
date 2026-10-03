using Godot;
namespace LastEmber;

public partial class AshPilgrim : Enemy
{
    private PilgrimVisual _visual=null!;
    private GroundAttackTelegraph _warning=null!;
    private FlameSlashRibbon _slash=null!;
    private Vector2 _aim=Vector2.Right,_waypoint;
    private float _releaseAge=10,_pathClock,_footstep;
    private float Windup=>Kind==EnemyKind.AshKnight?.65f:Kind==EnemyKind.AshHunter?.85f:1.15f;
    public override void _Ready()
    {
        MaxHealth=Kind==EnemyKind.AshKnight?120:Kind==EnemyKind.AshHunter?65:80;
        ContactDamage=Kind==EnemyKind.AshKnight?14:Kind==EnemyKind.AshHunter?10:12;
        BodyRadius=Kind==EnemyKind.AshKnight?23:19;
        base._Ready();
        Material=new CanvasItemMaterial {LightMode=CanvasItemMaterial.LightModeEnum.Unshaded};
        _visual=new PilgrimVisual {Kind=Kind};AddChild(_visual);
        _warning=new GroundAttackTelegraph {Tint=new Color(.95f,.27f,.16f),ShotLength=520};AddChild(_warning);
        _slash=new FlameSlashRibbon();AddChild(_slash);
    }
    protected override void OnStaggered()
    {
        Telegraph=0;_releaseAge=Mathf.Max(_releaseAge,.4f);Cooldown=Mathf.Max(Cooldown,.8f);_warning?.Clear();
    }
    protected override void Behave(float dt)
    {
        Active=true;_releaseAge+=dt;
        var offset=Run.Player.Position-Position;float distance=offset.Length();
        var direction=offset.Normalized();bool sight=Run.Room.HasLineOfSight(Position,Run.Player.Position);
        SpeedNow=(Kind==EnemyKind.AshKnight?78:Kind==EnemyKind.AshHunter?112:72)*(Weakened>0?.4f:1);
        if(Telegraph>0)
        {
            direction=Vector2.Zero;Telegraph=Mathf.Max(0,Telegraph-dt);
            _warning.Progress=1-Telegraph/Windup;_warning.ShotOrigin=Position;_warning.QueueRedraw();
            if(Telegraph<=0){ReleaseAttack();_warning.Clear();}
        }
        else if(_releaseAge<.4f)direction=Vector2.Zero;
        else
        {
            _aim=direction;
            float reach=Kind==EnemyKind.AshKnight?100:Kind==EnemyKind.AshHunter?470:430;
            if(Cooldown<=0&&distance<reach&&sight)
            {
                Target=Run.Player.Position;Telegraph=Windup;direction=Vector2.Zero;_warning.Clear();
                if(Kind==EnemyKind.AshHunter){_warning.ShotOrigin=Position;_warning.ShotDirections.Add(_aim);_warning.Show();}
                if(Kind==EnemyKind.AshPriest){_warning.Areas.Add(new GroundAttackTelegraph.Area(Target,64));_warning.Show();}
                Run.Audio.PlayAt("warning",Position,.65f);
            }
            else if(Kind!=EnemyKind.AshKnight&&sight)
            {
                float preferred=Kind==EnemyKind.AshHunter?260:230;
                direction*=distance<preferred-40?-1:distance>preferred+40?1:0;
            }
        }
        if(direction!=Vector2.Zero)
        {
            if(!sight&&direction.Dot(offset.Normalized())>.5f)
            {
                _pathClock-=dt;
                if(_pathClock<=0||Position.DistanceTo(_waypoint)<22){_pathClock=.4f;_waypoint=Run.Room.Navigate(Position,Run.Player.Position);}
                direction=(_waypoint-Position).Normalized();
            }
            direction=Run.Room.Steer(Position,direction,BodyRadius);
        }
        Vector2 separation=Vector2.Zero;
        if(Telegraph<=0&&_releaseAge>=.4f)foreach(var other in Run.Enemies)
        {
            if(other==this||other.Dead)continue;var away=Position-other.Position;
            float minimum=BodyRadius+other.BodyRadius+6;
            if(away.LengthSquared()>.01f&&away.LengthSquared()<minimum*minimum)separation+=away.Normalized()*45;
        }
        Velocity=direction*SpeedNow+Knockback+separation;Knockback=Knockback.MoveToward(Vector2.Zero,dt*700);MoveAndSlide();
        var bounds=Run.Room.Bounds.Grow(-BodyRadius);Position=Position.Clamp(bounds.Position,bounds.End);
        _footstep-=dt;if(Velocity.LengthSquared()>900&&_footstep<=0){_footstep=Kind==EnemyKind.AshKnight?.68f:.48f;Run.Audio.PlayAt(Kind==EnemyKind.AshKnight?"heavy_step":"step",Position,.45f);}
    }
    private void ReleaseAttack()
    {
        _releaseAge=0;Cooldown=Kind==EnemyKind.AshKnight?1.55f:Kind==EnemyKind.AshHunter?1.9f:2.6f;
        float damage=ContactDamage*(Weakened>0?.4f:1);
        if(Kind==EnemyKind.AshKnight)
        {
            if(Combat.InArc(Position,_aim,Run.Player.Position,106,.5f)&&Run.Room.HasLineOfSight(Position,Run.Player.Position))Run.Player.TakeDamage(new DamageInfo(damage,Position,170));
            Run.Audio.PlayAt("hit",Position,.5f);
        }
        else if(Kind==EnemyKind.AshHunter)
        {
            Run.Shoot(Position,Position+_aim*500,360,damage,false,new Color(.9f,.5f,.34f));Run.Audio.PlayAt("watcher_shot",Position,.7f);
        }
        else
        {
            // The fixed warning disk and damage share the exact same center/radius.
            if(Run.Room.HasLineOfSight(Position,Target))
            {
                Run.Fx.Splash(Target,64);
                if(Target.DistanceTo(Run.Player.Position)<64&&Run.Room.HasLineOfSight(Target,Run.Player.Position))Run.Player.TakeDamage(new DamageInfo(damage,Target,130));
            }
        }
    }
    public override void _Process(double delta)
    {
        if(Dead||!Run.Playing)return;
        _visual.UpdatePose(GetRealVelocity(),_aim,Telegraph>0?1-Telegraph/Windup:0,_releaseAge,Flash);
        if(Kind==EnemyKind.AshKnight&&_releaseAge<.3f)
            _slash.Configure(Vector2.Zero,_aim,-1,1,.78f,22,1-_releaseAge/.3f,_releaseAge,0,0,false,.7f,_releaseAge/.3f);
        else _slash.Hide();
    }
    public override void TakeDamage(DamageInfo hit)
    {
        if(Dead)return;base.TakeDamage(hit);
        if(!Dead)return;_warning.Clear();_visual.Reparent(Run.Room,true);_visual.Die();
    }
    public override void _Draw()
    {
        DrawSetTransform(new Vector2(0,4),0,new Vector2(1,.42f));DrawCircle(Vector2.Zero,BodyRadius,new Color(0,0,0,.5f));DrawSetTransform(Vector2.Zero,0,Vector2.One);
        if(Telegraph>0&&Kind==EnemyKind.AshKnight)
        {
            float angle=_aim.Angle();DrawArc(Vector2.Zero,106,angle-Mathf.Pi/3,angle+Mathf.Pi/3,28,new Color(1,.27f,.16f,.9f),2,true);
            DrawLine(Vector2.Zero,_aim.Rotated(-Mathf.Pi/3)*106,new Color(1,.27f,.16f,.4f),1,true);
            DrawLine(Vector2.Zero,_aim.Rotated(Mathf.Pi/3)*106,new Color(1,.27f,.16f,.4f),1,true);
        }
        if(Health<MaxHealth){DrawRect(new Rect2(-22,-96,44,4),new Color(.12f,.08f,.1f));DrawRect(new Rect2(-22,-96,44*Health/MaxHealth,4),new Color(.9f,.36f,.22f));}
    }
}
