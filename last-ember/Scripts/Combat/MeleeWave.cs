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
    public override void _Ready()
    {
        ZIndex=30;Material=new CanvasItemMaterial {LightMode=CanvasItemMaterial.LightModeEnum.Unshaded};
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
        }
        if(_age>=.4f)QueueFree();else QueueRedraw();
    }
    public override void _Draw()
    {
        if(Delay>0)return;
        float radius=Mathf.Max(4,Radius*Mathf.Clamp(_age/.3f,0,1));
        var color=FlamePalette.Fire(Blue);color.A=Mathf.Clamp(1-_age/.4f,0,1);
        DrawArc(Vector2.Zero,radius,Direction.Angle()-1.8f,Direction.Angle()+1.8f,64,color,6,true);
        DrawArc(Vector2.Zero,Mathf.Max(2,radius-8),Direction.Angle()-1.7f,Direction.Angle()+1.7f,64,new Color(1,1,1,color.A*.65f),2,true);
    }
}
