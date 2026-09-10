using Godot;
namespace LastEmber;
// Both bosses use the common combat/damage pipeline and elemental enemy component.
public partial class ElementalBoss : Enemy
{
    [Export] public bool Frost { get; set; }
    private float _attack=2,_warning;
    public override void _Ready()
    {
        Kind=EnemyKind.Boss;MaxHealth=950;BodyRadius=44;ContactDamage=14;
        IceArmored=Frost;FireAligned=!Frost;base._Ready();
    }
    protected override void Behave(float dt)
    {
        if(Weakened>0)return;
        _attack-=dt;
        if(_attack<=0 && _warning<=0){_warning=1;Target=Run.Player.Position;Run.Audio.Play("warning");}
        if(_warning>0)
        {
            _warning-=dt;
            if(_warning<=0)
            {
                if(Frost)
                {
                    Run.Fx.Ring(Target,115,new Color(.6f,.9f,1));
                    if(Run.Player.Position.DistanceTo(Target)<115)Run.Player.TakeDamage(new DamageInfo(IceArmored?18:10,Target));
                    IceArmored=true;
                }
                else for(int i=0;i<16;i++)Run.Shoot(Position,Position+Vector2.FromAngle(i*Mathf.Tau/16),235,10,true);
                OpenWeakPoint(1.2f);_attack=3.5f;
            }
        }
    }
    public override void _Draw()
    {
        base._Draw();
        if(_warning>0)DrawArc(Frost?Target-Position:Vector2.Zero,Frost?115:90,0,Mathf.Tau,64,new Color(1,.4f,.2f),4);
    }
}
