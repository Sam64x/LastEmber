using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

namespace LastEmber;

// A deterministic input driver used only by --playtest. It uses ordinary movement,
// attacks and damage; no invulnerability, healing, enemy removal or forced stage jumps.
public partial class BotPlaytest : Node
{
    public RunManager Run { get; set; }=null!;
    private readonly List<string> _report=new();
    private int _attempt, _lastStage=-1;
    private float _pathTimer;
    private Vector2 _waypoint;
    private bool _finishing;
    private float _nextReport=60;
    private readonly ulong[] _seeds={20260908,722,190823};
    private readonly string[][] _preference={
        new[]{"burning_edge","kindling","hungry_fire","coal_heart","ember_dash","blast_core","black_candle","last_spark","glass_flame","dead_lantern"},
        new[]{"blast_core","coal_heart","ember_dash","hungry_fire","burning_edge","kindling","last_spark","black_candle","glass_flame","dead_lantern"},
        new[]{"black_candle","glass_flame","last_spark","coal_heart","burning_edge","hungry_fire","kindling","ember_dash","blast_core","dead_lantern"}
    };
    public override void _Ready(){ProcessMode=ProcessModeEnum.Always;StartAttempt();}
    private void Log(string message){_report.Add(message);GD.Print(message);}
    private void StartAttempt()
    {
        Run.StartRun(_seeds[_attempt]);Run.Player.Automated=true;_lastStage=-1;_nextReport=60;
        Log($"PLAYTEST seed {_seeds[_attempt]} / build {_attempt}");
    }
    public override void _PhysicsProcess(double delta)
    {
        if(_finishing)return;
        if(Run.State is RunState.GameOver or RunState.Victory || Run.RunTime>1500)
        {FinishAttempt();return;}
        if(Run.State==RunState.Reward)
        {
            int best=0,rank=100;
            for(int i=0;i<Run.Offered.Count;i++)
            {int score=Array.IndexOf(_preference[_attempt],Run.Offered[i].Id);if(score<rank){rank=score;best=i;}}
            Run.ChooseReward(best);return;
        }
        if(Run.State==RunState.Altar){if(!Run.ChooseAltar(_attempt==1?1:0))Run.ChooseAltar(-1);return;}
        if(!Run.Playing)return;
        if(Run.StageIndex!=_lastStage)
        {
            _lastStage=Run.StageIndex;_pathTimer=0;
            Log($"  {Hud.TimeText(Run.RunTime)} stage {Run.StageIndex+1} {Run.CurrentStage}, Flame {Run.Player.Flame.Current:0}/{Run.Player.Flame.Maximum:0}");
        }
        if(Run.RunTime>=_nextReport)
        {Log($"  {Hud.TimeText(Run.RunTime)} kills {Run.Kills}, enemies {Run.Enemies.Count}, position {Run.Player.Position}, Flame {Run.Player.Flame.Current:0}");_nextReport+=60;}
        var player=Run.Player;
        if(Run.Room.Cleared){MoveTo(new Vector2(1800,552),(float)delta,true);return;}
        if(Run.CurrentStage==StageKind.Altar)
        {
            if(player.Position.DistanceTo(new Vector2(960,556))<140)Run.OpenAltar();
            else MoveTo(new Vector2(960,556),(float)delta,true);
            return;
        }
        Enemy? nearest=null;float distance=float.MaxValue;int clustered=0;
        foreach(var enemy in Run.Enemies)
        {
            float d=player.Position.DistanceTo(enemy.Position);
            if(d<distance){distance=d;nearest=enemy;}
            if(d<160&&Run.Room.HasLineOfSight(player.Position,enemy.Position))clustered++;
        }
        if(nearest==null){player.TestMovement=Vector2.Zero;return;}
        var direction=(nearest.Position-player.Position).Normalized();player.Aim=direction;
        bool clear=Run.Room.HasLineOfSight(player.Position,nearest.Position);
        if(clear&&distance<110+nearest.BodyRadius)player.TryMelee();
        if(clustered>=3&&player.Flame.Current>30)player.TryBurst();
        if(nearest is Extinguisher boss)
        {
            if(boss.Warning>0)
            {
                Vector2 escape=boss.AttackIndex%3==1?direction.Orthogonal():-direction;
                player.TestMovement=Run.Room.Steer(player.Position,escape,18);
                if(boss.Warning<.35f)player.TryDash(escape);
                return;
            }
            if(distance<110){player.TestMovement=Vector2.Zero;return;}
        }
        else if(nearest.WindingUp&&distance<nearest.BodyRadius+95)
        {
            player.TestMovement=Run.Room.Steer(player.Position,-direction,18);
            if(distance<65)player.TryDash(-direction);
            return;
        }
        if(clear&&distance<90+nearest.BodyRadius){player.TestMovement=Vector2.Zero;return;}
        MoveTo(nearest.Position,(float)delta,false);
        foreach(var enemy in Run.Enemies)if(enemy.Tethered){player.TryDash(-direction);break;}
    }
    private void MoveTo(Vector2 target,float dt,bool dash)
    {
        var player=Run.Player;_pathTimer-=dt;
        if(Run.Room.HasLineOfSight(player.Position,target))_waypoint=target;
        else if(_pathTimer<=0||player.Position.DistanceTo(_waypoint)<24)
        {_waypoint=Run.Room.Navigate(player.Position,target);_pathTimer=.35f;}
        var direction=Run.Room.Steer(player.Position,(_waypoint-player.Position).Normalized(),17);
        player.TestMovement=direction;
        if(dash&&player.Position.DistanceTo(_waypoint)>380&&Run.Room.HasLineOfSight(player.Position,player.Position+direction*200))player.TryDash(direction);
    }
    private async void FinishAttempt()
    {
        _finishing=true;
        Log($"RESULT {Run.State}: {Hud.TimeText(Run.RunTime)}, {Run.Kills} kills, {Run.Player.Flame.Current:0}/{Run.Player.Flame.Maximum:0} Flame");
        string relics="";foreach(var item in Run.Player.Build.Artifacts)relics+=item.Id+" ";Log("RELICS "+relics);
        _attempt++;
        if(_attempt<_seeds.Length)
        {
            // Avoid changing a physics space while MoveAndSlide callbacks are in flight.
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            StartAttempt();_finishing=false;return;
        }
        var path=ProjectSettings.GlobalizePath("res://TestResults");System.IO.Directory.CreateDirectory(path);
        System.IO.File.WriteAllLines(System.IO.Path.Combine(path,"playtest.txt"),_report);
        Run.Audio.StopAll();Run.Music.StopAll();await Task.Delay(150);GetTree().Quit();
    }
}
