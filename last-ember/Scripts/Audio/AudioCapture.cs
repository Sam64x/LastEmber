using System;
using System.Threading.Tasks;
using Godot;

namespace LastEmber;

// Records the actual Godot music bus; this is a verification mode, not a mock mix.
public partial class AudioCapture : Node
{
    public RunManager Run {get;set;}=null!;
    public override async void _Ready()
    {
        ProcessMode=ProcessModeEnum.Always;
        try
        {
            int bus=AudioServer.GetBusIndex("Music");
            var recording=new AudioEffectRecord {Format=AudioStreamWav.FormatEnum.Format16Bits};
            AudioServer.AddBusEffect(bus,recording);
            AudioServer.SetBusMute(0,true); // Recording remains on the upstream Music bus.
            Run.Music.SetVolume(.8f);recording.SetRecordingActive(true);
            await ToSignal(GetTree().CreateTimer(12),SceneTreeTimer.SignalName.Timeout);
            recording.SetRecordingActive(false);var wav=recording.GetRecording();
            var path=ProjectSettings.GlobalizePath("res://TestResults/Audio");System.IO.Directory.CreateDirectory(path);
            wav.SaveToWav(System.IO.Path.Combine(path,"Godot_Live_Music_Bus.wav"));
            var pcm=wav.Data;double energy=0,peak=0;
            for(int i=0;i<pcm.Length;i+=2)
            {double value=BitConverter.ToInt16(pcm,i)/32768.0;energy+=value*value;peak=Math.Max(peak,Math.Abs(value));}
            double rms=Math.Sqrt(energy/(pcm.Length/2));
            if(rms<.001||peak>=.98)throw new InvalidOperationException($"Unexpected live audio level: RMS {rms}, peak {peak}");
            string result=$"PASS: actual Godot music bus records audible unclipped audio.\nSample rate {wav.MixRate}; peak {peak:F4}; RMS {rms:F4}; PCM bytes {pcm.Length}.";
            System.IO.File.WriteAllText(System.IO.Path.Combine(path,"live-capture.txt"),result);GD.Print(result);
            Run.Music.StopAll();Run.Audio.StopAll();await Task.Delay(150);GetTree().Quit();
        }
        catch(Exception error){GD.PushError(error.ToString());GetTree().Quit(1);}
    }
}
