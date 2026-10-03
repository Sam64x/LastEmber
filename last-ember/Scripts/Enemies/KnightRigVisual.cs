using Godot;
namespace LastEmber;

// A continuous body plate preserves proportions; connected limb meshes bend at joints.
public partial class KnightRigVisual : Node2D
{
    public const string Asset="res://Assets/Enemies/ash-knight-layers-v3.png";
    private static readonly Rect2[] Regions={new(265,8,215,333),new(655,10,223,331),new(1120,26,154,314),
        new(280,360,168,309),new(710,358,116,314),new(1152,359,127,315),
        new(295,682,98,333),new(681,716,177,266),new(1145,691,133,324)};
    private sealed class Limb
    {
        public readonly Polygon2D Node;
        private readonly Vector2[] _source=new Vector2[34],_points=new Vector2[34];
        private readonly Vector2 _root,_joint,_end;
        public Limb(Texture2D atlas,Rect2 region,Vector2 root,Vector2 joint,Vector2 end)
        {
            _root=root;_joint=joint;_end=end;Node=new Polygon2D {Texture=atlas};
            for(int row=0;row<=16;row++)for(int side=0;side<2;side++)_source[row*2+side]=region.Position+region.Size*new Vector2(side,row/16f);
            Node.UV=_source;Node.Polygons=Faces(16);
        }
        public void Pose(Vector2 root,Vector2 joint,Vector2 end)
        {
            Vector2 Map(Vector2 p,Vector2 s0,Vector2 s1,Vector2 t0,Vector2 t1)
            {
                var source=s1-s0;var target=t1-t0;
                return t0+(p-s0).Rotated(target.Angle()-source.Angle())*(target.Length()/source.Length());
            }
            for(int i=0;i<_source.Length;i++)
            {
                float weight=Mathf.SmoothStep(_joint.Y-22,_joint.Y+22,_source[i].Y);
                _points[i]=Map(_source[i],_root,_joint,root,joint).Lerp(Map(_source[i],_joint,_end,joint,end),weight);
            }
            Node.Polygon=_points;
        }
    }
    private static Godot.Collections.Array Faces(int rows)
    {
        var faces=new Godot.Collections.Array();
        for(int row=0;row<rows;row++){int a=row*2;faces.Add(new[]{a,a+1,a+3});faces.Add(new[]{a,a+3,a+2});}
        return faces;
    }
    private Texture2D _atlas=null!;
    private Sprite2D _body=null!,_sword=null!,_shield=null!;
    private AtlasTexture _front=null!,_rear=null!;
    private Limb _swordArm=null!,_shieldArm=null!,_leftLeg=null!,_rightLeg=null!;
    private Polygon2D _cloth=null!;
    private readonly Vector2[] _clothPoints=new Vector2[26];
    private float _time,_phase,_motion,_yaw,_mirror=1,_clothSwing,_clothSpeed,_shieldLag;
    private float _windup,_release=10,_hurt,_impact,_death=-1;
    private Vector2 _velocity,_facing=Vector2.Right,_hitDirection=Vector2.Left;
    private bool _back;
    public event System.Action? FootPlanted;
    public override void _Ready()
    {
        _atlas=GD.Load<Texture2D>(Asset);TextureFilter=TextureFilterEnum.Linear;
        AtlasTexture Region(int i)=>new(){Atlas=_atlas,Region=Regions[i]};
        _front=Region(0);_rear=Region(1);
        _body=new Sprite2D {Texture=_front,Centered=false,ZIndex=2,Scale=Vector2.One*.18f};AddChild(_body);
        _sword=new Sprite2D {Texture=Region(6),Centered=false,Offset=new Vector2(-49,-52),Scale=Vector2.One*.145f,ZIndex=4};AddChild(_sword);
        _shield=new Sprite2D {Texture=Region(7),Centered=false,Offset=new Vector2(-89,-100),Scale=Vector2.One*.12f,ZIndex=4};AddChild(_shield);
        Limb Make(int i,Vector2 root,Vector2 joint,Vector2 end){var limb=new Limb(_atlas,Regions[i],root,joint,end);AddChild(limb.Node);return limb;}
        _swordArm=Make(2,new(1219,65),new(1152,187),new(1219,316));
        _shieldArm=Make(3,new(335,410),new(405,530),new(355,635));
        _leftLeg=Make(4,new(765,382),new(780,493),new(777,643));
        _rightLeg=Make(5,new(1215,382),new(1211,495),new(1190,642));
        _cloth=new Polygon2D {Texture=_atlas,ZIndex=3};AddChild(_cloth);
        var uv=new Vector2[26];
        for(int row=0;row<=12;row++)for(int side=0;side<2;side++)uv[row*2+side]=Regions[8].Position+Regions[8].Size*new Vector2(side,row/12f);
        _cloth.UV=uv;_cloth.Polygons=Faces(12);UpdateGeometry();
    }
    public void UpdatePose(Vector2 velocity,Vector2 facing,float windup,float release,float hurt)
    {
        _velocity=velocity;if(facing.LengthSquared()>.01f)_facing=facing.Normalized();
        _windup=windup;_release=release;_hurt=hurt;
    }
    public void ReactToHit(Vector2 direction){_hitDirection=direction.Normalized();_impact=1;}
    public void Die(){_death=0;}
    public void Advance(float delta)
    {
        float remaining=Mathf.Min(delta,.1f);
        while(remaining>0){float dt=Mathf.Min(remaining,1f/120);Step(dt);remaining-=dt;}
        UpdateGeometry();
    }
    private void Step(float dt)
    {
        _time+=dt;_impact=Mathf.MoveToward(_impact,0,dt*4.5f);
        _yaw=Mathf.LerpAngle(_yaw,_facing.Angle(),1-Mathf.Exp(-dt*12));
        if(Mathf.Cos(_yaw)<-.18f)_mirror=-1;else if(Mathf.Cos(_yaw)>.18f)_mirror=1;
        if(Mathf.Sin(_yaw)<-.4f)_back=true;else if(Mathf.Sin(_yaw)>-.12f)_back=false;
        bool moving=_windup<=0&&_release>=.48f&&_death<0&&_hurt<=0&&_velocity.Length()>4;
        _motion=Mathf.Lerp(_motion,moving?Mathf.Min(_velocity.Length()/78,1):0,1-Mathf.Exp(-dt*12));
        if(moving)
        {
            float previous=_phase;_phase+=_velocity.Length()*dt/56;
            if(Mathf.FloorToInt(previous*2)!=Mathf.FloorToInt(_phase*2))FootPlanted?.Invoke();
            _phase=Mathf.PosMod(_phase,1);
        }
        float clothTarget=-_velocity.X*_mirror*.0007f+(_release<.22f?.16f:0);
        _clothSpeed+=((clothTarget-_clothSwing)*65-_clothSpeed*11)*dt;_clothSwing+=_clothSpeed*dt;
        _shieldLag=Mathf.Lerp(_shieldLag,_windup>0?-.12f:_release<.2f?.15f:0,1-Mathf.Exp(-dt*8));
        if(_death>=0)_death+=dt;
        float flash=1+Mathf.Clamp(_hurt/.13f,0,1)*.3f;
        Modulate=new Color(flash,flash,flash,_death<0?1:1-Mathf.Clamp((_death-.15f)/.5f,0,1));Scale=new Vector2(_mirror,1);
    }
    private static Vector2 Elbow(Vector2 root,Vector2 end,float first,float second,float bend)
    {
        var delta=end-root;float d=Mathf.Clamp(delta.Length(),.1f,first+second-.01f);var axis=delta.Normalized();
        float along=(first*first-second*second+d*d)/(2*d);
        return root+axis*along+axis.Orthogonal()*Mathf.Sqrt(Mathf.Max(0,first*first-along*along))*bend;
    }
    private void UpdateGeometry()
    {
        float stride=Mathf.Sin(_phase*Mathf.Tau)*_motion;
        float raise=_windup>0?Mathf.SmoothStep(0,.58f,_windup):0;
        float cut=_windup>.8f?Mathf.SmoothStep(.8f,1,_windup):0;
        float recover=_release<.48f?Mathf.SmoothStep(.1f,.48f,_release):1;
        if(_release<.48f){raise=1-recover;cut=1;}
        var hip=new Vector2(stride*.7f-raise*2+cut*raise*6,-39+Mathf.Abs(stride)*.7f+Mathf.Sin(_time*2)*.2f);
        hip+=new Vector2(_hitDirection.X*_mirror,_hitDirection.Y)*_impact*2.5f;
        if(_death>=0)hip+=new Vector2(_death*8,_death*24);
        float lean=-raise*.08f+cut*raise*.19f+stride*.009f+(_death>=0?_death*.18f:0);
        Vector2 Socket(float x,float y)=>hip+new Vector2(x,y).Rotated(lean);
        _body.Texture=_back?_rear:_front;
        _body.Offset=_back?Regions[1].Position-new Vector2(769,273):Regions[0].Position-new Vector2(375,273);
        _body.Position=hip;_body.Rotation=lean;
        var leftHip=Socket(-5.4f,.5f);var rightHip=Socket(5.4f,1.5f);
        var direction=new Vector2(_velocity.X*_mirror,_velocity.Y*.32f).Normalized();
        if(_velocity.LengthSquared()<1)direction=Vector2.Right;
        Vector2 Foot(int side)
        {
            float phase=Mathf.PosMod(_phase+side*.5f,1);
            float travel=phase<.6f?Mathf.Lerp(12,-12,phase/.6f):Mathf.Lerp(-12,12,Mathf.SmoothStep(0,1,(phase-.6f)/.4f));
            float lift=phase<.6f?0:Mathf.Sin((phase-.6f)/.4f*Mathf.Pi)*3.5f;
            var step=direction*travel*_motion;step.Y=Mathf.Clamp(step.Y,-4,4);
            return new Vector2(side==0?-15:14,side==0?1:-4)+step+Vector2.Up*lift*_motion;
        }
        var leftFoot=Foot(0);var rightFoot=Foot(1);
        _leftLeg.Pose(leftHip,leftHip.Lerp(leftFoot,.44f)+new Vector2(1.3f,0),leftFoot);
        _rightLeg.Pose(rightHip,rightHip.Lerp(rightFoot,.44f)+new Vector2(1.3f,0),rightFoot);
        _leftLeg.Node.ZIndex=leftFoot.Y<rightFoot.Y?0:1;_rightLeg.Node.ZIndex=leftFoot.Y<rightFoot.Y?1:0;
        var swordShoulder=Socket(-15,-25.5f);var shieldShoulder=Socket(14,-24.5f);
        var idleHand=Socket(-23,3+stride*1.2f);
        var liftedHand=Socket(-24,-46);var contactHand=Socket(10,-16);var followHand=Socket(16,-4);
        var swordHand=idleHand.Lerp(liftedHand,raise).Lerp(contactHand,raise*cut);
        if(_release<.48f)swordHand=contactHand.Lerp(followHand,Mathf.SmoothStep(0,.1f,_release)).Lerp(idleHand,recover);
        swordHand=swordShoulder+(swordHand-swordShoulder).LimitLength(32.8f);
        var shieldHand=Socket(23,3-raise*10)+new Vector2(_shieldLag*5,0);
        _swordArm.Pose(swordShoulder,Elbow(swordShoulder,swordHand,16,17,-1),swordHand);
        _shieldArm.Pose(shieldShoulder,Elbow(shieldShoulder,shieldHand,16,17,1),shieldHand);
        float angle=Mathf.Lerp(1.12f,-1.15f,raise);
        float attackAngle=new Vector2(_facing.X*_mirror,_facing.Y*.5f).Angle();
        angle=Mathf.LerpAngle(angle,attackAngle,cut*raise);
        if(_release<.48f)angle=Mathf.LerpAngle(attackAngle+Mathf.SmoothStep(0,.15f,_release)*.7f,1.12f,recover);
        _sword.Position=swordHand;_sword.Rotation=angle-Mathf.Pi/2;
        _shield.Position=shieldHand;_shield.Rotation=_shieldLag;
        _swordArm.Node.ZIndex=_shieldArm.Node.ZIndex=_back?1:3;
        _sword.ZIndex=_back?1:5;
        _shield.ZIndex=_back?1:raise>.5f?2:4;
        for(int row=0;row<=12;row++)for(int side=0;side<2;side++)
        {
            float t=row/12f;
            _clothPoints[row*2+side]=hip+new Vector2((side-.5f)*14+(_clothSwing*20+Mathf.Sin(_time*2.5f+t*3)*.5f)*t*t,-2+t*31);
        }
        _cloth.Polygon=_clothPoints;
    }
}
