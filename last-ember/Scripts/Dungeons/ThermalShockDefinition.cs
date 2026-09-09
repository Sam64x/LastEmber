using Godot;
namespace LastEmber;
[GlobalClass]
public partial class ThermalShockDefinition : DungeonAbility
{
    public override AbilityRuntime CreateRuntime()=>new ThermalShockAbility();
}
public partial class ThermalShockAbility : AbilityRuntime
{
    protected override void Begin()
    {
        Affect(DungeonImpact.Heat,Blue?2:5);
        Run.Fx.Sparks(Player.Position,Tint,24);
        Run.Audio.Play(Blue?"blue_ice":"ice_crack");
    }
    public override void _Draw()
    {
        base._Draw();if(!Active)return;
        float t=Mathf.Clamp(Time/Duration,0,1);
        for(int i=0;i<16;i++)
        {
            var p=Vector2.FromAngle(i*Mathf.Tau/16)*Radius*t;
            DrawLine(p,p+new Vector2(9,-23)*(1-t),new Color(Tint,1-t),2,true);
            DrawCircle(p+new Vector2(0,-Time*32),8+t*16,new Color(Tint,.15f*(1-t)));
        }
    }
}
