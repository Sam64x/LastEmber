using Godot;

namespace LastEmber;

public partial class AshTrap : Node2D
{
    public RunManager Run {get;set;}=null!;
    private float _warning,_cooldown;
    public override void _PhysicsProcess(double delta)
    {
        if(!Run.Playing)return;
        float dt=(float)delta;_cooldown=Mathf.Max(0,_cooldown-dt);
        if(_warning>0)
        {
            _warning-=dt;
            if(_warning<=0)
            {
                if(Position.DistanceTo(Run.Player.Position)<48)Run.Player.TakeDamage(new DamageInfo(8,Position,80));
                Run.Audio.PlayAt("trap",Position,.85f);_cooldown=2.5f;
            }
        }
        else if(_cooldown<=0&&Position.DistanceTo(Run.Player.Position)<42)
        { _warning=.65f;Run.Audio.PlayAt("warning",Position,.75f); }
        QueueRedraw();
    }
    public override void _Draw()
    {
        var c=_warning>0?new Color(1,.45f,.2f):new Color(.43f,.37f,.35f);
        DrawArc(Vector2.Zero,28,0,Mathf.Tau,24,c,2);
        for(int i=0;i<6;i++)
        {var d=Vector2.FromAngle(i*Mathf.Tau/6);DrawLine(d*12,d*25,c,3);}
        if(_warning>0)DrawArc(Vector2.Zero,45,0,Mathf.Tau,32,c,2);
    }
}
