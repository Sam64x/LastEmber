using Godot;
namespace LastEmber;

// The contact event drives both combat and presentation; artists can replace any audio layer.
[GlobalClass]
public partial class StrikeStyleData : Resource
{
    [Export] public string Label { get; set; } = "Cut";
    [Export] public float Windup { get; set; } = .065f;
    [Export] public float Interval { get; set; } = .42f;
    [Export] public float SweepSeconds { get; set; } = .105f;
    [Export] public float TailSeconds { get; set; } = .22f;
    [Export] public float StartAngle { get; set; } = -1.25f;
    [Export] public float EndAngle { get; set; } = 1.05f;
    [Export] public float Width { get; set; } = 19;
    [Export] public float BodyImpulse { get; set; } = 7;
    [Export] public float HitStop { get; set; } = .026f;
    [Export] public float Shake { get; set; } = 2;
    [Export] public bool Cleave { get; set; }
    [Export(PropertyHint.Range,"0,2,1")] public int SoundCharacter { get; set; }
    [Export] public float SoundGainDb { get; set; }
    [Export] public Godot.Collections.Array<AudioStream> Ignition { get; set; } = new();
    [Export] public Godot.Collections.Array<AudioStream> Swing { get; set; } = new();
    [Export] public Godot.Collections.Array<AudioStream> Impact { get; set; } = new();
    [Export] public Godot.Collections.Array<AudioStream> Miss { get; set; } = new();
}
