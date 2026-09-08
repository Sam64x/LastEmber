using System;
using System.Collections.Generic;
using Godot;

namespace LastEmber;

// Original synthesized PCM, no external assets or licensing dependencies.
public partial class GameAudio : Node
{
    private readonly Dictionary<string, AudioStreamWav> _sounds = new();
    private readonly AudioStreamPlayer[] _voices = new AudioStreamPlayer[8];
    private readonly AudioStreamPlayer2D[] _spatial = new AudioStreamPlayer2D[8];
    private readonly Dictionary<string,float> _lastPlayed=new();
    private int _voice;
    private int _spatialVoice;
    private float _time;
    public float Volume {get;private set;}=.8f;
    public event Action? StrongSoundPlayed;
    public override void _Ready()
    {
        ProcessMode=ProcessModeEnum.Always;
        if(AudioServer.GetBusIndex("SFX")<0){AudioServer.AddBus();AudioServer.SetBusName(AudioServer.BusCount-1,"SFX");}
        for (int i = 0; i < _voices.Length; i++)
        {
            _voices[i] = new AudioStreamPlayer { VolumeDb = -15,Bus="SFX" }; AddChild(_voices[i]);
            _spatial[i]=new AudioStreamPlayer2D {Bus="SFX",VolumeDb=-19,MaxDistance=1100,Attenuation=1.6f};AddChild(_spatial[i]);
        }
        _sounds["swing"] = Tone(250, 80, .12f, .6f);
        _sounds["dash"] = Tone(400, 90, .2f, .4f);
        _sounds["burst"] = Tone(100, 35, .45f, .7f);
        _sounds["hurt"] = Tone(120, 45, .16f, .6f);
        _sounds["hit"] = Tone(350, 130, .08f, .5f);
        _sounds["reward"] = Tone(440, 880, .45f, 0);
        _sounds["warning"] = Tone(190, 240, .3f, .05f);
        _sounds["ignite"]=Texture(.65f,250,640,.65f,21);
        _sounds["ember_loss"]=Texture(.42f,850,210,.96f,89);
        _sounds["extinguish"]=Texture(1.1f,250,50,.85f,92);
        _sounds["step"]=Texture(.13f,90,42,.7f,10);
        _sounds["heavy_step"]=Texture(.32f,60,25,.45f,11);
        _sounds["moth"]=Texture(.4f,400,700,.94f,13);
        _sounds["siphon"]=Texture(.5f,180,78,.7f,15);
        SetVolume(Volume);
    }
    public override void _Process(double delta){_time+=(float)delta;}
    public void SetVolume(float value)
    {
        Volume=Mathf.Clamp(value,0,1);
        int bus=AudioServer.GetBusIndex("SFX");if(bus>=0)AudioServer.SetBusVolumeDb(bus,MusicDirector.Db(Volume));
    }
    public void Play(string id)
    {
        if (!_sounds.TryGetValue(id, out var stream)) return;
        float minimum=id=="hit"?.045f:id=="ignite"?.22f:.065f;
        if(_lastPlayed.TryGetValue(id,out float last)&&_time-last<minimum)return;
        _lastPlayed[id]=_time;
        var voice = _voices[_voice++ % _voices.Length]; voice.Stream = stream; voice.Play();
        voice.VolumeDb=id is "ember_loss" or "ignite"?-20:id=="hit"?-20:-15;
        if(id is "burst" or "hurt" or "warning")StrongSoundPlayed?.Invoke();
    }
    public void PlayAt(string id,Vector2 position,float gain=1)
    {
        if(!_sounds.TryGetValue(id,out var stream))return;
        var voice=_spatial[_spatialVoice++%_spatial.Length];voice.Stream=stream;voice.GlobalPosition=position;
        voice.VolumeDb=-19+MusicDirector.Db(gain);voice.PitchScale=.94f+(_spatialVoice%5)*.025f;voice.Play();
    }
    public void StopAll()
    {
        foreach(var voice in _voices)if(IsInstanceValid(voice)){voice.Stop();voice.Stream=null;}
        foreach(var voice in _spatial)if(IsInstanceValid(voice)){voice.Stop();voice.Stream=null;}
    }
    public override void _ExitTree(){StopAll();_sounds.Clear();}
    private static AudioStreamWav Texture(float duration,float from,float to,float noise,int seed)
    {
        const int rate=22050;int count=(int)(duration*rate);var data=new byte[count*2];var random=new Random(seed);
        double smooth=0,phase=0;
        for(int i=0;i<count;i++)
        {
            double t=(double)i/count;double white=random.NextDouble()*2-1;smooth=smooth*.68+white*.32;
            phase+=Mathf.Tau*Mathf.Lerp(from,to,(float)t)/rate;
            double envelope=Math.Min(1,t*35)*Math.Pow(1-t,2.3);
            double sample=((white-smooth)*noise*.50+Math.Sin(phase)*(1-noise))*envelope;
            short value=(short)(sample*24000);data[i*2]=(byte)(value&255);data[i*2+1]=(byte)((value>>8)&255);
        }
        return new AudioStreamWav {Format=AudioStreamWav.FormatEnum.Format16Bits,MixRate=rate,Data=data};
    }
    private static AudioStreamWav Tone(float from, float to, float duration, float noise)
    {
        const int rate = 22050;
        int samples = (int)(rate * duration); var bytes = new byte[samples * 2];
        var random = new Random(42); double phase = 0;
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            phase += Mathf.Lerp(from, to, t) * Mathf.Tau / rate;
            double value = (Math.Sin(phase) * (1 - noise) + (random.NextDouble() * 2 - 1) * noise) * Math.Pow(1 - t, 2) * Math.Min(1, t * 30);
            short sample = (short)(value * 25000);
            bytes[i * 2] = (byte)(sample & 255); bytes[i * 2 + 1] = (byte)((sample >> 8) & 255);
        }
        return new AudioStreamWav { Format = AudioStreamWav.FormatEnum.Format16Bits, MixRate = rate, Data = bytes };
    }
}
