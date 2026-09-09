using Godot;
namespace LastEmber;
public partial class RevealAbility : AbilityRuntime
{
    public override bool Reveals => true;
    private float _start;
    protected override void Begin()
    {
        _start=Player.Light.Radius;
        Player.RevealLight.Tint=Tint;Player.RevealLight.Lit=true;
        Player.RevealLight.Energy=Player.RevealLight.Intensity;
        Player.RevealLight.ResetRadius(_start);
        if(!Run.Lights.Contains(Player.RevealLight))Run.Lights.Add(Player.RevealLight);
        foreach(var enemy in Run.Enemies)enemy.HearReveal();
    }
    protected override void Tick(float dt)
    {
        float expand=Blue?.18f:.6f,hold=Blue?.22f:.9f;
        float fade=Mathf.Clamp((Time-expand-hold)/Mathf.Max(.05f,Duration-expand-hold),0,1);
        Player.RevealLight.ResetRadius(Time<expand?Mathf.Lerp(_start,Radius,Mathf.SmoothStep(0,1,Time/expand)):Mathf.Lerp(Radius,Player.Light.TargetRadius,Mathf.SmoothStep(0,1,fade)));
        Player.RevealLight.Energy=Player.RevealLight.Intensity*(1-fade);
        base.Tick(dt);
    }
    public override void Cancel()
    {
        base.Cancel();Player.RevealLight.Lit=false;Player.RevealLight.Energy=0;Run.Lights.Remove(Player.RevealLight);
    }
    public override void _Draw()
    {
        if(Active)DrawArc(Vector2.Zero,Mathf.Max(4,Player.RevealLight.Radius*.88f),0,Mathf.Tau,128,new Color(Tint,.65f*Mathf.Clamp(1-Time/Duration,0,1)),4,true);
    }
}
