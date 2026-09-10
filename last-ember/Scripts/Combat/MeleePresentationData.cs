using Godot;
namespace LastEmber;

[GlobalClass]
public partial class MeleePresentationData : Resource
{
    [Export(PropertyHint.Range,"0.08,0.35,0.01")] public float ClickWindow { get; set; } = .18f;
    [Export(PropertyHint.Range,"0.08,0.4,0.01")] public float InputBuffer { get; set; } = .24f;
    [Export] public float ChargedComboGrace { get; set; } = .75f;
    [Export] public Godot.Collections.Array<StrikeStyleData> Strikes { get; set; } = new();
    [Export] public StrikeStyleData BasicStrike { get; set; } = new() {Interval=.5f};
    [Export] public StrikeStyleData ChargedStrike { get; set; } = new()
        {Label="Charged cleave",Interval=.62f,Windup=.12f,Cleave=true,SoundCharacter=2,Width=35,BodyImpulse=13,Shake=4.5f};
    [Export] public AudioStream? ChargeLoop { get; set; }
    [Export] public AudioStream? ChargeReady { get; set; }
    [Export] public Godot.Collections.Array<AudioStream> Critical { get; set; } = new();
    [Export(PropertyHint.Range,"0,1,0.05")] public float LightningIntensity { get; set; } = .8f;
    public StrikeStyleData Style(int step,bool core)
        =>core&&Strikes.Count>0?Strikes[Mathf.Clamp(step-1,0,Strikes.Count-1)]:BasicStrike;
}
