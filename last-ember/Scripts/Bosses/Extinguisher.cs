using Godot;

namespace LastEmber;

public partial class Extinguisher : Enemy
{
    public int Phase { get; private set; } = 1;
    public int AttackIndex { get; private set; } = -1;
    public float Warning { get; private set; }
    public override bool WindingUp => Warning > 0;
    private float _recovery = 2.5f, _charge;
    private Vector2 _chargeDirection;
    private float _attackRadius;
    private float _stepClock;
    public override void _Ready()
    {
        Kind = EnemyKind.Boss; MaxHealth = 1800; BodyRadius = 49; ContactDamage = 15;
        base._Ready();
    }
    protected override void Behave(float dt)
    {
        _stepClock-=dt;
        if(_stepClock<=0){_stepClock=_charge>0?.3f:.85f;Run.Audio.PlayAt("heavy_step",Position,1.1f);}
        if (Warning > 0)
        {
            Warning -= dt;
            if (Warning <= 0)
            {
                if (AttackIndex % 3 == 1) _charge = .52f;
                else
                {
                    var center = AttackIndex % 3 == 0 ? Target : Position;
                    Run.Fx.Ring(center, _attackRadius, new Color(1,.25f,.13f));
                    if (Run.Player.Position.DistanceTo(center) < _attackRadius) Run.Player.TakeDamage(new DamageInfo(15, center, 210));
                    Run.Shake(6);
                }
                OpenWeakPoint();
                _recovery = Phase == 3 ? 1.45f : Phase == 2 ? 1.8f : 2.3f;
            }
            return;
        }
        if (_charge > 0)
        {
            _charge -= dt; Velocity = _chargeDirection * 780; MoveAndSlide();
            if (Position.DistanceTo(Run.Player.Position) < 74) Run.Player.TakeDamage(new DamageInfo(15, Position, 240));
            if(_charge<=0)OpenWeakPoint();
            Run.Fx.Ghost(Position);
            return;
        }
        _recovery -= dt;
        var toward = Run.Player.Position - Position;
        Velocity = toward.Length() > 135 ? toward.Normalized() * (Phase == 3 ? 116 : 84) : Vector2.Zero;
        MoveAndSlide();
        if (_recovery <= 0) BeginAttack();
    }
    public void BeginAttack()
    {
        AttackIndex++;
        Target = Run.Player.Position;
        _chargeDirection = (Target - Position).Normalized();
        _attackRadius = AttackIndex % 3 == 0 ? 125 : 175;
        Warning = Phase == 3 ? .85f : 1.1f;
        Run.Audio.Play("warning");
    }
    public override void TakeDamage(DamageInfo hit)
    {
        base.TakeDamage(hit with { Knockback = 0 });
        if (Dead) return;
        int next = Health / MaxHealth <= .4f ? 3 : Health / MaxHealth <= .7f ? 2 : 1;
        if (next == Phase) return;
        Phase = next;
        Run.Room.SetTorchPhase(Phase);
        Run.Fx.Ring(Position, 250, new Color(.7f,.45f,.8f));
        Run.Hud.Toast(Phase == 2 ? "TWO LIGHTS FALL. HOLD YOUR FIRE." : "THE LAST LIGHT IS YOURS.");
        _recovery = 1.8f; Warning = 0; _charge = 0;
    }
    public override void _Draw()
    {
        if (Warning > 0)
        {
            var red = new Color(1,.23f,.12f,.7f);
            if (AttackIndex % 3 == 1)
            {
                var perpendicular = _chargeDirection.Orthogonal() * 53;
                DrawColoredPolygon(new[] { perpendicular, -perpendicular, -perpendicular + _chargeDirection * 420, perpendicular + _chargeDirection * 420 }, new Color(.8f,.12f,.08f,.20f));
                DrawLine(Vector2.Zero,_chargeDirection * 420,red,4,true);
            }
            else
            {
                var center = AttackIndex % 3 == 0 ? Target - Position : Vector2.Zero;
                DrawCircle(center,_attackRadius,new Color(.7f,.10f,.08f,.16f));
                DrawArc(center,_attackRadius,0,Mathf.Tau,64,red,4,true);
                DrawArc(center,_attackRadius*(1- Warning/1.15f),0,Mathf.Tau,48,new Color(1,.6f,.3f,.5f),2,true);
            }
        }
        DrawCircle(new Vector2(0,34),62,new Color(0,0,0,.7f));
        var c = Flash > 0 ? Colors.White : new Color(.30f,.27f,.32f);
        DrawColoredPolygon(new[] {new Vector2(-56,34),new Vector2(-46,-24),new Vector2(-24,-40),new Vector2(-21,-69),new Vector2(0,-46),new Vector2(26,-68),new Vector2(32,-36),new Vector2(54,-20),new Vector2(61,39),new Vector2(0,52)},c);
        DrawColoredPolygon(new[] {new Vector2(-23,-20),new Vector2(23,-20),new Vector2(17,24),new Vector2(0,35),new Vector2(-18,23)},new Color(.085f,.08f,.11f));
        DrawLine(new Vector2(-16,-9),new Vector2(-4,-5),new Color(1,.39f,.24f),5);
        DrawLine(new Vector2(4,-5),new Vector2(16,-9),new Color(1,.39f,.24f),5);
        DrawArc(Vector2.Zero,67,0,Mathf.Tau,48,new Color(.62f,.30f,.27f),2,true);
        if (Burn.Active) DrawCircle(new Vector2(0,20),7,new Color(1,.5f,.1f));
    }
}
