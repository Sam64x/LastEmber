using Godot;
namespace LastEmber;

// Rigid armor segments driven by continuous joint positions. Cloth alone deforms.
// All positions are character-local, with the feet at y=0; facing mirrors the rig.
public partial class KnightRigVisual : Node2D
{
    public const string Asset="res://Assets/Enemies/ash-knight-parts-v2.png";
    private Texture2D _atlas=null!;
    private static readonly Rect2[] Regions={
        new(66,41,190,264),new(341,31,258,282),new(658,58,276,260),new(1043,18,139,302),
        new(48,322,219,287),new(371,327,231,283),new(684,332,195,273),new(1027,320,171,294),
        new(61,634,210,268),new(380,631,193,259),new(725,629,128,273),new(1022,628,154,266),
        new(117,905,82,328),new(384,920,169,299),new(659,915,242,311),new(950,914,277,323)
    };
    private readonly Vector2[] _feet={new(-11,0),new(11,1)};
    private readonly Vector2[] _swingStart={new(-11,0),new(11,1)};
    private readonly bool[] _swing=new bool[2];
    private float _time,_phase,_motion,_yaw,_mirror=1,_cloth,_clothVelocity,_shieldLag;
    private float _windup,_release=10,_hurt,_death=-1,_impact;
    private Vector2 _velocity,_facing=Vector2.Right,_hitDirection=Vector2.Left;
    private bool _back;
    public bool DrawBones {get;set;}
    public event System.Action? FootPlanted;
    public Vector2 SwordTip {get;private set;}
    public override void _Ready(){_atlas=GD.Load<Texture2D>(Asset);TextureFilter=TextureFilterEnum.Linear;}
    public void UpdatePose(Vector2 velocity,Vector2 facing,float windup,float release,float hurt)
    {
        _velocity=velocity;if(facing.LengthSquared()>.01f)_facing=facing.Normalized();
        _windup=windup;_release=release;_hurt=hurt;
    }
    public void ReactToHit(Vector2 direction){_hitDirection=direction.Normalized();_impact=1;}
    public void Die(){_death=0;}
    public void Advance(float delta)
    {
        // Bounded substeps keep the secondary spring stable after a frame hitch.
        float remaining=Mathf.Min(delta,.1f);
        while(remaining>0){float dt=Mathf.Min(remaining,1f/120);Step(dt);remaining-=dt;}
        QueueRedraw();
    }
    private void Step(float dt)
    {
        _time+=dt;_impact=Mathf.MoveToward(_impact,0,dt*4.5f);
        _yaw=Mathf.LerpAngle(_yaw,_facing.Angle(),1-Mathf.Exp(-dt*12));
        float nextMirror=Mathf.Cos(_yaw)<-.15f?-1:Mathf.Cos(_yaw)>.15f?1:_mirror;
        if(nextMirror!=_mirror){_mirror=nextMirror;ResetFeet();}
        if(Mathf.Sin(_yaw)<-.35f)_back=true;else if(Mathf.Sin(_yaw)>-.12f)_back=false;
        float moving=_windup>0||_release<.48f||_death>=0?0:Mathf.Clamp(_velocity.Length()/78,0,1);
        _motion=Mathf.Lerp(_motion,moving,1-Mathf.Exp(-dt*12));
        var localVelocity=new Vector2(_velocity.X*_mirror,_velocity.Y);
        if(moving>.05f)
        {
            // 64 world units per gait cycle; 62% planted, 38% returning.
            _phase=Mathf.PosMod(_phase+_velocity.Length()*dt/64,1);
            for(int i=0;i<2;i++)
            {
                float phase=Mathf.PosMod(_phase+i*.5f,1);bool swing=phase>=.62f;
                var rest=new Vector2(i==0?-11:11,i);
                if(swing)
                {
                    if(!_swing[i])_swingStart[i]=_feet[i];
                    float t=(phase-.62f)/.38f;
                    var target=rest+localVelocity.Normalized()*20;
                    _feet[i]=_swingStart[i].Lerp(target,Mathf.SmoothStep(0,1,t))+Vector2.Up*Mathf.Sin(t*Mathf.Pi)*7;
                }
                else
                {
                    if(_swing[i])FootPlanted?.Invoke();
                    _feet[i]-=localVelocity*dt; // cancels root translation during support
                }
                _swing[i]=swing;
                _feet[i]=rest+(_feet[i]-rest).LimitLength(28);
            }
        }
        else for(int i=0;i<2;i++){_feet[i]=_feet[i].Lerp(new Vector2(i==0?-11:11,i),1-Mathf.Exp(-dt*12));_swing[i]=false;}
        float targetCloth=-localVelocity.X*.0015f+Mathf.Sin(_time*3)*.015f+(_release<.3f?.35f:0);
        _clothVelocity+=(targetCloth-_cloth)*65*dt-_clothVelocity*10*dt;_cloth+=_clothVelocity*dt;
        _shieldLag=Mathf.Lerp(_shieldLag,_windup>0?-.22f:_release<.3f?.25f:Mathf.Sin(_phase*Mathf.Tau)*_motion*.1f,1-Mathf.Exp(-dt*7));
        float flash=1+Mathf.Clamp(_hurt/.13f,0,1)*.35f;
        if(_death>=0){_death+=dt;if(_death>.7f)QueueFree();}
        Modulate=new Color(flash,flash,flash,_death>=0?1-Mathf.Clamp((_death-.2f)/.5f,0,1):1);
    }
    private void ResetFeet(){for(int i=0;i<2;i++){_feet[i]=new Vector2(i==0?-11:11,i);_swing[i]=false;}}
    private static Vector2 Joint(Vector2 root,Vector2 end,float upper,float lower,float bend)
    {
        var delta=end-root;float distance=Mathf.Clamp(delta.Length(),.01f,upper+lower-.01f);
        var axis=delta.LengthSquared()>.001f?delta.Normalized():Vector2.Down;
        float along=(upper*upper-lower*lower+distance*distance)/(2*distance);
        float outwards=Mathf.Sqrt(Mathf.Max(0,upper*upper-along*along));
        return root+axis*along+axis.Orthogonal()*outwards*bend;
    }
    private void Part(int index,Vector2 position,Vector2 size,Vector2 anchor,float rotation=0)
    {
        DrawSetTransform(position,rotation,new Vector2(_mirror,1));
        // Positions have already been mirrored, while local artwork retains joint orientation.
        DrawTextureRectRegion(_atlas,new Rect2(-size*anchor,size),Regions[index]);
    }
    private Vector2 Project(Vector2 p)=>new(p.X*_mirror,p.Y);
    private void Segment(int index,Vector2 from,Vector2 to)
    {
        // Painted limbs are diagonal inside their cells. Bind the actual joint centers,
        // not the cell edges, so pauldrons stay on shoulders and gauntlets on wrists.
        var (start,end)=index switch {
            4=>(new Vector2(195,369),new Vector2(102,565)),
            5=>(new Vector2(453,370),new Vector2(558,560)),
            6=>(new Vector2(746,380),new Vector2(827,565)),
            7=>(new Vector2(1140,375),new Vector2(1078,572)),
            8=>(new Vector2(144,675),new Vector2(196,850)),
            9=>(new Vector2(482,665),new Vector2(476,843)),
            10=>(new Vector2(795,681),new Vector2(764,862)),
            _=>(new Vector2(1080,680),new Vector2(1127,859))};
        var source=end-start;var target=to-from;float scale=target.Length()/source.Length();
        DrawSetTransform(Project(from),(target.Angle()-source.Angle())*_mirror,new Vector2(_mirror*scale,scale));
        DrawTextureRectRegion(_atlas,new Rect2(Regions[index].Position-start,Regions[index].Size),Regions[index]);
    }
    private void Cloth(Vector2 origin,float width,float length,float twist)
    {
        DrawSetTransform(Project(origin),0,new Vector2(_mirror,1));
        var region=Regions[3];var textureSize=_atlas.GetSize();
        for(int i=0;i<10;i++)
        {
            float a=i/10f,b=(i+1)/10f;
            Vector2 Row(float v)=>new(Mathf.Sin(v*2.5f+_time*3)*v*1.3f+twist*v*v*24,v*length);
            var pa=Row(a);var pb=Row(b);
            Vector2 Uv(float u,float v)=>(region.Position+region.Size*new Vector2(u,v))/textureSize;
            DrawPolygon(new[]{pa+Vector2.Left*width/2,pa+Vector2.Right*width/2,pb+Vector2.Right*width/2,pb+Vector2.Left*width/2},
                new[]{Colors.White},new[]{Uv(0,a),Uv(1,a),Uv(1,b),Uv(0,b)},_atlas);
        }
    }
    public override void _Draw()
    {
        float breath=Mathf.Sin(_time*2.2f),stride=Mathf.Sin(_phase*Mathf.Tau)*_motion;
        float raise=_windup>0?Mathf.SmoothStep(0,.5f,_windup):0;
        float cut=_windup>.8f?Mathf.SmoothStep(.8f,1,_windup):0;
        float recovery=_release<.48f?Mathf.SmoothStep(.12f,.48f,_release):1;
        if(_release<.48f){raise=1-recovery;cut=1;}
        var recoil=new Vector2(_hitDirection.X*_mirror,_hitDirection.Y)*_impact*4;
        var hip=new Vector2(stride*1.1f-raise*2+cut*raise*5,-41+Mathf.Abs(stride)*1.5f+breath*.35f)+recoil;
        if(_death>=0)hip+=new Vector2(_death*13,_death*32);
        float side=Mathf.Abs(Mathf.Cos(_yaw));float bodyWidth=Mathf.Lerp(1,.82f,side);
        float twist=-raise*.13f+cut*raise*.25f+stride*.025f;
        var chest=hip+new Vector2(twist*15,-24+breath*.5f);
        var neck=chest+new Vector2(twist*7,-9);
        var swordShoulder=chest+new Vector2(-14*bodyWidth,0);
        var shieldShoulder=chest+new Vector2(14*bodyWidth,1);
        var idleHand=chest+new Vector2(-22,32+stride*2);
        var raisedHand=chest+new Vector2(-24,-24);
        var contactHand=chest+new Vector2(13,6);
        var followHand=chest+new Vector2(18,19);
        var swordHand=idleHand.Lerp(raisedHand,raise).Lerp(contactHand,cut*raise);
        if(_release<.48f)swordHand=contactHand.Lerp(followHand,Mathf.SmoothStep(0,.12f,_release)).Lerp(idleHand,recovery);
        float bladeAngle=Mathf.Lerp(1.05f,-1.1f,raise);
        float attackAngle=new Vector2(_facing.X*_mirror,_facing.Y*.5f).Angle();
        bladeAngle=Mathf.LerpAngle(bladeAngle,attackAngle,cut*raise);
        if(_release<.48f)bladeAngle=Mathf.LerpAngle(attackAngle+Mathf.SmoothStep(0,.16f,_release)*.8f,1.05f,recovery);
        var shieldHand=chest+new Vector2(22+_shieldLag*12,29-raise*12);
        var swordElbow=Joint(swordShoulder,swordHand,18,18,-1);
        var shieldElbow=Joint(shieldShoulder,shieldHand,18,17,1);
        void Leg(int i)
        {
            var root=hip+new Vector2(i==0?-8:8,0);
            // Reach constraint prevents armor stretching on steep screen-space strides.
            var foot=root+(_feet[i]-root).LimitLength(43.9f);
            var knee=Joint(root,foot,21,23,1);
            Segment(8+i,root,knee);Segment(10+i,knee,foot);
            if(DrawBones){DrawSetTransform(Vector2.Zero);DrawLine(Project(root),Project(knee),Colors.Cyan,1);DrawLine(Project(knee),Project(foot),Colors.Cyan,1);}
        }
        void SwordArm(){Segment(4,swordShoulder,swordElbow);Segment(6,swordElbow,swordHand);}
        void ShieldArm(){Segment(5,shieldShoulder,shieldElbow);Segment(7,shieldElbow,shieldHand);}
        void Sword(){Part(12,Project(swordHand),new Vector2(10,46),new Vector2(.5f,.18f),(bladeAngle-Mathf.Pi/2)*_mirror);}
        void Shield(){Part(13,Project(shieldHand),new Vector2(21,30),new Vector2(.5f,.4f),_shieldLag*_mirror);}
        Leg(_feet[0].Y<_feet[1].Y?0:1);Leg(_feet[0].Y<_feet[1].Y?1:0);
        if(_back){SwordArm();Sword();ShieldArm();Shield();}
        Part(2,Project(hip),new Vector2(27*bodyWidth,20),new Vector2(.5f,.2f),twist*_mirror);
        if(!_back)Cloth(hip+new Vector2(0,-3),13,29,_cloth);
        Part(_back?15:1,Project(hip+new Vector2(0,1)),new Vector2(38*bodyWidth,34),new Vector2(.5f,1),twist*_mirror);
        if(_back)Cloth(chest+new Vector2(0,3),19,35,_cloth);
        if(!_back){SwordArm();ShieldArm();}
        Part(_back?14:0,Project(neck+new Vector2(0,3)),new Vector2(20,25),new Vector2(.5f,.83f),(-twist*.45f+_impact*.1f)*_mirror);
        if(!_back){Sword();Shield();}
        SwordTip=Project(swordHand+Vector2.FromAngle(bladeAngle)*37);
        DrawSetTransform(Vector2.Zero);
        if(DrawBones){DrawLine(Project(swordShoulder),Project(swordElbow),Colors.Orange,1);DrawLine(Project(swordElbow),Project(swordHand),Colors.Orange,1);DrawCircle(SwordTip,2,Colors.Yellow);}
    }
}
