using System;
using Godot;
namespace LastEmber;

[Flags]
public enum ImpactTraits { None=0,Critical=1,Charged=2,Finisher=4,ArmorBreak=8,Ignite=16,Armored=32,Projectile=64,Arc=128,Wave=256 }

// Contact-clocked layers combine instead of substituting a generic burst.
public partial class FireImpactFx : Node2D
{
    public bool Active {get;private set;}
    private Vector2 _direction;
    private float _strength,_age,_seed;
    private bool _blue;
    private ImpactTraits _traits;
    private ShaderMaterial _bloomMaterial=null!;
    private readonly Vector2[] _velocities=new Vector2[36],_shard=new Vector2[4];
    private bool Has(ImpactTraits trait)=>(_traits&trait)!=0;
    public override void _Ready()
    {
        ZIndex=2;Material=new CanvasItemMaterial {LightMode=CanvasItemMaterial.LightModeEnum.Unshaded};Hide();
        _bloomMaterial=new ShaderMaterial {Shader=ResourceLoader.Load<Shader>("res://Assets/Shaders/impact_bloom.gdshader")};
        AddChild(new ColorRect {Position=new Vector2(-54,-54),Size=new Vector2(108,108),Material=_bloomMaterial,MouseFilter=Control.MouseFilterEnum.Ignore,ZIndex=-1});
    }
    public void Begin(Vector2 position,Vector2 direction,float strength,bool blue,ImpactTraits traits,ulong seed)
    {
        Position=position;_direction=direction.LengthSquared()>.01f?direction.Normalized():Vector2.Right;
        _strength=Mathf.Clamp(strength,0,1);_blue=blue;_traits=traits;_age=0;_seed=seed%997;
        using var rng=new RandomNumberGenerator {Seed=seed};
        for(int i=0;i<_velocities.Length;i++)
        {
            float spread=Has(ImpactTraits.Critical)||Has(ImpactTraits.Charged)?2.3f:1.25f;
            _velocities[i]=_direction.Rotated(rng.RandfRange(-spread,spread))*rng.RandfRange(75,Has(ImpactTraits.Critical)?360:260);
        }
        _bloomMaterial.SetShaderParameter("seed",_seed);
        _bloomMaterial.SetShaderParameter("blue_blend",blue?1f:0f);
        _bloomMaterial.SetShaderParameter("critical",Has(ImpactTraits.Critical)?1f:0f);
        _bloomMaterial.SetShaderParameter("strength",_strength);
        Active=true;Show();UpdateBloom();QueueRedraw();
    }
    public void Cancel(){Active=false;Hide();}
    private void UpdateBloom()=>_bloomMaterial.SetShaderParameter("effect_age",_age);
    internal void SeekPreview(float age){_age=age;Visible=age>=0&&age<.85f;UpdateBloom();QueueRedraw();}
    public override void _Process(double delta)
    {
        if(!Active)return;_age+=(float)delta;
        if(_age>=.85f){Cancel();return;}UpdateBloom();QueueRedraw();
    }
    private Color Fire(float alpha,float heat=0.5f)
        =>FlamePalette.Shift(new Color(1,Mathf.Lerp(.22f,.94f,heat),Mathf.Lerp(.015f,.7f,heat),alpha),_blue);
    public override void _Draw()
    {
        if(!Active)return;
        float power=Mathf.Lerp(.65f,1,_strength);
        if(Has(ImpactTraits.Critical))
        {
            float cut=Mathf.Clamp(1-_age/.19f,0,1);var axis=_direction.Rotated(.8f);
            DrawLine(-axis*26*cut,axis*34*cut,new Color(1,.98f,.83f,cut),2.5f*cut+.1f,true);
            DrawLine(-axis.Orthogonal()*21*cut,axis.Orthogonal()*21*cut,Fire(cut,.9f),1.7f*cut+.1f,true);
        }
        if(Has(ImpactTraits.Charged)||Has(ImpactTraits.Finisher)||Has(ImpactTraits.Wave))
        {
            float wave=Mathf.Clamp(_age/.32f,0,1),fade=(1-wave)*(1-wave);
            DrawArc(Vector2.Zero,9+wave*(Has(ImpactTraits.Charged)?60:39),_direction.Angle()-2.2f,_direction.Angle()+2.2f,56,Fire(fade*.65f,.7f),2.5f*(1-wave)+.2f,true);
        }
        if(Has(ImpactTraits.Armored))
            DrawArc(Vector2.Zero,14+_age*22,_direction.Angle()-1.7f,_direction.Angle()+1.7f,20,new Color(.64f,.84f,1,Mathf.Clamp(1-_age/.18f,0,1)),2,true);
        int count=Has(ImpactTraits.Critical)||Has(ImpactTraits.Charged)?36:Has(ImpactTraits.Projectile)?12:22;
        for(int i=0;i<count;i++)
        {
            float fade=Mathf.Clamp(1-_age/(.22f+i%5*.075f),0,1);if(fade<=0)continue;
            var velocity=_velocities[i];var p=velocity*_age/(1+_age*2)+new Vector2(0,_age*_age*35);
            if(Has(ImpactTraits.ArmorBreak)&&i%3==0)
            {
                var axis=Vector2.FromAngle(i+_age*8);var across=axis.Orthogonal();float size=3.5f*fade;
                _shard[0]=p+axis*size*2;_shard[1]=p+across*size;_shard[2]=p-axis*size;_shard[3]=p-across*size;
                DrawColoredPolygon(_shard,new Color(.65f,.9f,1,fade));
                DrawLine(_shard[0],_shard[1],new Color(.94f,1,1,fade),1,true);
            }
            else
            {
                var tail=velocity.Normalized()*(4+14*fade);
                DrawLine(p-tail,p,Fire(fade*.15f,fade),5*fade+.3f,true);
                DrawLine(p-tail*.6f,p,Fire(fade*power,fade),1+fade*1.3f,true);
                if(fade>.6f)DrawCircle(p,1.2f,new Color(1,.98f,.87f,fade));
            }
        }
        if(Has(ImpactTraits.Ignite))
        {
            float fade=Mathf.Clamp(1-_age/.8f,0,1);
            for(int i=0;i<6;i++)
            {
                var p=new Vector2(Mathf.Sin(i*4+_age*7)*12,-_age*(18+i*5));
                DrawLine(p+new Vector2(0,7*fade),p,Fire(fade*.75f,.45f),1+fade,true);
            }
        }
        if(Has(ImpactTraits.Arc))
        {
            float fade=Mathf.Clamp(1-_age/.16f,0,1);
            for(int i=0;i<3;i++)
            {
                var axis=_direction.Rotated((i-1)*.9f);var mid=axis*12+axis.Orthogonal()*4;
                DrawLine(Vector2.Zero,mid,new Color(.65f,.9f,1,fade),1.5f,true);
                DrawLine(mid,axis*25,new Color(.65f,.9f,1,fade),1,true);
            }
        }
    }
}
