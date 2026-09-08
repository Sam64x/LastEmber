using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace LastEmber;

public partial class MusicDirector : Node
{
    [Export] public float TransitionSeconds {get;set;}=2.8f;
    [Export(PropertyHint.Range,"0,1")] public float DefaultVolume {get;set;}=.8f;
    public RunManager Run {get;set;}=null!;
    public MusicCue CurrentCue {get;private set;}=MusicCue.Silence;
    public float Danger {get;private set;}
    public float SmoothedFlame {get;private set;}=1;
    public float Volume {get;private set;}=.8f;
    public float HopeSeconds {get;private set;}
    public int TransportStarts {get;private set;}
    public float PlaybackPosition=>_banks.TryGetValue(CurrentCue,out var bank)?bank.Player.GetPlaybackPosition():0;
    public bool AssetsReady {get;private set;}
    private readonly Dictionary<MusicCue,Bank> _banks=new();
    private readonly float[] _targets=new float[(int)MusicLayer.Count];
    private float _duck=1;
    private float _pauseGain=1;
    private bool _stopped;
    private readonly record struct Stem(MusicLayer Kind,int Index);
    private sealed class Bank
    {
        public AudioStreamPlayer Player=null!;
        public AudioStreamSynchronized Stream=null!;
        public readonly List<Stem> Stems=new();
        public readonly float[] Gains=new float[(int)MusicLayer.Count];
        public float Gain;
        public float Duration;
    }
    private static readonly float[] Trims={1.3f,2.0f,2.5f,1.8f,1.7f,2.0f,.75f,1.7f,.7f,.65f,1.8f,2.2f};
    public static float Calibration(MusicLayer layer)=>Trims[(int)layer];

    public override void _Ready()
    {
        ProcessMode=ProcessModeEnum.Always;
        EnsureBus("Music");EnsureBus("SFX");
        AudioServer.AddBusEffect(0,new AudioEffectHardLimiter {CeilingDb=-1,PreGainDb=0,Release=.12f});
        var config=new ConfigFile();Volume=DefaultVolume;
        if(config.Load("user://audio.cfg")==Error.Ok)
        {
            Volume=Mathf.Clamp((float)config.GetValue("audio","music",DefaultVolume),0,1);
            Run.Audio.SetVolume(Mathf.Clamp((float)config.GetValue("audio","sfx",.8f),0,1));
        }
        LoadScore();
    }
    private static void EnsureBus(string name)
    {
        if(AudioServer.GetBusIndex(name)>=0)return;
        AudioServer.AddBus();int index=AudioServer.BusCount-1;
        AudioServer.SetBusName(index,name);AudioServer.SetBusSend(index,"Master");
    }
    private void LoadScore()
    {
        using var json=JsonDocument.Parse(FileAccess.GetFileAsString("res://Assets/Audio/Music/score.json"));
        foreach(var cueEntry in json.RootElement.GetProperty("cues").EnumerateObject())
        {
            if(!Enum.TryParse<MusicCue>(cueEntry.Name,true,out var cue))continue;
            var stems=cueEntry.Value.GetProperty("stems");
            int stemCount=0;foreach(var unused in stems.EnumerateObject())stemCount++;
            var sync=new AudioStreamSynchronized {StreamCount=stemCount};
            var bank=new Bank {Stream=sync,Duration=cueEntry.Value.GetProperty("seconds").GetSingle()};
            int index=0;
            foreach(var entry in stems.EnumerateObject())
            {
                if(!Enum.TryParse<MusicLayer>(entry.Name,true,out var kind))throw new InvalidOperationException("Unknown music stem "+entry.Name);
                var stream=ResourceLoader.Load<AudioStreamOggVorbis>(entry.Value.GetString()!);
                if(stream==null)throw new InvalidOperationException("Music stem could not be imported: "+entry.Value);
                if(Math.Abs(stream.GetLength()-bank.Duration)>.025)throw new InvalidOperationException("Stem duration mismatch: "+entry.Name);
                stream.Loop=true;
                sync.SetSyncStream(index,stream);sync.SetSyncStreamVolume(index,-80);
                bank.Stems.Add(new Stem(kind,index++));
            }
            bank.Player=new AudioStreamPlayer {Name=cue.ToString(),Stream=sync,Bus="Music",VolumeDb=-80,ProcessMode=ProcessModeEnum.Always};
            AddChild(bank.Player);_banks[cue]=bank;
        }
        AssetsReady=_banks.Count==5;
    }
    public void SetVolume(float value){Volume=Mathf.Clamp(value,0,1);}
    public void SaveSettings()
    {
        var config=new ConfigFile();config.SetValue("audio","music",Volume);config.SetValue("audio","sfx",Run.Audio.Volume);
        config.Save("user://audio.cfg");
    }
    public void Duck()=>_duck=Mathf.Min(_duck,.80f);
    public void ResetForRun()
    {
        _stopped=false;HopeSeconds=0;Danger=0;SmoothedFlame=1;_duck=1;
        foreach(var pair in _banks)
        {
            if(pair.Key==MusicCue.Menu)continue;
            pair.Value.Player.Stop();pair.Value.Gain=0;Array.Clear(pair.Value.Gains);
        }
        CurrentCue=MusicCue.Menu;
    }
    public static float MeasureDanger(RunManager run)
    {
        if(!GodotObject.IsInstanceValid(run.Player)||run.State is RunState.Menu or RunState.Victory or RunState.GameOver)return 0;
        float threat=(1-run.Player.Flame.Ratio)*.12f;
        foreach(var enemy in run.Enemies)
        {
            if(enemy.Dead)continue;
            if(enemy.Kind==EnemyKind.Boss)return .95f;
            float distance=run.Player.Position.DistanceTo(enemy.Position);
            float proximity=Mathf.Clamp(1-distance/650,0,1);
            if(!enemy.Active&&distance>250)continue;
            threat+=.035f+proximity*.14f;
            if(enemy.Kind==EnemyKind.Torchbearer)threat+=.25f;
            if(enemy.Tethered)threat+=.17f;
        }
        return Mathf.Clamp(threat,0,1);
    }
    private MusicCue DesiredCue()
    {
        if(Run.State==RunState.Menu)return MusicCue.Menu;
        if(Run.State==RunState.Victory)return MusicCue.Victory;
        if(Run.State==RunState.GameOver)return MusicCue.Silence;
        return Run.CurrentStage switch {StageKind.Boss=>MusicCue.Boss,StageKind.Altar or StageKind.Elite=>MusicCue.Offering,_=>MusicCue.Streets};
    }
    public override void _Process(double delta)
    {
        if(!AssetsReady||_stopped)return;
        float dt=(float)delta;
        MusicCue desired=DesiredCue();
        if(desired!=CurrentCue)
        {
            CurrentCue=desired;
            if(_banks.TryGetValue(desired,out var start)&&!start.Player.Playing)
            {start.Player.Play();TransportStarts++;}
        }
        else if(_banks.TryGetValue(desired,out var initial)&&!initial.Player.Playing)
        {initial.Player.Play();TransportStarts++;}
        float flame=GodotObject.IsInstanceValid(Run.Player)&&Run.State!=RunState.Menu?Run.Player.Flame.Ratio:1;
        SmoothedFlame=Mathf.Lerp(SmoothedFlame,flame,1-Mathf.Exp(-dt*1.6f));
        Danger=Mathf.Lerp(Danger,MeasureDanger(Run),1-Mathf.Exp(-dt*.95f));
        int lit=0;float bossRatio=1;
        if(desired==MusicCue.Boss)
        {
            foreach(var light in Run.Room.Torches)if(light.Lit)lit++;
            foreach(var enemy in Run.Enemies)if(enemy is Extinguisher boss)bossRatio=boss.Health/boss.MaxHealth;
            if(bossRatio<=.15f&&Run.Playing)HopeSeconds+=dt;
        }
        _duck=Mathf.Lerp(_duck,1,1-Mathf.Exp(-dt*3));
        _pauseGain=Mathf.Lerp(_pauseGain,Run.State is RunState.Pause or RunState.Reward?.53f:1,1-Mathf.Exp(-dt*2));
        bool lastEmber=GodotObject.IsInstanceValid(Run.Player)&&Run.State!=RunState.Menu&&Run.Player.Flame.LastEmber;
        var state=new MusicFrame(desired,SmoothedFlame,Danger,lit,bossRatio,HopeSeconds,Run.CurrentStage==StageKind.Elite,lastEmber);
        MusicMix.Evaluate(state,_targets);
        foreach(var pair in _banks)
        {
            var bank=pair.Value;bool current=pair.Key==desired;
            bank.Gain=Mathf.MoveToward(bank.Gain,current?1:0,dt/Mathf.Max(.1f,TransitionSeconds));
            if(!current&&bank.Gain<=0){if(bank.Player.Playing)bank.Player.Stop();continue;}
            foreach(var stem in bank.Stems)
            {
                int index=(int)stem.Kind;
                float target=current?_targets[index]:bank.Gains[index];
                float rate=lastEmber?8:target<bank.Gains[index]?2.5f:1.3f;
                bank.Gains[index]=Mathf.Lerp(bank.Gains[index],target,1-Mathf.Exp(-dt*rate));
                bank.Stream.SetSyncStreamVolume(stem.Index,Db(bank.Gains[index]*Trims[index]));
            }
            bank.Player.VolumeDb=Db(bank.Gain*Volume*_duck*_pauseGain);
        }
    }
    public float LayerGain(MusicLayer layer)=>_banks.TryGetValue(CurrentCue,out var bank)?bank.Gains[(int)layer]:0;
    public int ActiveBanks
    {get{int count=0;foreach(var bank in _banks.Values)if(bank.Player.Playing)count++;return count;}}
    public static float Db(float linear)=>linear<=.0001f?-80:Mathf.LinearToDb(linear);
    public void StopAll()
    {
        _stopped=true;foreach(var bank in _banks.Values){bank.Player.Stop();bank.Gain=0;}
    }
    public override void _ExitTree()
    {StopAll();foreach(var bank in _banks.Values)bank.Player.Stream=null;_banks.Clear();}
}
