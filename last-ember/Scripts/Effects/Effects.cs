using System.Collections.Generic;
using Godot;

namespace LastEmber;

public partial class Effects : Node2D
{
    private sealed class Particle
    {
        public Vector2 Position, Velocity;
        public Color Color;
        public float Life, Max, Size;
        public int Kind;
        public string Label = "";
    }
    private readonly List<Particle> _particles = new();
    private readonly RandomNumberGenerator _rng = new();
    private readonly CriticalLightningFx[] _criticalPool=new CriticalLightningFx[8];
    private readonly FireImpactFx[] _impactPool=new FireImpactFx[24];
    private readonly EmberSplashFx[] _splashPool=new EmberSplashFx[12];
    private int _splashCursor;
    private int _impactCursor;
    private ulong _impactSeed;
    private ulong _criticalSeed;
    public override void _Ready()
    {
        ZIndex=25;_rng.Randomize();
        for(int i=0;i<_criticalPool.Length;i++){_criticalPool[i]=new CriticalLightningFx();AddChild(_criticalPool[i]);}
        for(int i=0;i<_impactPool.Length;i++){_impactPool[i]=new FireImpactFx();AddChild(_impactPool[i]);}
        for(int i=0;i<_splashPool.Length;i++){_splashPool[i]=new EmberSplashFx();AddChild(_splashPool[i]);}
    }
    public void Splash(Vector2 position,float radius,bool blue=false)
    {
        for(int i=0;i<_splashPool.Length;i++)
        {
            int index=(_splashCursor+i)%_splashPool.Length;
            if(_splashPool[index].Active)continue;_splashCursor=index;break;
        }
        _splashPool[_splashCursor].Begin(position,radius,blue);
        _splashCursor=(_splashCursor+1)%_splashPool.Length;
    }
    public void CriticalImpact(Vector2 position,Vector2 direction,float intensity,bool blue)
    {
        if(intensity<=0)return;
        foreach(var effect in _criticalPool)
        {
            if(effect.Active)continue;
            effect.Begin(position,direction,intensity,blue,++_criticalSeed*7919);return;
        }
    }
    public void ClearForRoom()
    {
        _particles.Clear();
        foreach(var child in GetChildren())
        {
            if(child is CriticalLightningFx critical)critical.Cancel();
            else if(child is FireImpactFx impact)impact.Cancel();
            else if(child is EmberSplashFx splash)splash.Cancel();
            else {if(child is CanvasItem canvas)canvas.Hide();child.QueueFree();}
        }
        QueueRedraw();
    }
    public void Sparks(Vector2 position, Color color, int count = 10)
    {
        for (int i = 0; i < count && _particles.Count < 700; i++)
        {
            float life = _rng.RandfRange(.2f, .65f);
            _particles.Add(new Particle { Position = position, Velocity = Vector2.FromAngle(_rng.Randf() * Mathf.Tau) * _rng.RandfRange(40, 230), Color = color, Life = life, Max = life, Size = _rng.RandfRange(2, 5) });
        }
    }
    public void Ring(Vector2 position, float radius, Color color)
        => _particles.Add(new Particle { Position = position, Size = radius, Color = color, Life = .45f, Max = .45f, Kind = 1 });
    public void Ghost(Vector2 position,bool blue=false)
        => _particles.Add(new Particle { Position = position, Size = 16, Color = FlamePalette.Fire(blue), Life = .25f, Max = .25f, Kind = 2 });
    public void Text(Vector2 position, string label, Color color)
        => _particles.Add(new Particle { Position = position, Velocity = new Vector2(0, -35), Label = label, Color = color, Life = .85f, Max = .85f, Kind = 3 });
    public void FireImpact(Vector2 position,Vector2 direction,float strength,bool blue=false,ImpactTraits traits=ImpactTraits.None)
    {
        for(int i=0;i<_impactPool.Length;i++)
        {
            int index=(_impactCursor+i)%_impactPool.Length;
            if(_impactPool[index].Active)continue;
            _impactCursor=index;break;
        }
        _impactPool[_impactCursor].Begin(position,direction,strength,blue,traits,++_impactSeed*7919);
        _impactCursor=(_impactCursor+1)%_impactPool.Length;
    }
    public void FlameTransition(Vector2 position,bool blue)
        =>AddChild(new FlameTransitionFx {Position=position,Blue=blue});
    public override void _Process(double delta)
    {
        float dt = (float)delta;
        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            var p = _particles[i]; p.Life -= dt;
            if (p.Life <= 0) { _particles.RemoveAt(i); continue; }
            p.Position += p.Velocity * dt;
        }
        QueueRedraw();
    }
    public override void _Draw()
    {
        foreach (var p in _particles)
        {
            var color = p.Color; color.A *= p.Life / p.Max;
            if (p.Kind == 1) DrawArc(p.Position, Mathf.Max(1, p.Size * (1 - p.Life / p.Max)), 0, Mathf.Tau, 64, color, 5, true);
            else if (p.Kind == 3) DrawString(ThemeDB.FallbackFont, p.Position, p.Label, fontSize: 21, modulate: color);
            else DrawCircle(p.Position, p.Size * (p.Kind == 2 ? p.Life / p.Max : 1), color);
        }
    }
}
