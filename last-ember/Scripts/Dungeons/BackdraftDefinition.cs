using Godot;
namespace LastEmber;
[GlobalClass]
public partial class BackdraftDefinition : DungeonAbility
{
    [Export] public float SuctionSeconds { get; set; } = .65f;
    [Export] public float PauseSeconds { get; set; } = .2f;
    [Export] public float SafeSeconds { get; set; } = 3;
    public override AbilityRuntime CreateRuntime()=>new BackdraftAbility();
}
public partial class BackdraftAbility : AbilityRuntime
{
    private bool _released;
    private BackdraftDefinition Data=>(BackdraftDefinition)Settings;
    protected override void Begin(){_released=false;Affect(DungeonImpact.Suction,Blue?.8f:Data.SafeSeconds);}
    protected override void Tick(float dt)
    {
        if(Time<Data.SuctionSeconds)Affect(DungeonImpact.Suction,Blue?.8f:Data.SafeSeconds);
        if(!_released && Time>=Data.SuctionSeconds+Data.PauseSeconds)
        {
            _released=true;
            if(!Blue){Affect(DungeonImpact.Blast);Run.Audio.Play("backdraft_blast");Run.Fx.Ring(Player.Position,Radius,Tint);Run.Shake(4);}
        }
        base.Tick(dt);
    }
    public override void _Draw()
    {
        if(!Active)return;
        if(_released)
        {
            if(Blue)return;
            float t=Mathf.Clamp((Time-Data.SuctionSeconds-Data.PauseSeconds)/Mathf.Max(.05f,Duration-Data.SuctionSeconds-Data.PauseSeconds),0,1);
            DrawArc(Vector2.Zero,Mathf.Max(4,Radius*t),0,Mathf.Tau,96,new Color(Tint,1-t),8*(1-t)+1,true);
            return;
        }
        for(int i=0;i<18;i++)
        {
            var direction=Vector2.FromAngle(i*Mathf.Tau/18+Time);
            float r=Radius*(1-Mathf.Clamp(Time/Data.SuctionSeconds,0,1));
            DrawLine(direction*r,direction*(r+24),Tint,3,true);
        }
    }
}
