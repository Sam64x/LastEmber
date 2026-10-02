using Godot;
namespace LastEmber;

public sealed class VisualSettings
{
    private const string Path="user://visual_settings.cfg";
    public float Atmosphere {get;private set;}=.75f;
    public bool TorchFlicker {get;private set;}=true;
    public void SetAtmosphere(float value)=>Atmosphere=Mathf.Clamp(value,0,1);
    public void SetFlicker(bool enabled)=>TorchFlicker=enabled;
    public void Load()
    {
        var config=new ConfigFile();if(config.Load(Path)!=Error.Ok)return;
        var density=config.GetValue("visual","atmosphere",.75f);
        if(density.VariantType is Variant.Type.Float or Variant.Type.Int)
        {
            float value=density.AsSingle();if(float.IsFinite(value))SetAtmosphere(value);
        }
        var flicker=config.GetValue("visual","torch_flicker",true);
        if(flicker.VariantType==Variant.Type.Bool)TorchFlicker=flicker.AsBool();
    }
    public void Save()
    {
        var config=new ConfigFile();config.SetValue("visual","atmosphere",Atmosphere);
        config.SetValue("visual","torch_flicker",TorchFlicker);config.Save(Path);
    }
}
