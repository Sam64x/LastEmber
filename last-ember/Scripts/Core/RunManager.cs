using System;
using System.Collections.Generic;
using Godot;

namespace LastEmber;

public enum RunState { Menu, Playing, Reward, Altar, Pause, GameOver, Victory }
public enum StageKind { Combat, Reward, Elite, Altar, Boss }

public partial class RunManager : Node
{
    public static readonly StageKind[] Route = { StageKind.Combat, StageKind.Combat, StageKind.Reward, StageKind.Combat, StageKind.Elite, StageKind.Altar, StageKind.Combat, StageKind.Reward, StageKind.Combat, StageKind.Boss };
    public RunState State { get; private set; } = RunState.Menu;
    public bool Playing => State == RunState.Playing;
    public int StageIndex { get; private set; }
    public StageKind CurrentStage => Route[Math.Clamp(StageIndex,0,Route.Length-1)];
    public int Kills { get; private set; }
    public float RunTime { get; private set; }
    public ulong Seed { get; private set; }
    public Player Player { get; private set; } = null!;
    public Room Room { get; private set; } = null!;
    public Hud Hud { get; private set; } = null!;
    public Effects Fx { get; private set; } = null!;
    public GameAudio Audio { get; private set; } = null!;
    public MusicDirector Music { get; private set; } = null!;
    public List<Enemy> Enemies { get; } = new();
    public List<EmberLight> Lights { get; } = new();
    public List<ArtifactData> Artifacts { get; } = new();
    public List<ArtifactData> Offered { get; } = new();
    public bool TestMode { get; private set; }
    private Node2D _world = null!, _transient = null!;
    private Camera2D _camera = null!;
    private readonly RandomNumberGenerator _rng = new();
    private readonly List<FirePatch> _fires = new();
    private float _shake, _clearDelay, _waveDelay;
    private int _wavesRemaining;
    private bool _stageResolved, _roomRewardTaken;
    private sealed class FirePatch { public Vector2 Position; public float Life=1.3f, Tick; }

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        SetupInput();
        foreach(var file in DirAccess.GetFilesAt("res://Resources/Artifacts"))
            if(file.EndsWith(".tres")) Artifacts.Add(ResourceLoader.Load<ArtifactData>("res://Resources/Artifacts/"+file));
        Artifacts.Sort((a,b)=>string.CompareOrdinal(a.Id,b.Id));
        Audio = new GameAudio(); AddChild(Audio);
        Music = new MusicDirector { Run=this };AddChild(Music);Audio.StrongSoundPlayed+=Music.Duck;
        Hud = new Hud { Run = this }; AddChild(Hud);
        ShowMenu();
        var args = OS.GetCmdlineUserArgs();
        TestMode = Array.Exists(args,a=>a=="--self-test");
        if (TestMode) CallDeferred(MethodName.StartTests);
        else if (Array.Exists(args,a=>a=="--playtest")) CallDeferred(MethodName.StartPlaytest);
        else if (Array.Exists(args,a=>a=="--music-test")) CallDeferred(MethodName.StartMusicTests);
        else if (Array.Exists(args,a=>a=="--capture")) CallDeferred(MethodName.StartCapture);
    }
    private static void SetupInput()
    {
        void KeyAction(string name,Key key) { if(!InputMap.HasAction(name)) InputMap.AddAction(name); InputMap.ActionAddEvent(name,new InputEventKey { PhysicalKeycode=key }); }
        KeyAction("left",Key.A);KeyAction("right",Key.D);KeyAction("up",Key.W);KeyAction("down",Key.S);
        KeyAction("dash",Key.Space);KeyAction("pause",Key.Escape);KeyAction("interact",Key.E);
        foreach(var pair in new[] {("melee",MouseButton.Left),("burst",MouseButton.Right)})
        { if(!InputMap.HasAction(pair.Item1)) InputMap.AddAction(pair.Item1); InputMap.ActionAddEvent(pair.Item1,new InputEventMouseButton { ButtonIndex=pair.Item2 }); }
    }
    public override void _UnhandledInput(InputEvent input)
    {
        if(input.IsActionPressed("pause")) { TogglePause(); GetViewport().SetInputAsHandled(); }
        if(input.IsActionPressed("interact") && Playing)
        {
            if(CurrentStage==StageKind.Altar && Player.Position.DistanceTo(new Vector2(960,556))<150 && !_stageResolved) OpenAltar();
            else if(Room.Cleared && Player.Position.X>1670) AdvanceStage();
        }
    }
    private void NewWorld()
    {
        if(IsInstanceValid(_world)) { RemoveChild(_world); _world.QueueFree(); }
        Enemies.Clear();Lights.Clear();_fires.Clear();
        _world = new Node2D { ProcessMode = ProcessModeEnum.Pausable }; AddChild(_world); MoveChild(_world,0);
        _world.AddChild(new CanvasModulate { Color=new Color(.40f,.41f,.46f) });
        _camera = new Camera2D { Position=new Vector2(960,540),PositionSmoothingEnabled=true,PositionSmoothingSpeed=6 }; _world.AddChild(_camera);
        Fx = new Effects();_world.AddChild(Fx);
    }
    public void ShowMenu()
    {
        GetTree().Paused=false; State=RunState.Menu; StageIndex=0;
        NewWorld();
        Room = ResourceLoader.Load<PackedScene>("res://Scenes/Rooms/Room.tscn").Instantiate<Room>();
        Room.Run=this;Room.Layout=0;_world.AddChild(Room);
        _world.AddChild(new EmberLight { Position=new Vector2(960,540),TargetRadius=650 });
        Hud.ShowMenu();
    }
    public void StartRun(ulong? seed=null)
    {
        GetTree().Paused=false; State=RunState.Playing;
        Seed=seed??(ulong)DateTime.UtcNow.Ticks;_rng.Seed=Seed;
        StageIndex=0;Kills=0;RunTime=0;Offered.Clear();_shake=0;
        NewWorld();
        Player=new Player { Run=this,Position=new Vector2(260,556),Automated=TestMode };_world.AddChild(Player);Lights.Add(Player.Light);
        Music.ResetForRun();
        LoadStage(); Hud.ShowHud();
    }
    private void LoadStage()
    {
        if(IsInstanceValid(Room) && Room.GetParent()==_world) { _world.RemoveChild(Room);Room.QueueFree(); }
        if(IsInstanceValid(_transient) && _transient.GetParent()==_world) { _world.RemoveChild(_transient);_transient.QueueFree(); }
        Enemies.Clear();Lights.Clear();Lights.Add(Player.Light);_fires.Clear();
        _transient=new Node2D();_world.AddChild(_transient);
        Room=ResourceLoader.Load<PackedScene>("res://Scenes/Rooms/Room.tscn").Instantiate<Room>();
        Room.Run=this;Room.Layout=_rng.RandiRange(0,7);Room.BossArena=CurrentStage==StageKind.Boss;
        _world.AddChild(Room);_world.MoveChild(Room,1);
        Player.Position=new Vector2(250,556);Player.Velocity=Vector2.Zero;
        _stageResolved=false;_roomRewardTaken=false;_clearDelay=1;_waveDelay=2;
        _wavesRemaining=CurrentStage==StageKind.Combat?2+StageIndex/3:0;
        Hud.Toast(CurrentStage switch {StageKind.Elite=>"A STOLEN SUN • TORCHBEARER",StageKind.Altar=>"THE ALTAR • APPROACH AND PRESS E",StageKind.Boss=>"THE FURNACE • THE EXTINGUISHER",StageKind.Reward=>"A MOMENT OF WARMTH",_=>$"DISTRICT {StageIndex+1:00} • CLEAR THE ASH"});
        if(CurrentStage==StageKind.Boss) Spawn(EnemyKind.Boss,new Vector2(1390,556));
        else if(CurrentStage==StageKind.Elite) { Spawn(EnemyKind.Torchbearer,new Vector2(1350,550));SpawnWave(4); }
        else if(CurrentStage==StageKind.Combat) SpawnWave(5+StageIndex);
        else if(CurrentStage==StageKind.Reward) { _stageResolved=true;OpenRewards(); }
    }
    public Enemy Spawn(EnemyKind kind,Vector2 position)
    {
        Enemy enemy=kind==EnemyKind.Boss?new Extinguisher():new Enemy {Kind=kind};
        enemy.Run=this;enemy.Position=position;Enemies.Add(enemy);_transient.AddChild(enemy);
        Fx.Ring(position,45,new Color(.75f,.3f,.2f));
        return enemy;
    }
    private void SpawnWave(int count)
    {
        for(int i=0;i<count;i++)
        {
            Vector2 position=new(1500,800);
            for(int attempt=0;attempt<100;attempt++)
            { position=new Vector2(_rng.RandfRange(340,1700),_rng.RandfRange(240,860)); if(Room.IsFree(position)&&position.DistanceTo(Player.Position)>320) break; }
            if(!Room.IsFree(position)||position.DistanceTo(Player.Position)<220) position=new Vector2(1690,250+i*45);
            int maximum=StageIndex==0?1:StageIndex==1?2:4;
            Spawn((EnemyKind)_rng.RandiRange(0,maximum),position);
        }
    }
    public override void _Process(double delta)
    {
        if(!Playing)return;
        float dt=(float)delta;RunTime+=dt;
        _shake=Mathf.MoveToward(_shake,0,dt*24);
        _camera.Offset = new Vector2(Mathf.Sin(RunTime*97),Mathf.Cos(RunTime*83))*_shake;
        // Mild camera follow, bounded so all doors remain in the viewport.
        _camera.Position = new Vector2(960,540)+(Player.Position-new Vector2(960,540))*.012f;
        for(int i=_fires.Count-1;i>=0;i--)
        {
            var fire=_fires[i];fire.Life-=dt;fire.Tick-=dt;
            if(fire.Life<=0){_fires.RemoveAt(i);continue;}
            if(fire.Tick<=0)
            {
                fire.Tick=.3f;Fx.Sparks(fire.Position,new Color(1,.45f,.1f),2);
                foreach(var enemy in Enemies.ToArray()) if(!enemy.Dead&&enemy.Position.DistanceTo(fire.Position)<42) enemy.TakeDamage(new DamageInfo(4,fire.Position,0,true));
            }
        }
        if(!_stageResolved && CurrentStage!=StageKind.Altar && Enemies.Count==0)
        {
            if(_wavesRemaining>0)
            {
                _waveDelay-=dt;
                if(_waveDelay<=0) { _wavesRemaining--;_waveDelay=2;SpawnWave(4+StageIndex);Hud.Toast("MORE SHADOWS GATHER"); }
            }
            else
            {
                _clearDelay-=dt;
                if(_clearDelay<=0) CompleteRoom();
            }
        }
        if(Room.Cleared && Player.Position.X>1775 && Mathf.Abs(Player.Position.Y-554)<85) AdvanceStage();
    }
    public bool IsLit(Vector2 position)
    {
        foreach(var light in Lights) if(IsInstanceValid(light)&&light.Contains(position))return true;
        return false;
    }
    public void BreakTethers() {foreach(var enemy in Enemies)enemy.BreakTether();}
    public void AddFire(Vector2 position) { if(_fires.Count<60)_fires.Add(new FirePatch {Position=position}); }
    public void Shoot(Vector2 origin,Vector2 target,float speed,float damage)
        =>_transient.AddChild(new Projectile {Run=this,Position=origin,Velocity=(target-origin).Normalized()*speed,Damage=damage});
    public void Explode(Vector2 origin,float radius,float damage,bool burn)
    {
        Fx.Ring(origin,radius,new Color(1,.58f,.19f));Fx.Sparks(origin,new Color(1,.5f,.1f),24);
        foreach(var enemy in Enemies.ToArray())
            if(!enemy.Dead&&enemy.Position.DistanceTo(origin)<radius+enemy.BodyRadius&&Room.HasLineOfSight(origin,enemy.Position)) enemy.TakeDamage(new DamageInfo(damage,origin,320,burn));
    }
    public void OnEnemyKilled(Enemy enemy,bool burning)
    {
        Enemies.Remove(enemy);Kills++;
        float heal=enemy.Kind switch{EnemyKind.Moth=>2,EnemyKind.Shade=>4,EnemyKind.Leech=>5,EnemyKind.Torchbearer=>12,EnemyKind.Boss=>0,_=>3};
        if(burning)heal+=Player.Build.Get(ArtifactEffect.BurnHeal);
        Player.Flame.Heal(heal);
        Fx.Sparks(enemy.Position,new Color(1,.48f,.13f),20);
        if(heal>0)Fx.Text(enemy.Position,$"+{heal:0} FLAME",new Color(1,.64f,.25f));
        if(burning&&Player.Build.Has(ArtifactEffect.BurnExplosion))Explode(enemy.Position,95,18,false);
        if(enemy.Kind==EnemyKind.Boss)EndRun(true);
    }
    public void Shake(float amount)=>_shake=Mathf.Max(_shake,amount);
    private void CompleteRoom()
    {
        _stageResolved=true;Room.Cleared=true;Room.QueueRedraw();Audio.Play("reward");
        // One reward per completed encounter; no farming by walking back through a door.
        OpenRewards();
    }
    public void AdvanceStage()
    {
        if(!Playing||!Room.Cleared)return;
        StageIndex++;
        if(StageIndex>=Route.Length){EndRun(true);return;}
        LoadStage();
    }
    public void OpenRewards()
    {
        if(_roomRewardTaken)return;
        Offered.Clear();
        var pool=new List<ArtifactData>();
        foreach(var item in Artifacts)if(item.Stackable||!Player.Build.Owns(item.Id))pool.Add(item);
        while(Offered.Count<3&&pool.Count>0)
        {
            float total=0; foreach(var item in pool)total+=RewardWeight(item);
            float roll=_rng.Randf()*total;var chosen=pool[^1];
            foreach(var item in pool){roll-=RewardWeight(item);if(roll<=0){chosen=item;break;}}
            Offered.Add(chosen);pool.Remove(chosen);
        }
        if(Offered.Count==0){_roomRewardTaken=true;Room.Cleared=true;Room.QueueRedraw();Hud.Toast("ALL RELICS CLAIMED • THE EXIT IS OPEN");return;}
        State=RunState.Reward;GetTree().Paused=true;Hud.ShowRewards();
    }
    public float RewardWeight(ArtifactData item)=>Player.Build.Has(ArtifactEffect.DarkRewards)?1+item.Rarity*1.5f:4-item.Rarity*.65f;
    public bool ChooseReward(int index)
    {
        if(State!=RunState.Reward||index<0||index>=Offered.Count)return false;
        if(!Player.Build.Apply(Offered[index],Player.Flame))return false;
        _roomRewardTaken=true;Room.Cleared=true;Room.QueueRedraw();Offered.Clear();
        State=RunState.Playing;GetTree().Paused=false;Hud.ShowHud();Audio.Play("reward");Hud.Toast("RELIC BOUND • CROSS THE EASTERN GATE");return true;
    }
    public void OpenAltar()
    {
        if(!Playing||CurrentStage!=StageKind.Altar||_stageResolved)return;
        State=RunState.Altar;GetTree().Paused=true;Hud.ShowAltar();
    }
    public bool ChooseAltar(int option)
    {
        if(State!=RunState.Altar)return false;
        if(option>=0&&!Player.Build.Sacrifice(option,Player.Flame))return false;
        _stageResolved=true;Room.Cleared=true;Room.QueueRedraw();State=RunState.Playing;GetTree().Paused=false;Hud.ShowHud();return true;
    }
    public void TogglePause()
    {
        if(State==RunState.Playing){State=RunState.Pause;GetTree().Paused=true;Hud.ShowPause();}
        else if(State==RunState.Pause){State=RunState.Playing;GetTree().Paused=false;Hud.ShowHud();}
    }
    public void EndRun(bool victory)
    {
        if(State is RunState.GameOver or RunState.Victory or RunState.Menu)return;
        State=victory?RunState.Victory:RunState.GameOver;GetTree().Paused=true;Hud.ShowEnd(victory);
    }
    private void StartTests() {AddChild(new SmokeTests { Run=this });}
    private void StartCapture() {AddChild(new CaptureScenes { Run=this });}
    private void StartPlaytest() {AddChild(new BotPlaytest { Run=this });}
    private void StartMusicTests() {AddChild(new MusicTests { Run=this });}
}
