using Godot;
namespace LastEmber;

// Presentation-only: facing, animation and trails never drive movement or combat.
public partial class PlayerVisual : Node2D
{
    public Player Player {get;set;}=null!;
    private static Texture2D? _atlas;
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
    private float _time,_gait,_blue,_hurt,_death=-1,_trailClock;
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
    }
    public void Hurt(Vector2 direction){_hurt=.26f;_hurtDirection=direction;}
    public void Dash(Vector2 direction){_dashVector=direction.Normalized();_dashAge=0;}
    public void Cast(bool blue){_castAge=0;_castBlue=blue;}
    public void Die(){_death=0;QueueRedraw();}
    public void ResetForRoom()
    {
        System.Array.Clear(_trailLife);_lean=Vector2.Zero;_trailClock=0;
        _dashAge=_castAge=1;_hurt=0;
        QueueRedraw();
    }
    public override void _Process(double delta)
    {
        if(!Player.Run.Playing && !(Player.Dead && Player.Run.State==RunState.GameOver))return;
        if(!Player.Dead && !Player.CanProcess())return;
        float dt=(float)delta;
        _time+=dt;_hurt=Mathf.Max(0,_hurt-dt);
        _dashAge=Mathf.Min(1,_dashAge+dt);_castAge=Mathf.Min(1,_castAge+dt);
        if(_death>=0)_death=Mathf.Min(1.2f,_death+dt);
        float speed=Player.Velocity.Length();
        _gait+=dt*Mathf.Lerp(2,11,Mathf.Clamp(speed/220,0,1));
        _blue=Mathf.Lerp(_blue,Player.Flame.LastEmber?1:0,1-Mathf.Exp(-dt*12));
        _lean=_lean.Lerp(Player.Velocity.LimitLength(220)/220,1-Mathf.Exp(-dt*10));
        var aim=Player.Aim;
        _direction=aim.Y<-.45f?2:Mathf.Abs(aim.X)>.55f?1:0;
        _flip=aim.X<0;
        for(int i=0;i<_trail.Length;i++)_trailLife[i]=Mathf.Max(0,_trailLife[i]-dt);
        _trailClock-=dt;
        if(Player.Dashing && _trailClock<=0)
        {
            _trailClock=.025f;_slot=(_slot+1)%_trail.Length;
            _trail[_slot]=GlobalPosition;_trailLife[_slot]=.2f;
            _trailFrame[_slot]=_direction+(Player.Flame.LastEmber?3:0);
            _trailFlip[_slot]=_flip;
        }
        QueueRedraw();
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
            float speed=Player.Dashing?12:Player.Velocity.Length()>30?8:5;
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
        float moving=Mathf.Clamp(Player.Velocity.Length()/220,0,1);
        float bob=Mathf.Sin(_gait*2)*1.1f*moving+Mathf.Sin(_time*3)*1.3f;
        DrawSetTransform(new Vector2(0,7),0,new Vector2(1,.4f));
        DrawCircle(Vector2.Zero,18,new Color(0,0,0,.52f*(1-dying)));
        for(int i=3;i>=1;i--)DrawCircle(Vector2.Zero,10+i*4,new Color(fire,.025f*(1-dying)));
        DrawSetTransform(Vector2.Zero,0,Vector2.One);
        DrawCast();
        var bodyScale=Player.VisualBodyScale*new Vector2(1+.018f*Mathf.Sin(_time*5),1+.022f*Mathf.Sin(_time*4));
        bodyScale*=new Vector2(1-dying*.5f,1-dying*.5f);
        float dashStretch=Player.Dashing?.22f:Mathf.Exp(-Mathf.Max(0,_dashAge-Player.DashDuration)*22)*.12f;
        if(_dashAge>=.5f)dashStretch=0;
        bodyScale*=new Vector2(1+dashStretch*(Mathf.Abs(_dashVector.X)*2-1),1+dashStretch*(Mathf.Abs(_dashVector.Y)*2-1));
        float gather=_castAge<.15f?Mathf.Sin(_castAge/.15f*Mathf.Pi)*.12f:0;
        bodyScale*=new Vector2(1-gather,1+gather);
        var offset=Player.VisualBodyOffset+new Vector2(_lean.X*2,-bob+dying*5)+_hurtDirection*Mathf.Sin(_hurt/.26f*Mathf.Pi)*3;
        DrawSetTransform(offset,Player.VisualBodyRotation+_lean.X*.11f,bodyScale);
        var tint=_hurt>0?new Color(1.35f,1.2f,1.1f,1-dying):new Color(1,1,1,1-dying);
        if(_blue<1)Sprite(Vector2.Zero,_direction,tint,Vector2.One);
        if(_blue>0)Sprite(Vector2.Zero,_direction+3,new Color(tint,tint.A*_blue),Vector2.One);
        // Detached sparks provide living motion without changing sprite silhouettes.
        for(int i=0;i<9;i++)
        {
            float phase=Mathf.PosMod(_time*(.5f+i*.04f)+i*.137f,1);
            float x=Mathf.Sin(i*7.13f+_time*2)* (8+phase*17)-_lean.X*phase*14;
            var p=new Vector2(x,-14-phase*30);
            DrawLine(p,p+new Vector2(-_lean.X*2,-2-i%3),new Color(fire,(1-phase)*.55f*(1-dying)),1,true);
        }
        DrawSetTransform(Vector2.Zero,0,Vector2.One);
        DrawDeath(fire,dying);
        // Small cursor-facing accent replaces the old stick-like aim indicator.
        var marker=Player.Aim*23;
        DrawLine(marker-Player.Aim.Rotated(.7f)*4,marker,new Color(fire,.7f*(1-dying)),1,true);
        DrawLine(marker-Player.Aim.Rotated(-.7f)*4,marker,new Color(fire,.7f*(1-dying)),1,true);
    }
    private void DrawComet(Color fire)
    {
        if(_dashAge>=.4f || _death>=0)return;
        float opacity=Player.Dashing?1:Mathf.Clamp(1-(_dashAge-Player.DashDuration)/.16f,0,1);
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
