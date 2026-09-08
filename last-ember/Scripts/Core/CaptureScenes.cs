using System;
using System.Threading.Tasks;
using Godot;

namespace LastEmber;

public partial class CaptureScenes : Node
{
    public RunManager Run { get; set; }=null!;
    public override async void _Ready()
    {
        ProcessMode=ProcessModeEnum.Always;
        try
        {
            await Wait(10);await Save("menu");
            Run.StartRun(20260908);Run.Player.Automated=true;Run.Player.Position=new Vector2(850,550);
            foreach(var enemy in Run.Enemies)enemy.FrozenForTest=true;
            await Wait(25);await Save("gameplay");
            Run.Player.TryReveal();await Wait(15);await Save("reveal-wave");
            await Wait(70);await Save("reveal-hold");
            await Wait(100);await Save("reveal-fading");
            await Wait(100);await Save("reveal-ended");
            Run.Player.Flame.Damage(Run.Player.Flame.Current-50);await Wait(65);await Save("light-50");
            Run.Player.Flame.Damage(30);await Wait(65);await Save("light-20");
            Run.Player.Flame.Damage(15);await Wait(65);await Save("light-5");
            Run.Player.Flame.Damage(4);await Wait(65);await Save("light-1");Run.Player.Flame.Heal(100);
            Run.OpenRewards();await Wait(10);await Save("rewards");
            Run.ChooseReward(0);Run.TogglePause();await Wait(5);await Save("pause");Run.TogglePause();
            Run.Player.Flame.Damage(88);await Wait(90);await Save("low-flame");
            Run.Player.Flame.Damage(1000);await Wait(5);await Save("game-over");
            Run.StartRun(20260908);Run.Player.Automated=true;
            int safety=0;
            while(Run.CurrentStage!=StageKind.Boss&&safety++<2200)
            {
                foreach(var enemy in Run.Enemies.ToArray()){enemy.FrozenForTest=true;enemy.TakeDamage(new DamageInfo(10000,Run.Player.Position));}
                if(Run.State==RunState.Reward)Run.ChooseReward(0);
                if(Run.CurrentStage==StageKind.Altar&&!Run.Room.Cleared)
                {Run.OpenAltar();await Wait(5);await Save("altar");Run.ChooseAltar(-1);}
                if(Run.Room.Cleared&&Run.Playing)Run.AdvanceStage();
                await Wait(6);
            }
            var boss=(Extinguisher)Run.Enemies[0];boss.FrozenForTest=true;
            Run.Player.Position=new Vector2(960,550);boss.Position=new Vector2(1250,500);
            await Wait(90);boss.BeginAttack();await Wait(5);await Save("boss-phase-1");
            boss.TakeDamage(new DamageInfo(1100,Run.Player.Position));await Wait(290);await Save("boss-phase-3");
            boss.TakeDamage(new DamageInfo(10000,Run.Player.Position));await Wait(5);await Save("victory");
            Run.ShowMenu();DisplayServer.WindowSetSize(new Vector2I(1024,768));await Wait(15);await Save("menu-4x3");
            DisplayServer.WindowSetSize(new Vector2I(1680,720));await Wait(15);await Save("menu-ultrawide");
            Run.Audio.StopAll();Run.Music.StopAll();await Task.Delay(150);
            GetTree().Quit();
        }
        catch(Exception e){GD.PushError(e.ToString());GetTree().Quit(1);}
    }
    private async Task Wait(int frames){for(int i=0;i<frames;i++)await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);}
    private async Task Save(string name)
    {
        await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
        var path=ProjectSettings.GlobalizePath("res://TestResults");System.IO.Directory.CreateDirectory(path);
        GetViewport().GetTexture().GetImage().SavePng(System.IO.Path.Combine(path,name+".png"));
    }
}
