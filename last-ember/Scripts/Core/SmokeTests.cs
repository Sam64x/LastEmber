using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

namespace LastEmber;

// Opt-in engine integration tests. Never activated in an ordinary run.
public partial class SmokeTests : Node
{
    public RunManager Run { get; set; } = null!;
    private int _checks;
    private readonly List<string> _results=new();
    public override async void _Ready()
    {
        ProcessMode=ProcessModeEnum.Always;
        try { await Execute(); WriteResult(true);Run.Audio.StopAll();Run.Music.StopAll();await Task.Delay(150);await Frames(3);GetTree().Quit(0); }
        catch(Exception error){GD.PushError(error.ToString());_results.Add(error.ToString());WriteResult(false);GetTree().Quit(1);}
    }
    private void Check(bool condition,string name)
    {
        if(!condition)throw new InvalidOperationException("FAIL: "+name);
        _checks++;_results.Add("PASS: "+name);GD.Print("PASS: "+name);
    }
    private async Task Frames(int count)
    {for(int i=0;i<count;i++)await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);}
    private void FreezeEnemies(){foreach(var enemy in Run.Enemies)enemy.FrozenForTest=true;}
    private ArtifactData Item(string id)=>Run.Artifacts.Find(a=>a.Id==id)??throw new Exception(id);
    private void Click(string name)
    {
        var button=Run.Hud.Modal?.FindChild(name,true,false) as Button;
        Check(button!=null&&!button.Disabled,"UI button available: "+name);
        var center=button!.GetGlobalRect().GetCenter();
        GetViewport().PushInput(new InputEventMouseMotion {Position=center,GlobalPosition=center},true);
        GetViewport().PushInput(new InputEventMouseButton {Position=center,GlobalPosition=center,ButtonIndex=MouseButton.Left,Pressed=true},true);
        GetViewport().PushInput(new InputEventMouseButton {Position=center,GlobalPosition=center,ButtonIndex=MouseButton.Left,Pressed=false},true);
    }
    private async Task Execute()
    {
        await Frames(3);
        Check(Run.State==RunState.Menu,"01-02: engine startup and main menu");
        Click("StartRun");await Frames(3);FreezeEnemies();
        Check(Run.Playing&&Run.Player.Flame.Current==100,"03: start run");
        Check(Run.Artifacts.Count==10,"10 artifact Resources load");
        var player=Run.Player;
        var origin=player.Position;player.TestMovement=Vector2.Right;await Frames(12);player.TestMovement=Vector2.Zero;
        float cardinal=player.Position.DistanceTo(origin);
        Check(cardinal>30&&cardinal<55,"04: movement at approximately 220 px/s");
        player.Position=origin;player.TestMovement=new Vector2(1,1);await Frames(12);player.TestMovement=Vector2.Zero;
        Check(Mathf.Abs(player.Position.DistanceTo(origin)-cardinal)<5,"05: diagonal movement normalized");
        player.Position=origin;
        Check(player.TryDash(Vector2.Right),"06: dash starts");
        Check(!player.TryDash(Vector2.Right),"07: dash cooldown prevents repeat");
        float flame=player.Flame.Current;player.TakeDamage(new DamageInfo(8,origin));
        Check(player.Flame.Current==flame,"dash invulnerability");await Frames(65);
        player.TakeDamage(new DamageInfo(8,origin));
        Check(player.Flame.Current==92,"08-09: damage reduces Flame");
        player.TakeDamage(new DamageInfo(8,origin));Check(player.Flame.Current==92,"damage i-frames prevent repeated hits");
        await Frames(55);float bright=player.Light.Radius;player.Flame.Damage(72);await Frames(70);
        Check(player.Light.Radius<bright-90&&player.Light.Radius>110,"10: light interpolates down with Flame, preserving visibility");
        Check(!player.TryBurst()||player.Flame.Current==10,"13: burst spends exactly 10");
        await Frames(110);player.Flame.Damage(1);Check(!player.TryBurst(),"burst denied below cost");
        player.Flame.Heal(100);player.Position=new Vector2(260,560);
        var target=Run.Spawn(EnemyKind.Ashling,new Vector2(330,560));target.FrozenForTest=true;
        player.Aim=Vector2.Right;Check(player.TryMelee(),"12: melee input accepted");
        Check(target.Health==20,"12: melee hit deals 20 damage");
        await Frames(32);player.Flame.Damage(10);float beforeKill=player.Flame.Current;player.TryMelee();
        Check(target.Dead,"15: enemy dies once");Check(player.Flame.Current==beforeKill+3,"16: Ashling kill restores 3 Flame");
        int kills=Run.Kills;target.TakeDamage(new DamageInfo(50,origin));Check(kills==Run.Kills,"dead enemy cannot reward twice");
        target=Run.Spawn(EnemyKind.Shade,new Vector2(350,560));target.FrozenForTest=true;
        flame=player.Flame.Current;Check(player.TryBurst(),"13: burst available after cooldown");
        Check(target.Health==12,"14: Flame Burst damages enemies in radius");Check(player.Flame.Current==flame-10,"13: burst cost independent of target count");
        target.Position=new Vector2(1700,850);
        await Frames(3);
        Check(player.Build.Apply(Item("coal_heart"),player.Flame)&&player.Flame.Maximum==120,"19: Coal Heart changes maximum");
        Check(!player.Build.Apply(Item("coal_heart"),player.Flame),"non-stackable duplicate rejected");
        player.Build.Apply(Item("glass_flame"),player.Flame);player.Build.Apply(Item("black_candle"),player.Flame);
        player.Flame.Damage(player.Flame.Current-20);
        Check(Mathf.IsEqualApprox(player.Build.DamageMultiplier(player.Flame),1.95f),"20: Glass Flame and Black Candle combine");
        player.Build.Apply(Item("last_spark"),player.Flame);player.Flame.Damage(7);
        Check(player.Build.SpeedMultiplier(player.Flame)==1.3f,"Last Spark absolute Flame threshold");
        player.Build.Apply(Item("dead_lantern"),player.Flame);
        Check(Mathf.IsEqualApprox(player.Build.LightMultiplier,.7f)&&Run.RewardWeight(Item("kindling"))>Run.RewardWeight(Item("coal_heart")),"Dead Lantern light and rarity effects");
        player.Flame.Heal(150);
        player.Build.Apply(Item("burning_edge"),player.Flame);player.Build.Apply(Item("kindling"),player.Flame);player.Build.Apply(Item("hungry_fire"),player.Flame);
        var burning=Run.Spawn(EnemyKind.Shade,new Vector2(360,570));burning.FrozenForTest=true;
        burning.TakeDamage(new DamageInfo(1,player.Position,0,true));float burnStart=burning.Health;await Frames(35);
        Check(burning.Health<burnStart,"Burn deals periodic damage");
        var nearby=Run.Spawn(EnemyKind.Shade,new Vector2(400,570));nearby.FrozenForTest=true;
        player.Flame.Damage(30);flame=player.Flame.Current;
        burning.TakeDamage(new DamageInfo(100,player.Position));
        Check(nearby.Health==42,"Kindling explosion damages nearby target");
        Check(player.Flame.Current==flame+7,"Hungry Fire adds 3 to burning Shade reward");
        player.Build.Apply(Item("ember_dash"),player.Flame);player.Build.Apply(Item("blast_core"),player.Flame);
        Check(player.Build.Get(ArtifactEffect.BurstRadius)==.3f,"Blast Core modifies burst radius");
        player.Position=new Vector2(250,550);player.TryDash(Vector2.Right);await Frames(4);
        var trailTarget=Run.Spawn(EnemyKind.Shade,new Vector2(280,550));trailTarget.FrozenForTest=true;await Frames(20);
        Check(trailTarget.Health<60&&trailTarget.Burn.Active,"Ember Dash ground damages and burns");

        // Fresh world isolates AI tests from artifact effects and lingering fire.
        Run.StartRun(722);await Frames(3);FreezeEnemies();player=Run.Player;player.Position=new Vector2(240,250);
        var moth=Run.Spawn(EnemyKind.Moth,new Vector2(1710,850));await Frames(4);
        Check(!moth.Active,"21: Moth passive outside all light");
        moth.Position=player.Position+new Vector2(190,0);await Frames(3);Check(moth.Active,"21: Moth detects gameplay light");
        var watcher=Run.Spawn(EnemyKind.Watcher,new Vector2(1710,850));await Frames(3);Check(!watcher.Active,"23: Watcher passive in darkness");
        watcher.Position=player.Position+new Vector2(210,0);await Frames(3);Check(watcher.Active,"23: Watcher activates through light");watcher.FrozenForTest=true;
        var shade=Run.Spawn(EnemyKind.Shade,new Vector2(1680,850));await Frames(3);float highSpeed=shade.SpeedNow;
        player.Flame.Damage(75);await Frames(4);Check(shade.SpeedNow>highSpeed,"22: Shade accelerates at low Flame");
        shade.FrozenForTest=true;moth.FrozenForTest=true;player.Flame.Heal(100);
        var leech=Run.Spawn(EnemyKind.Leech,player.Position+new Vector2(50,0));await Frames(4);
        Check(leech.Tethered,"24: Leech attaches tether");flame=player.Flame.Current;await Frames(50);
        Check(player.Flame.Current<flame,"24: tether drains Flame periodically");
        Check(player.TryDash(Vector2.Down),"dash available for tether escape");Check(!leech.Tethered,"25: dash breaks tether immediately");
        var elite=Run.Spawn(EnemyKind.Torchbearer,new Vector2(1500,760));elite.FrozenForTest=true;shade.Position=new Vector2(1450,740);shade.FrozenForTest=false;await Frames(5);
        Check(elite.Light!=null&&Run.IsLit(elite.Position)&&shade.SpeedNow==72,"27: Torchbearer light weakens Shade");
        elite.TakeDamage(new DamageInfo(1000,player.Position));Check(elite.Light!=null&&!elite.Light.Lit,"Torchbearer light extinguishes on death");
        Run.TogglePause();var pausedPosition=player.Position;float pausedTime=Run.RunTime;await Frames(12);
        Check(Run.State==RunState.Pause&&player.Position==pausedPosition&&Run.RunTime==pausedTime,"Pause freezes world and run clock");
        Click("Resume");Check(Run.Playing&&!GetTree().Paused,"Pause UI resumes simulation");
        player.Flame.Damage(1000);Check(Run.State==RunState.GameOver,"11,33: zero Flame opens Game Over");
        Click("Retry");await Frames(3);FreezeEnemies();
        Check(Run.Playing&&Run.Kills==0&&Run.StageIndex==0&&Run.Player.Build.Artifacts.Count==0&&Run.Player.Flame.Current==100,"34: Retry creates clean run state");

        // Seeded full-route integration: actual death callbacks, wave scheduling, reward buttons and gates.
        Run.StartRun(190823);await Frames(3);
        int rooms=0;bool altarSeen=false;int safety=0;
        while(Run.CurrentStage!=StageKind.Boss&&safety++<2400)
        {
            FreezeEnemies();
            foreach(var enemy in Run.Enemies.ToArray())enemy.TakeDamage(new DamageInfo(10000,Run.Player.Position));
            if(Run.State==RunState.Reward)
            {
                Check(Run.Offered.Count>0&&Run.Offered.Count<=3,"18: reward choice appears");
                var unique=new HashSet<string>();foreach(var item in Run.Offered)Check(unique.Add(item.Id)&&!Run.Player.Build.Owns(item.Id),"reward excludes duplicates/owned relics");
                Click("Reward0");Check(Run.Playing,"19: reward resumes gameplay");
            }
            if(Run.CurrentStage==StageKind.Altar&&!Run.Room.Cleared)
            {
                Run.OpenAltar();Check(Run.State==RunState.Altar,"26: altar interaction opens choices");
                float max=Run.Player.Flame.Maximum;Click("Altar1");
                Check(Run.Player.Flame.Maximum==max-20&&Run.Player.Build.BurstCost==6,"26: sacrifice trades maximum Flame for reduced burst cost");altarSeen=true;
            }
            if(Run.Room.Cleared&&Run.Playing){rooms++;Run.AdvanceStage();}
            await Frames(6);
        }
        Check(rooms==9&&altarSeen,"17: complete all pre-boss stages without deadlock");
        Check(Run.CurrentStage==StageKind.Boss&&Run.Enemies[0] is Extinguisher,"28: boss arena starts");
        var boss=(Extinguisher)Run.Enemies[0];boss.FrozenForTest=true;
        Check(Run.Room.Torches.Count==4&&Run.Room.Torches.TrueForAll(t=>t.Lit),"28: four arena torches lit");
        boss.TakeDamage(new DamageInfo(550,Run.Player.Position));Check(boss.Phase==2,"29: phase two at 70 percent");
        Check(Run.Room.Torches.FindAll(t=>t.Lit).Count==3,"first torch extinguishes before second");await Frames(95);
        Check(Run.Room.Torches.FindAll(t=>t.Lit).Count==2,"30: phase two extinguishes two torches");
        boss.TakeDamage(new DamageInfo(550,Run.Player.Position));Check(boss.Phase==3,"29: phase three at 40 percent");
        Check(Run.Room.Torches.FindAll(t=>t.Lit).Count==1,"third torch extinguishes before final blackout");await Frames(95);
        Check(Run.Room.Torches.TrueForAll(t=>!t.Lit),"30: phase three extinguishes remaining torches");
        boss.FrozenForTest=false;boss.BeginAttack();Check(boss.Warning>=.8f,"boss telegraph provides fair reaction window");
        Run.Player.Position=new Vector2(260,250);float dodgeFlame=Run.Player.Flame.Current;await Frames(65);
        Check(Run.Player.Flame.Current==dodgeFlame,"boss slam can be dodged");
        boss.TakeDamage(new DamageInfo(5000,Run.Player.Position));Check(boss.Dead&&Run.State==RunState.Victory,"31-32: boss death opens Victory");
        Click("Retry");await Frames(3);
        Check(Run.Playing&&Run.Kills==0&&Run.Player.Build.Artifacts.Count==0&&Run.Lights.Count==1,"35: new run after Victory resets enemies, lights and artifacts");
        var fp=new FlamePool();Check(!fp.ChangeMaximum(-80)&&fp.Maximum==100,"maximum Flame floor enforced");
        fp.Damage(90);Check(!fp.Spend(10)&&fp.Current==10,"ability cannot spend last Flame");
        var stats=new BuildStats();Check(stats.Sacrifice(0,fp)&&stats.AttackSpeed==1.25f,"altar attack speed option");Check(!stats.Sacrifice(1,fp),"altar cannot be reused");
        stats=new BuildStats();fp=new FlamePool();Check(stats.Sacrifice(2,fp)&&stats.DashExplosion,"altar dash explosion option");
        // Verify the actual third-hit hook and dash explosion rather than only their flags.
        Run.StartRun(8801);await Frames(3);FreezeEnemies();player=Run.Player;
        player.Position=new Vector2(240,550);player.Build.Apply(Item("burning_edge"),player.Flame);
        var thirdHitTarget=Run.Spawn(EnemyKind.Torchbearer,new Vector2(320,550));thirdHitTarget.FrozenForTest=true;player.Aim=Vector2.Right;
        player.TryMelee();await Frames(32);player.TryMelee();Check(!thirdHitTarget.Burn.Active,"Burning Edge does not ignite first two hits");
        await Frames(32);player.TryMelee();Check(thirdHitTarget.Burn.Active,"Burning Edge ignites actual third melee hit");
        player.Build.Sacrifice(2,player.Flame);float dashTargetHealth=thirdHitTarget.Health;player.TryDash(Vector2.Down);
        Check(thirdHitTarget.Health==dashTargetHealth-26,"altar dash explosion deals damage");
        await Frames(15);
        // World masks and line-of-sight prevent hits across pillars.
        var wall=Run.Room.Obstacles[0];player.Position=new Vector2(wall.Position.X-32,wall.GetCenter().Y);
        player.TestMovement=Vector2.Right;await Frames(30);player.TestMovement=Vector2.Zero;
        Check(player.Position.X<wall.Position.X-13,"CharacterBody2D collides with world geometry");
        Check(!Run.Room.HasLineOfSight(wall.Position-new Vector2(20,-20),new Vector2(wall.End.X+20,wall.Position.Y+20)),"combat line-of-sight blocked by pillars");
        // Same seed recreates the room and roster; distinct seeds vary the run.
        string Signature()
        {string s=Run.Room.Layout.ToString();foreach(var e in Run.Enemies)s+=$"|{e.Kind}:{e.Position}";return s;}
        Run.StartRun(130);string first=Signature();Run.StartRun(130);Check(Signature()==first,"seed reproduces layout and enemy composition");
        Run.StartRun(131);Check(Signature()!=first,"different seed varies run");
        var layouts=new HashSet<string>();for(int i=0;i<8;i++)layouts.Add(string.Join(";",Room.LayoutObstacles(i)));
        Check(layouts.Count==8,"eight distinct handcrafted layouts");
        Run.ShowMenu();Check(Run.State==RunState.Menu&&!GetTree().Paused,"main menu return clears pause");
    }
    private void WriteResult(bool success)
    {
        var path=ProjectSettings.GlobalizePath("res://TestResults");System.IO.Directory.CreateDirectory(path);
        System.IO.File.WriteAllText(System.IO.Path.Combine(path,"integration.txt"),$"{(success?"PASS":"FAIL")} — {_checks} assertions\nGodot {Engine.GetVersionInfo()["string"]}\n"+string.Join("\n",_results));
        GD.Print($"TEST RESULT: {(success?"PASS":"FAIL")} ({_checks} assertions)");
        var license=new System.Text.StringBuilder(Engine.GetLicenseText());
        license.AppendLine("\n\nDEPENDENCY COPYRIGHT NOTICES\n").AppendLine(Json.Stringify(Engine.GetCopyrightInfo(),"  "));
        foreach(var entry in Engine.GetLicenseInfo())license.AppendLine("\n\n"+entry.Key+"\n").AppendLine(entry.Value.ToString());
        System.IO.File.WriteAllText(System.IO.Path.Combine(path,"GODOT-LICENSES.txt"),license.ToString());
    }
}
