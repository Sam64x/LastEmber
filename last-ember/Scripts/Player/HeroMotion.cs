using Godot;
namespace LastEmber;

// Presentation input also permits isolated art rendering without a Player scene.
public readonly record struct HeroVisualInput(Vector2 Velocity,Vector2 Aim,float MoveSpeed,
    bool Dashing,float DashDuration,bool Blue,Vector2 StrikeOffset,float StrikeRotation,Vector2 StrikeScale)
{
    public static HeroVisualInput Idle => new(Vector2.Zero,Vector2.Down,220,false,.18f,false,Vector2.Zero,0,Vector2.One);
}

public sealed class HeroMotion
{
    public Vector2 Lean {get;private set;}
    public Vector2 Airflow {get;private set;}
    public Vector2 Facing=>Vector2.FromAngle(_angle);
    public float Movement {get;private set;}
    public float Gait {get;private set;}
    public float Turn {get;private set;}
    private Vector2 _leanVelocity,_flowVelocity,_previousVelocity;
    private float _angle=Mathf.Pi/2;
    private bool _initialized;

    public void Reset()
    {
        Lean=Airflow=_leanVelocity=_flowVelocity=_previousVelocity=Vector2.Zero;
        Movement=Gait=Turn=0;_initialized=false;
    }
    public void Step(float delta,HeroVisualInput input)
    {
        float dt=Mathf.Clamp(delta,0,.1f);if(dt<=0)return;
        var velocity=(input.Velocity/Mathf.Max(1,input.MoveSpeed)).LimitLength(3.6f);
        var acceleration=((velocity-_previousVelocity)/dt).LimitLength(18);
        _previousVelocity=velocity;
        var aim=input.Aim.LengthSquared()>.001f?input.Aim.Normalized():Facing;
        if(!_initialized){_angle=aim.Angle();acceleration=Vector2.Zero;_initialized=true;}
        float oldAngle=_angle;
        _angle=Mathf.LerpAngle(_angle,aim.Angle(),1-Mathf.Exp(-dt*19));
        float angularSpeed=Mathf.AngleDifference(oldAngle,_angle)/dt;
        Turn=Mathf.Lerp(Turn,Mathf.Clamp(angularSpeed/8,-1,1),1-Mathf.Exp(-dt*10));
        Movement=Mathf.Lerp(Movement,Mathf.Clamp(velocity.Length(),0,1),1-Mathf.Exp(-dt*12));
        if(!input.Dashing)Gait+=input.Velocity.Length()*dt*Mathf.Tau/135;
        var leanTarget=(velocity*.7f+acceleration*.025f).LimitLength(1.25f);
        var flowTarget=(velocity+acceleration*.035f).LimitLength(2.8f);
        var lean=Lean;var flow=Airflow;
        // Bounded substeps keep the damped springs stable at low rendering rates.
        int steps=Mathf.CeilToInt(dt*120);float h=dt/steps;
        for(int i=0;i<steps;i++)
        {
            _leanVelocity+=((leanTarget-lean)*225-_leanVelocity*23)*h;
            lean+=_leanVelocity*h;
            _flowVelocity+=((flowTarget-flow)*115-_flowVelocity*15)*h;
            flow+=_flowVelocity*h;
        }
        Lean=lean;Airflow=flow;
    }
}
