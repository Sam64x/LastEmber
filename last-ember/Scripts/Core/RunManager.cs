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
    public DungeonController Dungeon { get; private set; } = null!;
    public List<DungeonDefinition> Dungeons { get; } = new();
    public int DungeonIndex { get; private set; }
    private CanvasModulate _ambient=null!;
    public Player Player { get; private set; } = null!;
    public Room Room { get; private set; } = null!;
    public Hud Hud { get; private set; } = null!;
    public Effects Fx { get; private set; } = null!;
    public GameAudio Audio { get; private set; } = null!;
    public MusicDirector Music { get; private set; } = null!;
    public List<Enemy> Enemies { get; } = new();
    public List<EmberLight> Lights { get; } = new();
    public List<ArtifactData> Artifacts { get; } = new();
    public List<RewardOption> Offered { get; } = new();
    public RewardGenerator Rewards { get; } = new();
    public bool DevEnabled { get; private set; }
    public bool DevInvulnerable { get; set; }
    public DevPanel DevTools { get; private set; } = null!;
    public bool TestMode { get; private set; }
    public bool ShrineAvailable => _stageResolved && !_roomRewardTaken && CurrentStage!=StageKind.Altar && CurrentStage!=StageKind.Boss && StageIndex>0;
    private Node2D _world = null!, _transient = null!;
    private Camera2D _camera = null!;
    private readonly RandomNumberGenerator _rng = new();
    private readonly List<FirePatch> _fires = new();
    private float _shake, _clearDelay, _waveDelay;
    private float _hitStop;
    private int _wavesRemaining;
    private bool _stageResolved, _roomRewardTaken;
    private sealed class FirePatch { public Vector2 Position; public float Life=1.3f, Tick; }

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        SetupInput();
        foreach(var file in ResourceLoader.ListDirectory("res://Resources/Dungeons"))
            if(file.EndsWith(".tres"))Dungeons.Add(ResourceLoader.Load<DungeonDefinition>("res://Resources/Dungeons/"+file));
        Dungeons.Sort((a,b)=>string.CompareOrdinal(a.ResourcePath,b.ResourcePath));
        foreach(var file in ResourceLoader.ListDirectory("res://Resources/Artifacts"))
            if(file.EndsWith(".tres")) Artifacts.Add(ResourceLoader.Load<ArtifactData>("res://Resources/Artifacts/"+file));
        Artifacts.Sort((a,b)=>string.CompareOrdinal(a.Id,b.Id));
        Rewards.Load();
        Audio = new GameAudio(); AddChild(Audio);
        Music = new MusicDirector { Run=this };AddChild(Music);Audio.StrongSoundPlayed+=Music.Duck;
        Hud = new Hud { Run = this }; AddChild(Hud);
        DevTools=new DevPanel {Run=this};AddChild(DevTools);
        ShowMenu();
        var args = OS.GetCmdlineUserArgs();
        TestMode = Array.Exists(args,a=>a=="--self-test");
        if (TestMode) CallDeferred(MethodName.StartTests);
        else if (Array.Exists(args,a=>a=="--attunement-capture")) CallDeferred(MethodName.StartAttunementCapture);
        else if (Array.Exists(args,a=>a=="--attunement-test")) CallDeferred(MethodName.StartAttunementTests);
        else if (Array.Exists(args,a=>a=="--playtest")) CallDeferred(MethodName.StartPlaytest);
        else if (Array.Exists(args,a=>a=="--music-test")) CallDeferred(MethodName.StartMusicTests);
        else if (Array.Exists(args,a=>a=="--audio-capture")) CallDeferred(MethodName.StartAudioCapture);
        else if (Array.Exists(args,a=>a=="--capture")) CallDeferred(MethodName.StartCapture);
    }
    private static void SetupInput()
    {
        void KeyAction(string name,Key key) { if(!InputMap.HasAction(name)) InputMap.AddAction(name); InputMap.ActionAddEvent(name,new InputEventKey { PhysicalKeycode=key }); }
        KeyAction("left",Key.A);KeyAction("right",Key.D);KeyAction("up",Key.W);KeyAction("down",Key.S);
        KeyAction("dash",Key.Space);KeyAction("pause",Key.Escape);KeyAction("interact",Key.E);
        KeyAction("reveal",Key.Q);KeyAction("dev_tools",Key.F1);
        foreach(var pair in new[] {("melee",MouseButton.Left)})
        { if(!InputMap.HasAction(pair.Item1)) InputMap.AddAction(pair.Item1); InputMap.ActionAddEvent(pair.Item1,new InputEventMouseButton { ButtonIndex=pair.Item2 }); }
    }
    public override void _Input(InputEvent input)
    {
        if(!Playing||!IsInstanceValid(Player)||Player.Automated)return;
        if(input.IsActionPressed("melee"))Player.CaptureMeleeInput(true);
        else if(input.IsActionReleased("melee"))Player.CaptureMeleeInput(false);
    }
    public override void _Notification(int what)
    {
        if(what==NotificationApplicationFocusOut&&IsInstanceValid(Player))Player.SuppressUiClick();
    }
    public override void _UnhandledInput(InputEvent input)
    {
        if(input.IsActionPressed("dev_tools") && DevEnabled)
        {DevTools.Toggle();GetViewport().SetInputAsHandled();return;}
        if(input.IsActionPressed("pause") && DevTools.Open)
        {DevTools.Close();GetViewport().SetInputAsHandled();return;}
        if(input.IsActionPressed("pause")) { TogglePause(); GetViewport().SetInputAsHandled(); }
        if(input.IsActionPressed("interact") && Playing)
        {
            if(ShrineAvailable && Player.Position.DistanceTo(Room.ShrinePosition)<90) OpenRewards();
            else if(CurrentStage==StageKind.Altar && Player.Position.DistanceTo(Room.Bounds.GetCenter())<110 && !_stageResolved) OpenAltar();
            else if(Room.Cleared && Player.Position.DistanceTo(Room.ExitPosition)<100 && Room.HasLineOfSight(Player.Position,Room.ExitPosition)) AdvanceStage();
        }
    }
    private void NewWorld()
    {
        DevTools.Close();
        _hitStop=0;
        if(IsInstanceValid(_world)) { RemoveChild(_world); _world.QueueFree(); }
        Enemies.Clear();Lights.Clear();_fires.Clear();
        _world = new Node2D { ProcessMode = ProcessModeEnum.Pausable }; AddChild(_world); MoveChild(_world,0);
        _ambient=new CanvasModulate {Color=Colors.Black};_world.AddChild(_ambient);
        Dungeon=new DungeonController {Run=this};_world.AddChild(Dungeon);
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
    public void StartRun(ulong? seed=null,int dungeonIndex=0)
    {
        GetTree().Paused=false; State=RunState.Playing;
        Seed=seed??(ulong)DateTime.UtcNow.Ticks;_rng.Seed=Seed;
        StageIndex=0;Kills=0;RunTime=0;Offered.Clear();_shake=0;
        NewWorld();
        Player=new Player { Run=this,Position=new Vector2(260,556),Automated=TestMode };_world.AddChild(Player);Lights.Add(Player.Light);
        DungeonIndex=Math.Clamp(dungeonIndex,0,Dungeons.Count-1);
        Dungeon.Enter(Dungeons[DungeonIndex]);_ambient.Color=Dungeon.Definition.Ambient;
        Music.ResetForRun();
        LoadStage(); Hud.ShowHud();
    }
    private void LoadStage()
    {
        if(IsInstanceValid(Room) && Room.GetParent()==_world) { _world.RemoveChild(Room);Room.QueueFree(); }
        if(IsInstanceValid(_transient) && _transient.GetParent()==_world) { _world.RemoveChild(_transient);_transient.QueueFree(); }
        Enemies.Clear();Player.ResetForRoom();Fx.ClearForRoom();Lights.Clear();Lights.Add(Player.Light);_fires.Clear();
        _transient=new Node2D();_world.AddChild(_transient);
        var rooms=Dungeon.Definition.Rooms;
        Room=(rooms.Count>0?rooms[StageIndex%rooms.Count]:ResourceLoader.Load<PackedScene>("res://Scenes/Rooms/Room.tscn")).Instantiate<Room>();
        Room.Run=this;Room.Layout=StageIndex<=3?StageIndex%8:_rng.RandiRange(0,7);Room.BossArena=CurrentStage==StageKind.Boss;
        _world.AddChild(Room);_world.MoveChild(Room,1);
        Dungeon.Populate(Room);
        Player.Position=Room.EntrancePosition;Player.Velocity=Vector2.Zero;
        _stageResolved=false;_roomRewardTaken=false;_clearDelay=1;_waveDelay=2;
        _wavesRemaining=CurrentStage==StageKind.Combat&&StageIndex>=3?1:0;
        Hud.Toast(CurrentStage switch {StageKind.Elite=>"A STOLEN SUN • TORCHBEARER",StageKind.Altar=>"THE ALTAR • APPROACH AND PRESS E",StageKind.Boss=>"THE FURNACE • THE EXTINGUISHER",StageKind.Reward=>"A MOMENT OF WARMTH",_=>$"DISTRICT {StageIndex+1:00} • CLEAR THE ASH"});
        if(CurrentStage==StageKind.Boss) Spawn(EnemyKind.Boss,new Vector2(1390,556));
        else if(CurrentStage==StageKind.Elite) { Spawn(EnemyKind.Torchbearer,Room.MapPoint(new Vector2(1350,550)));SpawnWave(3); }
        else if(CurrentStage==StageKind.Combat && StageIndex>0) SpawnWave(StageIndex==1?2:4+StageIndex/3);
        else if(CurrentStage==StageKind.Reward) { _stageResolved=true;Room.QueueRedraw(); }
        if(StageIndex==0) { _stageResolved=true;_roomRewardTaken=true;Room.Cleared=true;Room.QueueRedraw();Hud.Toast("WASD • FOLLOW YOUR LIGHT TO THE EASTERN GATE"); }
        else if(StageIndex==1)Hud.Toast("Q • LIGHT SLOWS THE SHADES • COSTS 5 FLAME");
        else if(StageIndex==3)Hud.Toast("LISTEN • SOME CREATURES ANSWER THE LIGHT");
        if(CurrentStage is StageKind.Combat or StageKind.Elite)
            Hud.Toast(Dungeon.Definition.DisplayName+" • "+Dungeon.Definition.Lesson);
    }
    public Enemy Spawn(EnemyKind kind,Vector2 position)
    {
        Enemy enemy=kind==EnemyKind.Boss?(Dungeon.Definition.Boss?.Instantiate<Enemy>() ?? new Extinguisher()):new Enemy {Kind=kind};

        enemy.Run=this;enemy.Position=position;Enemies.Add(enemy);_transient.AddChild(enemy);
        enemy.ContactDamage*=Mathf.Max(0,Dungeon.Definition.EnemyDamageMultiplier);
        Fx.Ring(position,45,new Color(.75f,.3f,.2f));
        return enemy;
    }
    private void SpawnWave(int count)
    {
        for(int i=0;i<count;i++)
        {
            Vector2 position=new(1500,800);
            for(int attempt=0;attempt<100;attempt++)
            { position=new Vector2(_rng.RandfRange(Room.Bounds.Position.X+65,Room.Bounds.End.X-65),_rng.RandfRange(Room.Bounds.Position.Y+65,Room.Bounds.End.Y-65)); if(Room.IsFree(position)&&position.DistanceTo(Player.Position)>320) break; }
            if(!Room.IsFree(position)||position.DistanceTo(Player.Position)<220) continue;
            var types=Dungeon.Definition.Enemies;
            Spawn(types.Count>0?types[i%types.Count]:EnemyKind.Shade,position);
        }
    }
    public override void _Process(double delta)
    {
        if(_hitStop>0)
        {
            _hitStop=Mathf.Max(0,_hitStop-(float)delta);
            if(_hitStop<=0&&IsInstanceValid(_world))_world.ProcessMode=ProcessModeEnum.Pausable;
            return;
        }
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
                fire.Tick=.3f;Fx.Sparks(fire.Position,FlamePalette.Fire(Player.Flame.LastEmber),2);
                foreach(var enemy in Enemies.ToArray()) if(!enemy.Dead&&enemy.Position.DistanceTo(fire.Position)<42) enemy.TakeDamage(new DamageInfo(4*Player.Flame.LastEmberDamageMultiplier,fire.Position,0,true));
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
        if(Room.Cleared && Player.Position.DistanceTo(Room.ExitPosition)<44) AdvanceStage();
    }
    public bool IsLit(Vector2 position)
    {
        foreach(var light in Lights) if(IsInstanceValid(light)&&light.Contains(position))return true;
        return false;
    }
    public void BreakTethers() {foreach(var enemy in Enemies)enemy.BreakTether();}
    public void AddFire(Vector2 position) { if(_fires.Count<60)_fires.Add(new FirePatch {Position=position}); }
    public void Shoot(Vector2 origin,Vector2 target,float speed,float damage,bool fire=false)
        =>_transient.AddChild(new Projectile {Run=this,Position=origin,Velocity=(target-origin).Normalized()*speed,Damage=damage,Fire=fire});
    public void Explode(Vector2 origin,float radius,float damage,bool burn)
    {
        Fx.Ring(origin,radius,FlamePalette.Fire(Player.Flame.LastEmber));Fx.Sparks(origin,FlamePalette.Fire(Player.Flame.LastEmber),24);
        foreach(var enemy in Enemies.ToArray())
            if(!enemy.Dead&&enemy.Position.DistanceTo(origin)<radius+enemy.BodyRadius&&Room.HasLineOfSight(origin,enemy.Position)) enemy.TakeDamage(new DamageInfo(damage*Player.Flame.LastEmberDamageMultiplier,origin,320,burn));
    }
    public void OnEnemyKilled(Enemy enemy,bool burning)
    {
        Enemies.Remove(enemy);Kills++;
        Player.Melee.OnEnemyKilled(enemy,burning);
        if(enemy.Kind!=EnemyKind.Boss && _rng.Randf()<.28f)
        {
            float amount=_rng.RandiRange(3,5);
            if(burning)amount+=Player.Build.Get(ArtifactEffect.BurnHeal);
            _transient.AddChild(new EmberPickup {Run=this,Position=enemy.Position,Amount=amount});
        }
        Fx.Sparks(enemy.Position,FlamePalette.Fire(Player.Flame.LastEmber),20);
        if(burning&&Player.Build.Has(ArtifactEffect.BurnExplosion))Explode(enemy.Position,95,18,false);
        if(enemy.Kind==EnemyKind.Boss)
        {
            if(DungeonIndex+1<Dungeons.Count)CallDeferred(MethodName.NextDungeon);
            else EndRun(true);
        }
    }
    public void Shake(float amount)=>_shake=Mathf.Max(_shake,amount);
    public void HitStop(float seconds)
    {
        if(!Playing||!IsInstanceValid(_world))return;
        _hitStop=Mathf.Max(_hitStop,Mathf.Clamp(seconds,0,.06f));
        _world.ProcessMode=ProcessModeEnum.Disabled;
    }
    private void CompleteRoom()
    {
        _stageResolved=true;Room.QueueRedraw();Audio.Play("reward");
        Hud.Toast("EMBER SHRINE • RESTORE OR CLAIM A RELIC • E TO CHOOSE");
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
        if(!Playing||!ShrineAvailable||Player.Position.DistanceTo(Room.ShrinePosition)>=90)return;
        Offered.Clear();
        Offered.AddRange(Rewards.Generate(Player,Artifacts,_rng,RewardWeight));
        State=RunState.Reward;GetTree().Paused=true;Hud.ShowRewards();
    }
    public float RewardWeight(ArtifactData item)=>Player.Build.Has(ArtifactEffect.DarkRewards)?1+item.Rarity*1.5f:4-item.Rarity*.65f;
    public bool ChooseReward(int index)
    {
        if(State!=RunState.Reward||index<0||index>=Offered.Count)return false;
        if(!Offered[index].Acquire(Player))return false;
        _roomRewardTaken=true;Room.Cleared=true;Room.QueueRedraw();Offered.Clear();
        State=RunState.Playing;GetTree().Paused=false;Hud.ShowHud();Audio.Play("reward");Hud.Toast("RELIC BOUND • CROSS THE EASTERN GATE");return true;
    }
    public bool ChooseRestore()
    {
        if(State!=RunState.Reward||_roomRewardTaken)return false;
        Player.Flame.Heal(20);_roomRewardTaken=true;Room.Cleared=true;Room.QueueRedraw();Offered.Clear();
        State=RunState.Playing;GetTree().Paused=false;Hud.ShowHud();Audio.Play("reward");
        Hud.Toast("FLAME RESTORED • THE RELICS RETURN TO ASH");return true;
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
        if(State==RunState.Playing){State=RunState.Pause;Player.SuppressUiClick();GetTree().Paused=true;Hud.ShowPause();}
        else if(State==RunState.Pause){State=RunState.Playing;GetTree().Paused=false;Hud.ShowHud();}
    }
    public void EndRun(bool victory)
    {
        if(State is RunState.GameOver or RunState.Victory or RunState.Menu)return;
        State=victory?RunState.Victory:RunState.GameOver;GetTree().Paused=true;Hud.ShowEnd(victory);
    }
    public void SwitchDungeon(int index)
    {
        if(!Playing || index<0 || index>=Dungeons.Count)return;
        Player.ResetForRoom();DungeonIndex=index;Dungeon.Enter(Dungeons[index]);_ambient.Color=Dungeon.Definition.Ambient;
        StageIndex=0;LoadStage();
    }
    public void SetDevEnabled(bool enabled)
    {
        DevEnabled=enabled;
        if(!enabled){DevInvulnerable=false;DevTools.Close();}
    }
    public void DevSpawnTarget(bool armored=false,bool fire=false)
    {
        if(!DevEnabled || State is not (RunState.Playing or RunState.Pause))return;
        for(int i=0;i<12;i++)
        {
            var point=Player.Position+Vector2.FromAngle(i*Mathf.Tau/12)*115;
            if(!Room.IsFree(point,24)||Enemies.Exists(enemy=>!enemy.Dead&&enemy.Position.DistanceTo(point)<53))continue;
            var enemy=new Enemy {Run=this,Position=point,Kind=EnemyKind.Ashling,MaxHealth=160,TrainingDummy=true,IceArmored=armored,FireAligned=fire};
            Enemies.Add(enemy);_transient.AddChild(enemy);return;
        }
        Hud.Toast("No clear space for a target. Move away from walls.");
    }
    public void DevClearTargets()
    {
        if(!DevEnabled)return;
        foreach(var enemy in Enemies.ToArray())
        {
            if(!enemy.TrainingDummy)continue;
            Enemies.Remove(enemy);enemy.BreakTether();enemy.QueueFree();
        }
    }
    private void NextDungeon()=>SwitchDungeon(DungeonIndex+1);
    private void StartAttunementCapture()=>AddChild(new AttunementCapture {Run=this});
    private void StartAttunementTests(){TestMode=true;AddChild(new AttunementTests {Run=this});}
    private void StartTests() {AddChild(new SmokeTests { Run=this });}
    private void StartCapture() {AddChild(new CaptureScenes { Run=this });}
    private void StartPlaytest() {AddChild(new BotPlaytest { Run=this });}
    private void StartMusicTests() {AddChild(new MusicTests { Run=this });}
    private void StartAudioCapture() {AddChild(new AudioCapture { Run=this });}
}
