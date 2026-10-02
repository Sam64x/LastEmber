using Godot;
namespace LastEmber;

// World-space samples retain the path after the hero has stopped or turned.
public partial class DashWake : Node2D
{
    private const int Capacity=32;
    private const float Lifetime=.22f;
    private readonly Vector2[] _points=new Vector2[Capacity];
    private readonly float[] _remaining=new float[Capacity];
    private readonly Vector2[] _quad=new Vector2[4],_uv=new Vector2[4];
    private readonly Color[] _colors=new Color[4];
    private ShaderMaterial _material=null!;
    private int _head,_count;
    private float _time;
    public override void _Ready()
    {
        _material=new ShaderMaterial {Shader=ResourceLoader.Load<Shader>("res://Assets/Shaders/dash_wake.gdshader")};
        Material=_material;
    }
    public void Begin(Vector2 position,bool blue)
    {
        Clear();_head=0;_count=1;_points[0]=position;_remaining[0]=Lifetime;
        _material.SetShaderParameter("blue_blend",blue?1f:0f);
    }
    public void Clear(){System.Array.Clear(_remaining);_count=0;QueueRedraw();}
    public void Advance(float dt,bool emitting,Vector2 position)
    {
        _time+=dt;
        for(int i=0;i<Capacity;i++)_remaining[i]=Mathf.Max(0,_remaining[i]-dt);
        if(emitting&&_count>0)
        {
            float distance=position.DistanceTo(_points[_head]);
            // No ribbon while blocked; discard a teleport instead of drawing across rooms.
            if(distance>200){Clear();}
            else if(distance>1)
            {
                _head=(_head+1)%Capacity;_points[_head]=position;_remaining[_head]=Lifetime;
                _count=Mathf.Min(Capacity,_count+1);
            }
        }
        _material.SetShaderParameter("effect_time",_time);QueueRedraw();
    }
    public override void _Draw()
    {
        float length=0;
        for(int i=0;i<_count-1;i++)
        {
            int newer=(_head-i+Capacity)%Capacity,older=(_head-i-1+Capacity)%Capacity;
            if(_remaining[older]<=0||_remaining[newer]<=0)break;
            var a=ToLocal(_points[newer]);var b=ToLocal(_points[older]);
            var side=(a-b).Normalized().Orthogonal();
            float segment=a.DistanceTo(b);
            float lifeA=_remaining[newer]/Lifetime,lifeB=_remaining[older]/Lifetime;
            float widthA=10*Mathf.Pow(lifeA,.7f),widthB=10*Mathf.Pow(lifeB,.7f);
            _quad[0]=a-side*widthA;_quad[1]=a+side*widthA;
            _quad[2]=b+side*widthB;_quad[3]=b-side*widthB;
            _uv[0]=new Vector2(length/80,0);_uv[1]=new Vector2(length/80,1);
            _uv[2]=new Vector2((length+segment)/80,1);_uv[3]=new Vector2((length+segment)/80,0);
            _colors[0]=_colors[1]=new Color(1,1,1,Mathf.Pow(lifeA,1.5f));
            _colors[2]=_colors[3]=new Color(1,1,1,Mathf.Pow(lifeB,1.5f));
            DrawPrimitive(_quad,_colors,_uv);length+=segment;
        }
    }
    public override void _ExitTree()=>_material.Dispose();
}
