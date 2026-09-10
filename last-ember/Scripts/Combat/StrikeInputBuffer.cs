using System.Collections.Generic;
using Godot;
namespace LastEmber;

// Capture edges even during hit-stop. One replaceable buffered attack, never an unlimited queue.
public sealed class StrikeInputBuffer
{
    private readonly record struct Edge(bool Down,double Time,Vector2 Aim);
    private readonly Queue<Edge> _edges=new(8);
    private bool _gesture, _held, _queued;
    private double _pressedAt;
    private float _life, _charge;
    private Vector2 _aim;
    public bool Deciding => _gesture && _held;
    public void Capture(bool down,Vector2 aim,double now)
    {
        if(_edges.Count==8)_edges.Dequeue();
        _edges.Enqueue(new Edge(down,now,aim));
    }
    public void Clear(){_edges.Clear();_gesture=false;_held=false;_queued=false;_life=0;}
    public void Pump(MeleeController owner,float dt,double now,bool physicallyHeld)
    {
        _life-=dt;if(_life<=0)_queued=false;
        bool heavy=owner.Build.Has(MeleeEffectKind.HeavyCore);
        while(_edges.TryDequeue(out var edge))
        {
            if(edge.Down)
            {
                _held=true;_gesture=true;_pressedAt=edge.Time;
                if(!heavy)Queue(0,edge.Aim,owner.Presentation.InputBuffer);
            }
            else
            {
                _held=false;
                if(heavy&&_gesture)
                    Queue(owner.Charging?owner.FinishHeldCharge():0,edge.Aim,owner.Presentation.InputBuffer);
                _gesture=false;
            }
        }
        if(_queued&&owner.ReadyForAttack&&!owner.Charging)
        {
            _queued=false;owner.TryAttack(_charge,_aim);
        }
        if(heavy)
        {
            // No charge, slowdown or charge sound during the click decision window.
            if(_gesture&&_held&&!owner.Charging&&!_queued&&
                now-_pressedAt>=Mathf.Max(.08f,owner.Presentation.ClickWindow)&&owner.ReadyForAttack)
                owner.BeginHeldCharge();
        }
        else if(physicallyHeld&&!_queued&&owner.ReadyForAttack)owner.TryAttack();
    }
    private void Queue(float charge,Vector2 aim,float seconds)
    {
        _queued=true;_charge=charge;_aim=aim;_life=Mathf.Max(.08f,seconds);
    }
}
