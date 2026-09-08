using Godot;

namespace LastEmber;

public partial class FireImpactFx : Node2D
{
    public Vector2 Direction {get;set;}
    public float Strength {get;set;}
    public bool Blue {get;set;}
    private float _age;
    private readonly Vector2[] _velocities=new Vector2[26];
    public override void _Ready()
    {
        Material=new CanvasItemMaterial {LightMode=CanvasItemMaterial.LightModeEnum.Unshaded};
        var rng=new RandomNumberGenerator();rng.Randomize();
        for(int i=0;i<_velocities.Length;i++)
            _velocities[i]=Direction.Rotated(rng.RandfRange(-1.05f,1.05f))*rng.RandfRange(70,310);
    }
    public override void _Process(double delta)
    {
        _age+=(float)delta;
        if(_age>.8f){QueueFree();return;}
        QueueRedraw();
    }
    public override void _Draw()
    {
        float power=Mathf.Lerp(.35f,1,Strength);
        if(Blue)power=Mathf.Max(power,.65f);
        if(_age<.075f)
        {
            float flash=1-_age/.075f;
            DrawCircle(Vector2.Zero,16*flash,FlamePalette.Shift(new Color(1,.8f,.28f,flash*power),Blue));
            DrawCircle(Vector2.Zero,8*flash,FlamePalette.Shift(new Color(1,.99f,.88f,flash*power),Blue));
        }
        for(int i=0;i<_velocities.Length;i++)
        {
            bool coal=i%3==0;
            float life=coal?.8f:.23f+(i%5)*.04f;
            float fade=Mathf.Clamp(1-_age/life,0,1);if(fade<=0)continue;
            var velocity=_velocities[i];
            var p=velocity*_age/(1+_age*2)+new Vector2(0,coal?-_age*_age*35:_age*_age*60);
            var color=FlamePalette.Shift(new Color(1,Mathf.Lerp(.08f,.87f,fade*fade),Mathf.Lerp(.01f,.38f,fade*fade),fade*power),Blue);
            if(coal)DrawCircle(p,1.6f+fade,color);
            else
            {
                DrawLine(p-velocity.Normalized()*(5+14*fade),p,color,(2+i%3)*power,true);
                if(fade>.75f)DrawCircle(p,1.5f,FlamePalette.Shift(new Color(1,.97f,.8f,fade*power),Blue));
            }
        }
    }
}
