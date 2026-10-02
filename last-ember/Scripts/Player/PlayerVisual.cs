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
    private int _slot;
    private float _time,_gait,_blue,_hurt,_death=-1,_trailClock;
    private bool _flip;
    private int _direction;
    private Vector2 _lean;
    public static Texture2D Atlas=>_atlas??=ResourceLoader.Load<Texture2D>("res://Assets/Characters/ember-spirit-atlas.png");
    public override void _Ready()
    {
        ProcessMode=ProcessModeEnum.Always;
        Material=new CanvasItemMaterial {LightMode=CanvasItemMaterial.LightModeEnum.Unshaded};
        TextureFilter=TextureFilterEnum.Linear;
        _=Atlas;
    }
    public void Hurt()=>_hurt=.26f;
    public void Die(){_death=0;QueueRedraw();}
    public void ResetForRoom()
    {
        System.Array.Clear(_trailLife);_lean=Vector2.Zero;_trailClock=0;
        QueueRedraw();
    }
    public override void _Process(double delta)
    {
        if(!Player.Run.Playing && !(Player.Dead && Player.Run.State==RunState.GameOver))return;
        if(!Player.Dead && !Player.CanProcess())return;
        float dt=(float)delta;
        _time+=dt;_hurt=Mathf.Max(0,_hurt-dt);
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
        // Atlas cells have a uniform floor anchor. Physics stays at the feet.
        var cell=Atlas.GetSize()/new Vector2(3,2);
        var size=new Vector2(76,76*cell.Y/cell.X)*scale;
        var rect=new Rect2(center-new Vector2(size.X*.5f,size.Y*.85f),size);
        if(flip??_flip){rect.Position+=new Vector2(size.X,0);rect.Size=new Vector2(-size.X,size.Y);}
        var source=Source(frame);var textureSize=Atlas.GetSize();
        for(int i=0;i<4;i++)_colors[i]=tint;
        // A continuous strip mesh lets the crown and cloak move independently
        // while keeping the mask and eyes steady. Adjacent edges share vertices.
        float Wave(float y)
        {
            float crown=Mathf.Clamp((.32f-y)/.32f,0,1);
            float cloth=Mathf.Clamp((y-.58f)/.42f,0,1);
            return Mathf.Sin(_time*5+y*12+frame%3)*crown*1.1f+
                Mathf.Sin(_time*7-y*9)*cloth*.65f;
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
        for(int i=0;i<_trail.Length;i++)
            if(_trailLife[i]>0)Sprite(ToLocal(_trail[i]),_trailFrame[i],new Color(1,1,1,.26f*_trailLife[i]/.2f),Vector2.One,_trailFlip[i]);
        float dying=_death<0?0:Mathf.Clamp(_death/1.2f,0,1);
        float moving=Mathf.Clamp(Player.Velocity.Length()/220,0,1);
        float bob=Mathf.Sin(_gait*2)*1.7f*moving+Mathf.Sin(_time*3)*.6f;
        var fire=new Color(1,.43f,.1f).Lerp(new Color(.16f,.66f,1),_blue);
        DrawSetTransform(new Vector2(0,7),0,new Vector2(1,.4f));
        DrawCircle(Vector2.Zero,18,new Color(0,0,0,.52f*(1-dying)));
        for(int i=3;i>=1;i--)DrawCircle(Vector2.Zero,10+i*4,new Color(fire,.025f*(1-dying)));
        DrawSetTransform(Vector2.Zero,0,Vector2.One);
        var bodyScale=Player.VisualBodyScale*new Vector2(1+.018f*Mathf.Sin(_time*5),1+.022f*Mathf.Sin(_time*4));
        bodyScale*=new Vector2(1+dying*.35f,1-dying*.72f);
        if(Player.Dashing)bodyScale*=new Vector2(1.08f,.88f);
        var offset=Player.VisualBodyOffset+new Vector2(_lean.X*2,-bob+dying*8);
        DrawSetTransform(offset,Player.VisualBodyRotation+_lean.X*.07f,bodyScale);
        var tint=_hurt>0?new Color(1.35f,1.2f,1.1f,1-dying):new Color(1,1,1,1-dying);
        if(_blue<1)Sprite(Vector2.Zero,_direction,tint,Vector2.One);
        if(_blue>0)Sprite(Vector2.Zero,_direction+3,new Color(tint,tint.A*_blue),Vector2.One);
        // Detached sparks provide living motion without changing sprite silhouettes.
        for(int i=0;i<9;i++)
        {
            float phase=Mathf.PosMod(_time*(.5f+i*.04f)+i*.137f,1);
            float x=Mathf.Sin(i*7.13f+_time*2)* (8+phase*17)-_lean.X*phase*14;
            var p=new Vector2(x,-27-phase*43);
            DrawLine(p,p+new Vector2(-_lean.X*2,-2-i%3),new Color(fire,(1-phase)*.55f*(1-dying)),1,true);
        }
        if(Player.Revealing)
        {
            DrawSetTransform(new Vector2(0,5),0,new Vector2(1,.4f));
            DrawArc(Vector2.Zero,26+Mathf.Sin(_time*8)*2,0,Mathf.Tau,48,new Color(fire,.65f),1.5f,true);
        }
        DrawSetTransform(Vector2.Zero,0,Vector2.One);
        // Small cursor-facing accent replaces the old stick-like aim indicator.
        var marker=Player.Aim*23;
        DrawLine(marker-Player.Aim.Rotated(.7f)*4,marker,new Color(fire,.7f*(1-dying)),1,true);
        DrawLine(marker-Player.Aim.Rotated(-.7f)*4,marker,new Color(fire,.7f*(1-dying)),1,true);
    }
}
