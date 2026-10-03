using Godot;
namespace LastEmber;

// Knight uses articulated armor; hunter and priest retain the six-pose atlas.
public partial class PilgrimVisual : Node2D
{
    public EnemyKind Kind {get;set;}=EnemyKind.AshKnight;
    private Texture2D _atlas=null!;
    private ShaderMaterial _material=null!;
    private KnightRigVisual? _rig;
    public event System.Action? FootPlanted;
    private float _time,_gait,_speed,_windup,_release=10,_hurt,_death=-1;
    private Vector2 _facing=Vector2.Right;
    public static float IdleBottom(EnemyKind kind)=>kind==EnemyKind.AshKnight?.89f:kind==EnemyKind.AshHunter?.985f:.95f;
    public static string Asset(EnemyKind kind)=>"res://Assets/Enemies/ash-"+(kind==EnemyKind.AshHunter?"hunter":kind==EnemyKind.AshPriest?"priest":"knight")+"-atlas-v1.png";
    public override void _Ready()
    {
        if(Kind==EnemyKind.AshKnight){_rig=new KnightRigVisual();_rig.FootPlanted+=()=>FootPlanted?.Invoke();AddChild(_rig);return;}
        _atlas=GD.Load<Texture2D>(Asset(Kind));
        _material=new ShaderMaterial {Shader=GD.Load<Shader>("res://Assets/Shaders/pilgrim_pose.gdshader")};
        Material=_material;TextureFilter=TextureFilterEnum.Linear;
        _material.SetShaderParameter("attack_width",Kind==EnemyKind.AshHunter?1f:1.12f);
        _material.SetShaderParameter("idle_bottom",IdleBottom(Kind));
        _material.SetShaderParameter("windup_top",Kind==EnemyKind.AshKnight?.11f:Kind==EnemyKind.AshPriest?.025f:0f);
    }
    public void UpdatePose(Vector2 velocity,Vector2 facing,float windup,float releaseAge,float hurt)
    {
        _speed=velocity.Length();if(facing.LengthSquared()>.01f)_facing=facing;
        _windup=windup;_release=releaseAge;_hurt=hurt;
        _rig?.UpdatePose(velocity,facing,windup,releaseAge,hurt);
    }
    public void ReactToHit(Vector2 direction)=>_rig?.ReactToHit(direction);
    public void Die(){_death=0;_speed=0;_windup=0;ZIndex=7;_rig?.Die();}
    public override void _Process(double delta)=>Advance((float)delta);
    public void Advance(float dt)
    {
        if(_rig!=null){_rig.Advance(dt);if(_death>=0){_death+=dt;if(_death>.68f)QueueFree();}return;}
        _time+=dt;_gait+=_speed*dt/38;
        float a=0,b=0,blend=0;
        if(_speed>8){a=1;b=2;blend=(Mathf.Sin(_gait*Mathf.Pi)+1)*.5f;blend=Mathf.SmoothStep(.2f,.8f,blend);}
        if(_windup>0){a=0;b=3;blend=Mathf.SmoothStep(0,.32f,_windup);}
        else if(_release<.38f){a=4;b=0;blend=Mathf.SmoothStep(.15f,.38f,_release);}
        if(_hurt>0){a=b=5;blend=0;}
        float opacity=1;
        if(_death>=0){_death+=dt;a=b=5;opacity=1-Mathf.Clamp(_death/.48f,0,1);if(_death>=.48f){QueueFree();return;}}
        _material.SetShaderParameter("frame_a",a);_material.SetShaderParameter("frame_b",b);
        _material.SetShaderParameter("pose_blend",blend);_material.SetShaderParameter("clock",_time);
        _material.SetShaderParameter("flash",Mathf.Clamp(_hurt/.13f,0,1));_material.SetShaderParameter("opacity",opacity);
        Rotation=_windup>0?-.05f*_windup:_release<.25f?.07f*Mathf.Exp(-_release*15):Mathf.Sin(_gait*Mathf.Pi)*.018f*Mathf.Min(_speed/100,1);
        float breath=Mathf.Sin(_time*2.1f)*.008f;
        Scale=new Vector2(_facing.X<-.08f?-1:1,1+breath);
        if(_death>=0){Rotation+=_death*.8f;Scale*=new Vector2(1,1-_death*.6f);}
        QueueRedraw();
    }
    public override void _Draw()
    {
        if(_rig!=null)return;
        float size=Kind==EnemyKind.AshKnight?112:104;
        float anchor=Kind==EnemyKind.AshKnight?.9f:Kind==EnemyKind.AshHunter?.965f:.945f;
        DrawTextureRect(_atlas,new Rect2(-size*.5f,-size*anchor,size,size),false);
    }
    public override void _ExitTree()=>_material?.Dispose();
}
