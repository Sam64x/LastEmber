using Godot;

namespace LastEmber;

// A reusable, emissive flame ribbon. Seeded ripples keep its edge alive without frame-to-frame flicker.
public partial class FireStrikeFx : Node2D
{
    public bool Blue {get;set;}
    private float _age,_strength,_seed;
    private bool _active,_released;
    private Vector2 _aim;
    public override void _Ready()
    {
        ZIndex=18;
        Material=new CanvasItemMaterial {LightMode=CanvasItemMaterial.LightModeEnum.Unshaded};
    }
    public void Begin(Vector2 aim,float strength)
    {
        _aim=aim;_strength=strength;_age=0;_seed+=2.39f;_active=true;_released=false;QueueRedraw();
    }
    public void Release(){_released=true;_age=.018f;QueueRedraw();}
    public void Clear(){_active=false;QueueRedraw();}
    public override void _Process(double delta)
    {
        if(!_active)return;
        _age+=(float)delta;
        if(_released&&_age>.27f)_active=false;
        QueueRedraw();
    }
    public override void _Draw()
    {
        if(!_active)return;
        float power=Mathf.Lerp(.25f,1,_strength);
        if(Blue)power=Mathf.Max(power,.65f);
        if(!_released)
        {
            float charge=Mathf.Clamp(_age/.06f,0,1);
            var center=_aim*(28-charge*7);
            for(int i=0;i<7;i++)
            {
                var d=Vector2.FromAngle(i*Mathf.Tau/7+_seed+charge*.9f);
                DrawLine(center+d*(20-charge*12),center+d*4,FlamePalette.Shift(new Color(1,.32f,.045f,power*charge),Blue),2);
            }
            DrawCircle(center,4+charge*6,FlamePalette.Shift(new Color(1,.65f,.16f,power),Blue));
            DrawCircle(center,2+charge*3,FlamePalette.Shift(new Color(1,.96f,.78f,power),Blue));
            return;
        }
        float head=Mathf.Lerp(-1.38f,1.38f,Mathf.Clamp(_age/.10f,0,1));
        float fade=1-Mathf.SmoothStep(.075f,.27f,_age);
        Color[] colors={new( .72f,.045f,.008f),new(1,.24f,.015f),new(1,.73f,.08f),new(1,.98f,.83f)};
        float[] widths={1,.7f,.39f,.14f};
        const int segments=40;
        for(int layer=0;layer<4;layer++)
        {
            var points=new Vector2[(segments+1)*2];
            for(int i=0;i<=segments;i++)
            {
                float t=(float)i/segments;
                float angle=Mathf.Lerp(-1.4f,head,t);
                float ripple=Mathf.Sin(t*59+_seed-_age*31)*.55f+Mathf.Sin(t*103+_seed*3+_age*21)*.3f;
                float taper=.08f+Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*Mathf.Pi)),.6f);
                float radius=82+Mathf.Sin(t*15+_seed)*5+_age*36;
                float width=Mathf.Lerp(8,29,_strength)*widths[layer]*taper*(1+ripple*.5f)*fade;
                var d=Vector2.FromAngle(_aim.Angle()+angle);
                points[i]=d*(radius+width*(1+ripple*.3f));
                points[points.Length-1-i]=d*(radius-width*.65f);
            }
            var color=FlamePalette.Shift(colors[layer],Blue);color.A=fade*power;
            DrawColoredPolygon(points,color);
        }
        // Detached tongues and coals move outwards along the sweep, then cool red.
        for(int i=0;i<17;i++)
        {
            float angle=-1.35f+i*.164f;
            if(angle>head)continue;
            var d=Vector2.FromAngle(_aim.Angle()+angle);
            float travel=_age*(65+i%5*24);
            float radius=92+Mathf.Sin(i*17+_seed)*9+travel;
            var p=d*radius;
            var color=FlamePalette.Shift(new Color(1,Mathf.Lerp(.12f,.75f,fade),.035f,fade*power),Blue);
            DrawLine(p-d*(4+i%4*2)*fade,p,color,Mathf.Lerp(1,3,_strength),true);
        }
    }
}
