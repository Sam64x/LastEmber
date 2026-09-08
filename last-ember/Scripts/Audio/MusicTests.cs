using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

namespace LastEmber;

public partial class MusicTests : Node
{
    public RunManager Run {get;set;}=null!;
    private readonly List<string> _results=new();
    private int _passed;
    private float _musicVolume,_sfxVolume;
    private void Check(bool condition,string description)
    {
        if(!condition)throw new InvalidOperationException("FAIL: "+description);
        _passed++;_results.Add("PASS: "+description);GD.Print("PASS: "+description);
    }
    private async Task Frames(int count)
    {for(int i=0;i<count;i++)await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);}
    private float[] Mix(MusicFrame frame){var gains=new float[(int)MusicLayer.Count];MusicMix.Evaluate(frame,gains);return gains;}
    public override async void _Ready()
    {
        ProcessMode=ProcessModeEnum.Always;_musicVolume=Run.Music.Volume;_sfxVolume=Run.Audio.Volume;
        bool success=false;
        try{await Execute();AudioPreviewPlan.Export();success=true;}
        catch(Exception error){_results.Add(error.ToString());GD.PushError(error.ToString());}
        Run.Music.SetVolume(_musicVolume);Run.Audio.SetVolume(_sfxVolume);Run.Music.SaveSettings();
        Run.Music.StopAll();Run.Audio.StopAll();await Task.Delay(150);
        var path=ProjectSettings.GlobalizePath("res://TestResults");System.IO.Directory.CreateDirectory(path);
        System.IO.File.WriteAllText(System.IO.Path.Combine(path,"music-tests.txt"),$"{(success?"PASS":"FAIL")} — {_passed} audio assertions\n"+string.Join("\n",_results));
        GD.Print($"MUSIC TESTS: {(success?"PASS":"FAIL")} ({_passed} assertions)");GetTree().Quit(success?0:1);
    }
    private async Task Execute()
    {
        var high=Mix(new MusicFrame(MusicCue.Streets,1,0));
        var combat=Mix(new MusicFrame(MusicCue.Streets,1,1));
        var half=Mix(new MusicFrame(MusicCue.Streets,.5f,.5f));
        var low=Mix(new MusicFrame(MusicCue.Streets,.2f,.2f));
        var critical=Mix(new MusicFrame(MusicCue.Streets,.05f,1));
        Check(high[(int)MusicLayer.Percussion]==0&&combat[(int)MusicLayer.Percussion]>.6f,"Danger adds combat percussion to the same exploration cue");
        Check(high[(int)MusicLayer.Tension]>0,"high Flame creates quiet attention textures even at low Danger");
        Check(half[(int)MusicLayer.Strings]<high[(int)MusicLayer.Strings]&&half[(int)MusicLayer.Melody]>0,"50 percent Flame retains melody and reduces strings");
        Check(low[(int)MusicLayer.Heartbeat]>.2f&&low[(int)MusicLayer.Fragments]>0,"20 percent Flame brings heartbeat and broken motif");
        Check(critical[(int)MusicLayer.Percussion]==0&&critical[(int)MusicLayer.Bass]==0&&critical[(int)MusicLayer.Strings]==0,"5 percent Flame removes ordinary combat layers even under high Danger");
        Check(critical[(int)MusicLayer.Breath]>0&&critical[(int)MusicLayer.Fire]<.05f,"near silence keeps breath and a final ember");
        var boss=Mix(new MusicFrame(MusicCue.Boss,1,1,4));
        Check(boss[(int)MusicLayer.Choir]>0&&boss[(int)MusicLayer.Strings]>0,"four torches support choir and strings");
        var three=Mix(new MusicFrame(MusicCue.Boss,1,1,3));var two=Mix(new MusicFrame(MusicCue.Boss,1,1,2));var one=Mix(new MusicFrame(MusicCue.Boss,1,1,1));
        Check(three[(int)MusicLayer.Choir]==0&&three[(int)MusicLayer.Strings]>0,"first torch removes choir");
        Check(two[(int)MusicLayer.Strings]==0&&two[(int)MusicLayer.Percussion]>0,"second torch removes strings");
        Check(one[(int)MusicLayer.Percussion]<two[(int)MusicLayer.Percussion]*.15f,"third torch nearly removes percussion");
        var dark=Mix(new MusicFrame(MusicCue.Boss,1,1,0,.35f));
        Check(dark[(int)MusicLayer.Melody]==0&&dark[(int)MusicLayer.Percussion]==0&&dark[(int)MusicLayer.Bass]==0,"last torch creates musical blackout");
        Check(dark[(int)MusicLayer.Heartbeat]>0&&dark[(int)MusicLayer.Breath]>0,"blackout preserves heartbeat and breath");
        var hope=Mix(new MusicFrame(MusicCue.Boss,1,1,0,.15f,0));
        var finale=Mix(new MusicFrame(MusicCue.Boss,1,1,0,.1f,12));
        Check(hope[(int)MusicLayer.Hope]>0&&hope[(int)MusicLayer.Percussion]==0,"at 15 percent boss HP the piano returns first");
        Check(finale[(int)MusicLayer.Strings]>0&&finale[(int)MusicLayer.Percussion]>0,"hope grows into strings and percussion after the piano");
        var victory=Mix(new MusicFrame(MusicCue.Victory,.1f,0));
        Check(victory[(int)MusicLayer.Melody]>.8f&&victory[(int)MusicLayer.Choir]>0,"victory completes the warm theme regardless of remaining Flame");
        foreach(float f in new[]{0f,.05f,.2f,.5f,1f})foreach(float d in new[]{0f,.5f,1f})
            foreach(float gain in Mix(new MusicFrame(MusicCue.Streets,f,d)))if(!float.IsFinite(gain)||gain<0||gain>1)throw new Exception("Unbounded music gain");
        Check(true,"mix remains bounded at all Flame/Danger edge cases");

        await Frames(210);Check(Run.Music.AssetsReady,"five cue banks and every equal-duration Ogg stem load");
        Check(Run.Music.CurrentCue==MusicCue.Menu&&Run.Music.ActiveBanks==1,"menu music transport starts");
        Run.Hud.ShowAudio();var slider=Run.Hud.Modal!.FindChild("MusicVolume",true,false) as HSlider;
        Check(slider!=null,"music volume control appears in menu");slider!.Value=37;
        Check(Mathf.IsEqualApprox(Run.Music.Volume,.37f),"music slider controls actual director volume");
        Run.Music.SetVolume(_musicVolume);Run.ShowMenu();Run.StartRun(190823);Run.Player.Automated=true;
        foreach(var enemy in Run.Enemies){enemy.FrozenForTest=true;enemy.Position=new Vector2(1720,850);}
        await Frames(300);Check(Run.Music.CurrentCue==MusicCue.Streets,"new run crossfades from menu to exploration");
        int starts=Run.Music.TransportStarts;float calm=Run.Music.LayerGain(MusicLayer.Percussion);
        var nearby=new List<Enemy>();
        for(int i=0;i<9;i++){var enemy=Run.Spawn(EnemyKind.Ashling,Run.Player.Position+new Vector2(100+i*5,40));enemy.FrozenForTest=true;nearby.Add(enemy);}
        await Frames(360);
        Check(Run.Music.Danger>.8f&&Run.Music.LayerGain(MusicLayer.Percussion)>calm+.3f,"real nearby enemies fade in combat layers");
        Check(Run.Music.TransportStarts==starts,"entering combat does not restart or seek the synchronized transport");
        foreach(var enemy in nearby)enemy.TakeDamage(new DamageInfo(10000,Run.Player.Position));await Frames(360);
        Check(Run.Music.Danger<.15f&&Run.Music.LayerGain(MusicLayer.Percussion)<.08f,"combat layers fade back out after danger passes");
        Run.Player.Flame.Damage(Run.Player.Flame.Current-20);await Frames(360);
        Check(Run.Music.LayerGain(MusicLayer.Heartbeat)>.2f,"real Flame loss introduces heartbeat");
        float living=Run.Music.LayerGain(MusicLayer.Melody);Run.Player.Flame.Damage(15);await Frames(360);
        Check(Run.Music.LayerGain(MusicLayer.Melody)<living*.2f,"real 5 Flame strips away melody");
        float ember=Run.Music.LayerGain(MusicLayer.Ambient);Run.Player.Flame.Heal(20);await Frames(360);
        Check(Run.Music.LayerGain(MusicLayer.Ambient)>ember*3,"restoring 5 to 25 Flame brings music back");
        Check(Run.Music.TransportStarts==starts,"health-driven fades preserve musical phase");
        Run.TogglePause();await Frames(240);Check(Run.Music.ActiveBanks==1&&Run.Music.CurrentCue==MusicCue.Streets,"pause ducks music without destroying its transport");Run.TogglePause();
        Run.Music.SetVolume(0);await Frames(60);Check(Run.Music.ActiveBanks==1&&Run.Audio.Volume==_sfxVolume,"music mute preserves synchronization and leaves SFX independent");Run.Music.SetVolume(_musicVolume);
        Run.Player.Flame.Damage(1000);await Frames(210);Check(Run.Music.CurrentCue==MusicCue.Silence&&Run.Music.ActiveBanks==0,"death fades the score into silence");
        Run.StartRun(44);Run.Player.Automated=true;foreach(var enemy in Run.Enemies)enemy.FrozenForTest=true;await Frames(210);
        Check(Run.Music.CurrentCue==MusicCue.Streets&&Run.Music.HopeSeconds==0&&Run.Music.ActiveBanks==1,"retry resets boss musical state without duplicate players");
        Run.EndRun(true);await Frames(210);Check(Run.Music.CurrentCue==MusicCue.Victory&&Run.Music.ActiveBanks==1,"victory crossfades to completed theme");
        Run.ShowMenu();await Frames(210);Check(Run.Music.CurrentCue==MusicCue.Menu&&Run.Music.ActiveBanks==1,"return to menu releases previous cue");
    }
}
