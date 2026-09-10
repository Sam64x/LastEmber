using Godot;
namespace LastEmber;

// Poolable local contact accent: no damage, distant chaining or full-screen flash.
public partial class CriticalLightningFx : Node2D
{
    private readonly Vector2[][] _bolts=new Vector2[6][];
    private readonly Vector2[][] _branches=new Vector2[3][];
    private readonly RandomNumberGenerator _rng=new();
    private float _age,_strength;
    private int _shape;
    private bool _blue;
    private Vector2 _direction;
    public bool Active { get; private set; }
    public void Cancel(){Active=false;Hide();}
    public override void _ExitTree()=>_rng.Dispose();
    public override void _Ready()
    {
        Material=new CanvasItemMaterial {LightMode=CanvasItemMaterial.LightModeEnum.Unshaded};
        ZIndex=5;Hide();
        for(int i=0;i<_bolts.Length;i++)_bolts[i]=new Vector2[8];
        for(int i=0;i<_branches.Length;i++)_branches[i]=new Vector2[4];
    }
    public void Begin(Vector2 position,Vector2 direction,float strength,bool blue,ulong seed)
    {
        GlobalPosition=position;_direction=direction;_strength=Mathf.Clamp(strength,0,1);_blue=blue;
        _rng.Seed=seed;_age=0;_shape=0;Active=true;Rebuild();Show();QueueRedraw();
    }
    private void Rebuild()
    {
        for(int i=0;i<_bolts.Length;i++)
        {
            var axis=_direction.Rotated(i*Mathf.Tau/_bolts.Length+_rng.RandfRange(-.13f,.13f));
            float reach=_rng.RandfRange(28,62)*(.7f+_strength*.3f);
            for(int j=0;j<_bolts[i].Length;j++)
            {
                float t=j/7f;
                _bolts[i][j]=axis*reach*t+axis.Orthogonal()*_rng.RandfRange(-7,7)*Mathf.Sin(t*Mathf.Pi);
            }
        }
        for(int i=0;i<_branches.Length;i++)
        {
            var start=_bolts[i*2][3];var axis=start.Normalized().Rotated(.8f);
            for(int j=0;j<4;j++)_branches[i][j]=start+axis*j*6+axis.Orthogonal()*_rng.RandfRange(-3,3);
        }
    }
    public override void _Process(double delta)
    {
        if(!Active)return;
        _age+=(float)delta;
        if(_age>=.24f){Active=false;Hide();return;}
        int shape=_age<.045f?0:_age<.10f?1:2;
        if(shape!=_shape){_shape=shape;Rebuild();}
        QueueRedraw();
    }
    public override void _Draw()
    {
        if(!Active)return;
        float fade=Mathf.Pow(Mathf.Clamp(1-_age/.24f,0,1),1.5f)*_strength;
        var glow=_blue?new Color(.20f,.65f,1,fade*.3f):new Color(.58f,.55f,1,fade*.3f);
        var edge=_blue?new Color(.65f,.93f,1,fade):new Color(.82f,.85f,1,fade);
        foreach(var bolt in _bolts)
        {
            DrawPolyline(bolt,glow,7,true);DrawPolyline(bolt,edge,2.5f,true);
            DrawPolyline(bolt,new Color(1,1,1,fade),1,true);
        }
        foreach(var branch in _branches)DrawPolyline(branch,edge,1,true);
        float contact=Mathf.Clamp(1-_age/.06f,0,1)*_strength;
        if(contact>0)
        {
            DrawCircle(Vector2.Zero,7*contact,new Color(1,1,1,contact*.9f));
            DrawLine(-_direction*13,_direction*13,new Color(1,.95f,.8f,contact),2,true);
        }
    }
}
