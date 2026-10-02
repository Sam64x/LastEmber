using System.Collections.Generic;
using Godot;
namespace LastEmber;

// Room-owned expanding front. Each target is hit at most once; room teardown cancels aftershocks.
public partial class MeleeWave : Node2D
{
    public RunManager Run { get; set; }=null!;
    public Vector2 Direction { get; set; }
    public float Radius { get; set; }=180;
    public float Damage { get; set; }
    public float Heat { get; set; }
    public float Delay { get; set; }
    public bool Blue { get; set; }
    private float _age;
    private readonly HashSet<ulong> _hit=new();
    private FlameSlashRibbon _visual=null!;
    public override void _Ready()
    {
        ZIndex=30;Material=new CanvasItemMaterial {LightMode=CanvasItemMaterial.LightModeEnum.Unshaded};
        _visual=new FlameSlashRibbon();AddChild(_visual);
    }
    public override void _PhysicsProcess(double delta)
    {
        if(!Run.Playing)return;
        if(Run.Player.Dead){QueueFree();return;}
        if(Delay>0){Delay-=(float)delta;return;}
        _age+=(float)delta;
        float radius=Radius*Mathf.Clamp(_age/.3f,0,1);
        foreach(var enemy in Run.Enemies.ToArray())
        {
            if(enemy.Dead||_hit.Contains(enemy.GetInstanceId())||
               !Combat.InArc(GlobalPosition,Direction,enemy.Position,radius+enemy.BodyRadius,-.25f)||
               !Run.Room.HasLineOfSight(GlobalPosition,enemy.Position))continue;
            _hit.Add(enemy.GetInstanceId());
            if(Heat>0)enemy.AddHeat(Heat);
            enemy.TakeDamage(new DamageInfo(Damage,GlobalPosition,320));
            var direction=(enemy.Position-GlobalPosition).Normalized();
            Run.Fx.FireImpact(enemy.Position-direction*enemy.BodyRadius*.65f,direction,.65f,Blue,ImpactTraits.Wave);
        }
        if(_age>=.4f)QueueFree();
        else _visual.Configure(Vector2.Zero,Direction,-1,1,Mathf.Max(4,radius)/115,26,1-_age/.4f,_age,0,0,Blue,1,_age/.4f);
    }
}
