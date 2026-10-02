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
            if(!Blue){Affect(DungeonImpact.Blast);Run.Audio.Play("backdraft_blast");Run.Fx.Splash(Player.Position,Radius);Run.Shake(4);}
        }
        base.Tick(dt);
    }
    public override void _Draw()
    {
        if(!Active)return;
        if(_released)return;
        for(int i=0;i<18;i++)
        {
            var direction=Vector2.FromAngle(i*Mathf.Tau/18+Time);
            float r=Radius*(1-Mathf.Clamp(Time/Data.SuctionSeconds,0,1));
            DrawLine(direction*r,direction*(r+24),Tint,3,true);
        }
    }
}
