using Godot;
namespace LastEmber;

// Presentation-only: facing, animation and trails never drive movement or combat.
public partial class PlayerVisual : Node2D
{
    public Player? Player {get;set;}
    private HeroVisualInput _input=HeroVisualInput.Idle;
    private readonly HeroMotion _motion=new();
    private ShaderMaterial _maskMaterial=null!;
    private static Texture2D? _atlas;
    private LivingFlame _flame=null!;
    private Sprite2D _warmMask=null!;
    private readonly Vector2[] _trail=new Vector2[7];
    private readonly float[] _trailLife=new float[7];
    private readonly int[] _trailFrame=new int[7];
    private readonly bool[] _trailFlip=new bool[7];
    private readonly Vector2[] _quad=new Vector2[4],_uv=new Vector2[4];
    private readonly Color[] _colors=new Color[4];
    private readonly Vector2[] _ribbon=new Vector2[42];
    private Vector2 _dashVector=Vector2.Right,_hurtDirection;
    private float _dashAge=1,_castAge=1;
    private bool _castBlue;
    private int _slot;
    private float _time,_fireTime,_gait,_blue,_hurt,_death=-1,_trailClock;
    private bool _flip;
    private int _direction;
    private Vector2 _lean;
    public static Texture2D Atlas=>_atlas??=ResourceLoader.Load<Texture2D>("res://Assets/Characters/ember-mask-atlas-v2.png");
    public override void _Ready()
    {
        ProcessMode=ProcessModeEnum.Always;
        Material=new CanvasItemMaterial {LightMode=CanvasItemMaterial.LightModeEnum.Unshaded};
        TextureFilter=TextureFilterEnum.Linear;
        _=Atlas;
        _flame=new LivingFlame {ZIndex=1};AddChild(_flame);
        var maskMaterial=new ShaderMaterial {Shader=ResourceLoader.Load<Shader>("res://Assets/Shaders/ember_mask.gdshader")};
        _maskMaterial=maskMaterial;
        _warmMask=new Sprite2D {Texture=Atlas,Hframes=3,Vframes=2,Offset=new Vector2(0,-Atlas.GetHeight()/2f*.1f),Material=maskMaterial,ZIndex=2};
        AddChild(_warmMask);
        SyncBody();
    }
    public void Hurt(Vector2 direction){_hurt=.26f;_hurtDirection=direction;}
    public void Dash(Vector2 direction){_dashVector=direction.Normalized();_dashAge=0;}
    public void Cast(bool blue){_castAge=0;_castBlue=blue;}
    public void Die(){_death=0;QueueRedraw();}
    public void ResetForRoom()
    {
        System.Array.Clear(_trailLife);_lean=Vector2.Zero;_trailClock=0;
        _motion.Reset();_input=HeroVisualInput.Idle;_death=-1;
        _dashAge=_castAge=1;_hurt=0;
        SyncBody();QueueRedraw();
    }
    private (Vector2 Offset,float Rotation,Vector2 Scale,float Dying) BodyPose()
    {
        float dying=_death<0?0:Mathf.Clamp(_death/1.2f,0,1);
        float moving=_motion.Movement;
        float breath=Mathf.Sin(_time*2.4f);
        float bob=Mathf.Sin(_gait*2)*1.6f*moving+breath*1.3f*(1-moving*.55f);
        var scale=_input.StrikeScale*new Vector2(1+.018f*breath-.025f*Mathf.Cos(_gait*2)*moving,1-.015f*breath+.035f*Mathf.Cos(_gait*2)*moving);
        scale*=new Vector2(1-dying*.5f,1-dying*.5f);
        float gather=_castAge<.15f?Mathf.Sin(_castAge/.15f*Mathf.Pi)*.12f:0;
        scale*=new Vector2(1-gather,1+gather);
        var sway=_motion.Facing.Orthogonal()*Mathf.Sin(_gait)*moving*.65f;
        var offset=_input.StrikeOffset+_lean*2+sway+new Vector2(0,-bob+dying*5)+_hurtDirection*Mathf.Sin(_hurt/.26f*Mathf.Pi)*3;
        return (offset,_input.StrikeRotation+_lean.X*.11f+_motion.Turn*.045f,scale,dying);
    }
    private void SyncBody()
    {
        var pose=BodyPose();
        var body=new Transform2D(pose.Rotation,pose.Scale,0,pose.Offset);
        float stretch=_input.Dashing?.24f:_dashAge<.5f?Mathf.Exp(-Mathf.Max(0,_dashAge-_input.DashDuration)*22)*.12f:0;
        var d=_dashVector;float along=1+stretch,across=1-stretch*.45f;
        float cross=(along-across)*d.X*d.Y;
        var deformation=new Transform2D(new Vector2(along*d.X*d.X+across*d.Y*d.Y,cross),new Vector2(cross,along*d.Y*d.Y+across*d.X*d.X),Vector2.Zero);
        body=deformation*body;
        _flame.Transform=body;
        float surge=_input.Dashing?1:Mathf.Exp(-_dashAge*12)*.5f;
        surge+=_castAge<.5f?Mathf.Sin(_castAge/.5f*Mathf.Pi)*.5f:0;
        _flame.Update(_fireTime,_blue,1-pose.Dying,_motion.Airflow.Rotated(-pose.Rotation),surge,_motion.Movement,_motion.Turn);
        _maskMaterial.SetShaderParameter("facing",_motion.Facing);
        _maskMaterial.SetShaderParameter("breath",Mathf.Sin(_time*2.4f));
        _maskMaterial.SetShaderParameter("blue_blend",_blue);
        float pixelScale=64f/(Atlas.GetWidth()/3f);
        _warmMask.Transform=new Transform2D(body.X*pixelScale,body.Y*pixelScale,body.Origin);
        _warmMask.FlipH=false;
        var tint=_hurt>0?new Color(1.35f,1.2f,1.1f,1-pose.Dying):new Color(1,1,1,1-pose.Dying);
        _warmMask.Frame=0;_warmMask.Modulate=tint;
    }
    public override void _Process(double delta)
    {
        if(Player==null)return;
        if(!Player.Run.Playing && !(Player.Dead && Player.Run.State==RunState.GameOver))return;
        if(!Player.Dead && !Player.CanProcess())return;
        Advance((float)delta,new HeroVisualInput(Player.Dead?Vector2.Zero:Player.GetRealVelocity(),Player.Aim,Player.MoveSpeed,
            Player.Dashing,Player.DashDuration,Player.Flame.LastEmber,Player.VisualBodyOffset,Player.VisualBodyRotation,Player.VisualBodyScale));
    }
    public void Advance(float delta,HeroVisualInput input)
    {
        _input=input;float dt=Mathf.Clamp(delta,0,.1f);_motion.Step(dt,input);
        _time+=dt;_hurt=Mathf.Max(0,_hurt-dt);
        _fireTime+=dt*(1+_motion.Movement*.2f+(input.Dashing?.6f:0));
        _dashAge=Mathf.Min(1,_dashAge+dt);_castAge=Mathf.Min(1,_castAge+dt);
        if(_death>=0)_death=Mathf.Min(1.2f,_death+dt);
        _gait=_motion.Gait;
        _blue=Mathf.Lerp(_blue,input.Blue?1:0,1-Mathf.Exp(-dt*12));
        _lean=_motion.Lean;
        var aim=_motion.Facing;
        _direction=aim.Y<-.45f?2:Mathf.Abs(aim.X)>.55f?1:0;
        _flip=aim.X<0;
        for(int i=0;i<_trail.Length;i++)_trailLife[i]=Mathf.Max(0,_trailLife[i]-dt);
        _trailClock-=dt;
        if(input.Dashing && _trailClock<=0)
        {
            _trailClock=.025f;_slot=(_slot+1)%_trail.Length;
            _trail[_slot]=GlobalPosition;_trailLife[_slot]=.2f;
            _trailFrame[_slot]=_direction+(input.Blue?3:0);
            _trailFlip[_slot]=_flip;
        }
        SyncBody();QueueRedraw();
    }
    private Rect2 Source(int frame)
    {
        var cell=Atlas.GetSize()/new Vector2(3,2);
        return new Rect2(new Vector2(frame%3,frame/3)*cell,cell);
    }
    private void Sprite(Vector2 center,int frame,Color tint,Vector2 scale,bool? flip=null)
    {
        // The mask center anchors physics; the fire floats around it.
        var cell=Atlas.GetSize()/new Vector2(3,2);
        var size=new Vector2(64,64*cell.Y/cell.X)*scale;
        var rect=new Rect2(center-new Vector2(size.X*.5f,size.Y*.6f),size);
        if(flip??_flip){rect.Position+=new Vector2(size.X,0);rect.Size=new Vector2(-size.X,size.Y);}
        var source=Source(frame);var textureSize=Atlas.GetSize();
        for(int i=0;i<4;i++)_colors[i]=tint;
        // A continuous strip mesh lets the flame envelope move independently
        // while keeping the mask and eyes steady. Adjacent edges share vertices.
        float Wave(float y)
        {
            float crown=Mathf.Clamp((.32f-y)/.32f,0,1);
            float tail=Mathf.Clamp((y-.78f)/.22f,0,1);
            float speed=_input.Dashing?12:_input.Velocity.Length()>30?8:5;
            return Mathf.Sin(_time*speed+y*12+frame%3)*crown*1.7f+
                Mathf.Sin(_time*7-y*9)*tail*1.2f;
        }
        for(int band=0;band<10;band++)
        {
            float top=band/10f,bottom=(band+1)/10f;
            var a=rect.Position+new Vector2(Wave(top),rect.Size.Y*top);
            var b=rect.Position+new Vector2(Wave(bottom),rect.Size.Y*bottom);
            _quad[0]=a;_quad[1]=a+new Vector2(rect.Size.X,0);
            _quad[2]=b+new Vector2(rect.Size.X,0);_quad[3]=b;
            _uv[0]=(source.Position+new Vector2(0,source.Size.Y*top))/textureSize;
            _uv[1]=_uv[0]+new Vector2(source.Size.X/textureSize.X,0);
            _uv[3]=(source.Position+new Vector2(0,source.Size.Y*bottom))/textureSize;
            _uv[2]=_uv[3]+new Vector2(source.Size.X/textureSize.X,0);
            DrawPolygon(_quad,_colors,_uv,Atlas);
        }
    }
    public override void _Draw()
    {
        var fire=new Color(1,.43f,.1f).Lerp(new Color(.16f,.66f,1),_blue);
        DrawComet(fire);
        for(int i=0;i<_trail.Length;i++)
            if(_trailLife[i]>0)Sprite(ToLocal(_trail[i]),_trailFrame[i],new Color(1,1,1,.26f*_trailLife[i]/.2f),Vector2.One,_trailFlip[i]);
        float dying=_death<0?0:Mathf.Clamp(_death/1.2f,0,1);
        DrawSetTransform(new Vector2(0,7),0,new Vector2(1,.4f));
        DrawCircle(Vector2.Zero,18,new Color(0,0,0,.52f*(1-dying)));
        for(int i=3;i>=1;i--)DrawCircle(Vector2.Zero,10+i*4,new Color(fire,.025f*(1-dying)));
        DrawSetTransform(Vector2.Zero,0,Vector2.One);
        DrawCast();
        var pose=BodyPose();
        DrawSetTransform(pose.Offset,pose.Rotation,pose.Scale);
        // Detached sparks provide living motion without changing sprite silhouettes.
        for(int i=0;i<9;i++)
        {
            float phase=Mathf.PosMod(_time*(.5f+i*.04f)+i*.137f,1);
            float x=Mathf.Sin(i*7.13f+_time*2)* (8+phase*17);
            var wind=_motion.Airflow.Rotated(-pose.Rotation);
            var p=new Vector2(x,-14-phase*30)-wind*phase*22;
            DrawLine(p,p-wind*2+new Vector2(0,-2-i%3),new Color(fire,(1-phase)*.55f*(1-dying)),1,true);
        }
        DrawSetTransform(Vector2.Zero,0,Vector2.One);
        DrawDeath(fire,dying);
        // Small cursor-facing accent replaces the old stick-like aim indicator.
        var marker=_motion.Facing*23;
        DrawLine(marker-_motion.Facing.Rotated(.7f)*4,marker,new Color(fire,.7f*(1-dying)),1,true);
        DrawLine(marker-_motion.Facing.Rotated(-.7f)*4,marker,new Color(fire,.7f*(1-dying)),1,true);
    }
    private void DrawComet(Color fire)
    {
        if(_dashAge>=.4f || _death>=0)return;
        float opacity=_input.Dashing?1:Mathf.Clamp(1-(_dashAge-_input.DashDuration)/.16f,0,1);
        if(opacity<=.01f)return;
        var normal=_dashVector.Orthogonal();
        for(int layer=0;layer<2;layer++)
        {
            for(int i=0;i<=20;i++)
            {
                float t=i/20f;
                var center=-_dashVector*(8+t*58)+normal*Mathf.Sin(t*16-_time*22)*t*3;
                float width=Mathf.Max(.02f,1-t)*(layer==0?8:3.5f)*opacity;
                _ribbon[i]=center+normal*width;_ribbon[41-i]=center-normal*width;
            }
            DrawColoredPolygon(_ribbon,new Color(layer==0?fire:new Color(1,.93f,.75f).Lerp(new Color(.8f,.98f,1),_blue),opacity*(layer==0?.45f:.65f)));
        }
    }
    private void DrawCast()
    {
        if(_castAge>=.75f || _death>=0)return;
        float age=_castAge/.75f;
        var tint=FlamePalette.Fire(_castBlue);
        float radius=age<.2f?Mathf.Lerp(27,14,age/.2f):Mathf.Lerp(14,48,Mathf.SmoothStep(0,1,(age-.2f)/.8f));
        float alpha=Mathf.Sin(age*Mathf.Pi)*.8f;
        DrawSetTransform(new Vector2(0,8),0,new Vector2(1,.5f));
        DrawArc(Vector2.Zero,radius,0,Mathf.Tau,64,new Color(tint,alpha),2,true);
        DrawArc(Vector2.Zero,radius*.8f,_time*3,_time*3+Mathf.Pi*1.4f,40,new Color(tint,alpha*.5f),1,true);
        for(int i=0;i<8;i++)
        {
            var point=Vector2.FromAngle(i*Mathf.Tau/8+age*2)*radius;
            DrawCircle(point,2+Mathf.Sin(age*Mathf.Pi)*1.5f,new Color(.12f,.11f,.13f,alpha));
            DrawLine(point,point+point.Normalized()*3,new Color(tint,alpha),1,true);
        }
        DrawSetTransform(Vector2.Zero,0,Vector2.One);
    }
    private void DrawDeath(Color fire,float phase)
    {
        if(_death<0 || phase>=1)return;
        for(int i=0;i<16;i++)
        {
            float angle=i*2.39996f;
            var direction=Vector2.FromAngle(angle);
            float distance=(8+i%4*4)*Mathf.Sqrt(phase)*2;
            var point=direction*distance+new Vector2(0,phase*phase*18);
            float size=(i%3==0?3:1.7f)*(1-phase);
            var side=direction.Orthogonal();
            _quad[0]=point+direction*size*1.6f;_quad[1]=point+side*size;
            _quad[2]=point-direction*size;_quad[3]=point-side*size*.7f;
            DrawColoredPolygon(_quad,new Color(i%3==0?new Color(.22f,.18f,.17f):fire,(1-phase)*.9f));
            if(i%3!=0)DrawLine(point,point-direction*4*(1-phase),new Color(fire,(1-phase)*.6f),1,true);
        }
    }
}
