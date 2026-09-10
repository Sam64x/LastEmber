using Godot;
using System.Collections.Generic;
namespace LastEmber;

// Separate voice groups protect combo and critical cues from enemy footsteps and generic SFX.
public partial class MeleeAudio : Node
{
    public Player Player { get; set; }=null!;
    public MeleePresentationData Presentation { get; set; }=null!;
    private readonly AudioStreamPlayer[] _swing=new AudioStreamPlayer[3],_hit=new AudioStreamPlayer[4],
        _ignite=new AudioStreamPlayer[2],_critical=new AudioStreamPlayer[2];
    private AudioStreamPlayer _charge=null!,_ready=null!;
    private readonly int[,] _variants=new int[3,4];
    private readonly List<AudioStreamPlayer> _voices=new(13);
    private int _cursor,_critVariant;
    private bool _charging,_fullPlayed;
    private float _chargeLevel,_chargeRatio;
    private MeleeSoundBank Bank=>Player.Run.Audio.MeleeBank;
    public override void _Ready()
    {
        ProcessMode=ProcessModeEnum.Always;
        foreach(var pool in new[]{_swing,_hit,_ignite,_critical})for(int i=0;i<pool.Length;i++)pool[i]=Voice();
        _charge=Voice();_ready=Voice();
    }
    private AudioStreamPlayer Voice()
    {
        var voice=new AudioStreamPlayer {Bus="SFX",VolumeDb=-80};AddChild(voice);_voices.Add(voice);return voice;
    }
    private AudioStreamPlayer Available(AudioStreamPlayer[] pool)
    {
        foreach(var voice in pool)if(!voice.Playing)return voice;
        return pool[_cursor++%pool.Length];
    }
    public void Strike(StrikeStyleData style,MeleeSoundPhase phase,float power,bool blue,float charge=0)
    {
        int character=Mathf.Clamp(style.SoundCharacter,0,2),kind=(int)phase;
        if(kind>3)return;
        int variation=_variants[character,kind]++;
        var overrides=phase switch
        {
            MeleeSoundPhase.Ignition=>style.Ignition,MeleeSoundPhase.Swing=>style.Swing,
            MeleeSoundPhase.Hit=>style.Impact,_=>style.Miss
        };
        AudioStream stream=overrides.Count>0?overrides[variation%overrides.Count]:Bank.Get(character,variation,phase);
        var pool=phase==MeleeSoundPhase.Hit?_hit:phase==MeleeSoundPhase.Ignition?_ignite:_swing;
        float gain=(phase==MeleeSoundPhase.Hit?-12:phase==MeleeSoundPhase.Swing?-16:phase==MeleeSoundPhase.Ignition?-24:-24)+style.SoundGainDb;
        Play(Available(pool),stream,gain+Mathf.Lerp(-3,0,Mathf.Clamp(power,0,1)),
            (blue?1.16f:1)*(1+((variation%3)-1)*.012f)*Mathf.Lerp(1,.9f,Mathf.Clamp(charge,0,1)));
        if(phase==MeleeSoundPhase.Hit)Player.Run.Audio.EmphasizeCombat();
    }
    public void Critical(bool blue)
    {
        int variant=_critVariant++;
        var stream=Presentation.Critical.Count>0?Presentation.Critical[variant%Presentation.Critical.Count]:Bank.Get(0,variant,MeleeSoundPhase.Critical);
        Play(Available(_critical),stream,-16,blue?1.18f:1);
    }
    private static void Play(AudioStreamPlayer voice,AudioStream stream,float volume,float pitch)
    {
        voice.Stream=stream;voice.VolumeDb=volume;voice.PitchScale=pitch;voice.StreamPaused=false;voice.Play();
    }
    public void StartCharge()
    {
        _charging=true;_fullPlayed=false;_chargeRatio=0;
        if(!_charge.Playing)Play(_charge,Presentation.ChargeLoop??Bank.Get(0,0,MeleeSoundPhase.Charge),-70,1);
    }
    public void UpdateCharge(float amount,bool blue)
    {
        _chargeRatio=amount;_charge.PitchScale=(blue?1.14f:1)*Mathf.Lerp(.9f,1.32f,amount);
        if(amount>=1&&!_fullPlayed)
        {
            _fullPlayed=true;Play(_ready,Presentation.ChargeReady??Bank.Get(0,0,MeleeSoundPhase.Ready),-20,blue?1.18f:1);
        }
    }
    public void StopCharge(bool cancel=false)
    {
        _charging=false;_fullPlayed=false;
        if(cancel)_ready.Stop();
        if(!Player.Run.Playing){_chargeLevel=0;_charge.Stop();}
    }
    public override void _Process(double delta)
    {
        bool paused=!Player.Run.Playing;
        foreach(var voice in _voices)voice.StreamPaused=paused;
        if(paused)return;
        // Hit-stop does not chop the transient; actual menus pause the whole melee soundscape.
        _chargeLevel=Mathf.MoveToward(_chargeLevel,_charging?1:0,(float)delta*18);
        if(_chargeLevel>0)_charge.VolumeDb=-28+_chargeRatio*5+Mathf.LinearToDb(_chargeLevel);
        else if(_charge.Playing)_charge.Stop();
    }
    public void StopAll()
    {
        _charging=false;_chargeLevel=0;
        foreach(var voice in _voices)if(IsInstanceValid(voice))voice.Stop();
    }
    public override void _ExitTree()=>StopAll();
}
