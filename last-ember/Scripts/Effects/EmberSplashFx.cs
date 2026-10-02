using Godot;
namespace LastEmber;

// Pooled cosmetic explosion, independent of damage timing and target selection.
public partial class EmberSplashFx : Node2D
{
    private static Texture2D? _atlas;
    private ShaderMaterial _material=null!;
    private float _age,_radius;
    private bool _burstOnly;
    public bool Active {get;private set;}
    public override void _Ready()
    {
        _atlas??=GD.Load<Texture2D>("res://Assets/Characters/ember-splash-atlas-v1.png");
        _material=new ShaderMaterial {Shader=GD.Load<Shader>("res://Assets/Shaders/flame_slash.gdshader")};
        Material=_material;TextureFilter=TextureFilterEnum.Linear;Hide();
    }
    public void Begin(Vector2 position,float radius,bool blue,bool burstOnly=false)
    {
        Position=position;_radius=Mathf.Max(1,radius);Active=true;_age=0;_burstOnly=burstOnly;
        _material.SetShaderParameter("blue_blend",blue?1f:0f);Seek(0);
    }
    public void Cancel(){Active=false;Hide();}
    public void Seek(float age)
    {
        _age=age;Visible=Active&&age>=0&&age<(_burstOnly?.26f:.58f);
        float phase=age<.12f?age/.12f:1+(age-.12f)/.46f*2;
        if(_burstOnly)phase=Mathf.Clamp(age/.26f,0,1)*.6f;
        _material.SetShaderParameter("phase",Mathf.Clamp(phase,0,3));
        _material.SetShaderParameter("effect_age",age);
        _material.SetShaderParameter("opacity",1-Mathf.SmoothStep(_burstOnly?.045f:.3f,_burstOnly?.26f:.58f,age));
        Scale=Vector2.One*Mathf.Lerp(_burstOnly?.7f:.35f,1,Mathf.SmoothStep(0,_burstOnly?.06f:.12f,age));
        QueueRedraw();
    }
    public override void _Process(double delta)
    {
        if(!Active)return;Seek(_age+(float)delta);if(_age>=.58f)Cancel();
    }
    public override void _Draw()=>DrawTextureRect(_atlas!,new Rect2(-_radius,-_radius,_radius*2,_radius*2),false);
    public override void _ExitTree()=>_material.Dispose();
}
