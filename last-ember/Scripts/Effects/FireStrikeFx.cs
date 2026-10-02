using Godot;
namespace LastEmber;

// Reusable overlapping trails; contact origins are frozen in world space.
public partial class FireStrikeFx : Node2D
{
    private sealed class Stroke
    {
        public bool Active,Released,Blue;
        public StrikeStyleData Style=null!;
        public Vector2 Aim,Origin;
        public float Age,Power,Size,Windup,Sweep,Tail,Seed,Charge;
    }
    public bool Blue { get; set; }
    private readonly Stroke[] _strokes={new(),new(),new()};
    private readonly FlameSlashRibbon[] _layers=new FlameSlashRibbon[3];
    private readonly StrikeStyleData _basic=new();
    private int _slot=-1,_serial;
    private bool _charging;
    private Vector2 _chargeAim;
    private float _charge,_chargeAge,_fullAge;
    private Stroke? Current=>_slot<0?null:_strokes[_slot];
    public Vector2 BodyOffset
    {
        get
        {
            if(_charging)return -_chargeAim*(3+5*_charge);
            var s=Current;if(s==null||!s.Active)return Vector2.Zero;
            float recover=Mathf.SmoothStep(.035f,.09f,s.Age)*Mathf.Sin(Mathf.Clamp(s.Age/.25f,0,1)*Mathf.Pi)*2;
            return s.Aim*(s.Released?s.Style.BodyImpulse*Mathf.Exp(-s.Age*19)-recover:-6*Mathf.SmoothStep(0,1,Mathf.Clamp(s.Age/s.Windup,0,1)));
        }
    }
    public float BodyRotation
    {
        get
        {
            var s=Current;if(_charging||s==null||!s.Active)return 0;
            float sign=Mathf.Sign(s.Style.EndAngle-s.Style.StartAngle);
            return sign*(s.Released?.11f*Mathf.Exp(-s.Age*17):-.1f*Mathf.Clamp(s.Age/s.Windup,0,1));
        }
    }
    public Vector2 BodyScale
    {
        get
        {
            if(_charging)return new Vector2(1-.08f*_charge,1+.1f*_charge);
            var s=Current;if(s==null||!s.Active)return Vector2.One;
            float deformation=s.Released?.11f*Mathf.Exp(-s.Age*20):-.06f*Mathf.Clamp(s.Age/s.Windup,0,1);
            return new Vector2(1+deformation,1-deformation*.75f);
        }
    }
    public override void _Ready()
    {
        ZIndex=18;Material=new CanvasItemMaterial {LightMode=CanvasItemMaterial.LightModeEnum.Unshaded};
        for(int i=0;i<_layers.Length;i++){_layers[i]=new FlameSlashRibbon();AddChild(_layers[i]);}
    }
    public void Begin(Vector2 aim,float strength)=>Begin(aim,strength,_basic,1,.065f,1,0);
    public void Begin(Vector2 aim,float strength,StrikeStyleData style,float size,float windup,float tempo,float charge)
    {
        _charging=false;_slot=(_slot+1)%_strokes.Length;
        var s=_strokes[_slot];s.Active=true;s.Released=false;s.Style=style;s.Aim=aim;s.Age=0;
        s.Power=Mathf.Clamp(strength,0,1);s.Size=size;s.Windup=Mathf.Max(.02f,windup);
        s.Sweep=Mathf.Max(.045f,style.SweepSeconds/Mathf.Sqrt(tempo));s.Tail=Mathf.Max(.09f,style.TailSeconds/Mathf.Sqrt(tempo));
        s.Seed=++_serial*2.39f;s.Charge=charge;s.Blue=Blue;RefreshRibbons();QueueRedraw();
    }
    public void Release()
    {
        var s=Current;if(s==null)return;
        s.Released=true;s.Age=0;s.Origin=GlobalPosition;RefreshRibbons();QueueRedraw();
    }
    public void ShowCharge(Vector2 aim,float ratio)
    {
        if(!_charging){_chargeAge=0;_fullAge=0;}
        _charging=true;_chargeAim=aim;_charge=ratio;QueueRedraw();
    }
    public void StopCharge(){_charging=false;_charge=0;QueueRedraw();}
    public void Clear(){foreach(var s in _strokes)s.Active=false;foreach(var layer in _layers)layer.Hide();StopCharge();QueueRedraw();}
    public override void _Process(double delta)
    {
        float dt=(float)delta;bool redraw=_charging;
        if(_charging){_chargeAge+=dt;if(_charge>=1)_fullAge+=dt;}
        foreach(var s in _strokes)
        {
            if(!s.Active)continue;redraw=true;s.Age+=dt;
            if(s.Released&&s.Age>s.Sweep+s.Tail)s.Active=false;
        }
        if(redraw){RefreshRibbons();QueueRedraw();if(GetParent() is CanvasItem body)body.QueueRedraw();}
    }
    private void RefreshRibbons()
    {
        for(int i=0;i<_strokes.Length;i++)
        {
            var s=_strokes[i];var layer=_layers[i];
            if(!s.Active){layer.Hide();continue;}
            float ready=Mathf.Clamp(s.Age/s.Windup,0,1);
            float progress=1-Mathf.Pow(1-Mathf.Clamp(s.Age/s.Sweep,0,1),3);
            float head=s.Released?Mathf.Lerp(0,s.Style.EndAngle,progress):Mathf.Lerp(s.Style.StartAngle,0,Mathf.Clamp((ready-.65f)/.35f,0,1));
            float fade=s.Released?1-Mathf.SmoothStep(s.Sweep*.3f,s.Sweep+s.Tail,s.Age):0;
            layer.Configure(s.Released?ToLocal(s.Origin):Vector2.Zero,s.Aim,s.Style.StartAngle,head,s.Size,s.Style.Width,fade,s.Age,s.Seed,s.Charge,s.Blue,s.Power,s.Released?Mathf.Clamp(s.Age/(s.Sweep+s.Tail),0,1):0);
        }
    }
    internal void SeekPreview(float age)
    {
        var s=Current;if(s==null)return;
        s.Active=age<s.Windup+s.Sweep+s.Tail;s.Released=age>=s.Windup;
        s.Age=s.Released?age-s.Windup:age;s.Origin=GlobalPosition;
        RefreshRibbons();QueueRedraw();
    }
    public override void _Draw()
    {
        for(int i=1;i<=_strokes.Length;i++)
        {
            var s=_strokes[(_slot+i+_strokes.Length)%_strokes.Length];if(!s.Active)continue;
            if(!s.Released)
            {
                float ready=Mathf.Clamp(s.Age/s.Windup,0,1);
                var palm=s.Aim.Rotated(s.Style.StartAngle)*(22+ready*14);
                DrawCircle(palm,3+ready*5,FlamePalette.Shift(new Color(1,.55f,.16f,.8f*ready),s.Blue));
                continue;
            }
        }
        if(_charging)ChargeFilaments();
    }
    private void ChargeFilaments()
    {
        var center=_chargeAim*25;var fire=FlamePalette.Fire(Blue);
        for(int i=0;i<6;i++)
        {
            float angle=i*Mathf.Tau/6+_chargeAge*(1+_charge);
            var a=center+Vector2.FromAngle(angle)*(38-_charge*14);
            var b=center+Vector2.FromAngle(angle+.35f)*13;
            DrawLine(a,b,new Color(fire,.3f+_charge*.35f),1.4f+_charge,true);
        }
        DrawCircle(center,3+_charge*6,new Color(fire,.65f));
        DrawCircle(center,2+_charge*3,new Color(1,1,1,.4f+_charge*.4f));
        DrawArc(Vector2.Zero,31,-Mathf.Pi/2,-Mathf.Pi/2+Mathf.Tau*Mathf.Max(.01f,_charge),48,new Color(fire,.65f),2,true);
        if(_charge>=1)
        {
            float flash=Mathf.Clamp(1-_fullAge/.18f,0,1);
            DrawArc(center,12+(1-flash)*18,0,Mathf.Tau,40,new Color(1,1,1,flash*.7f),2,true);
            DrawLine(center-new Vector2(0,9),center+new Vector2(0,9),new Color(1,1,1,.6f),1.5f,true);
        }
    }
    public override void _ExitTree()=>_basic.Dispose();
}
