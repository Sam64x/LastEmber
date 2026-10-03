using Godot;
namespace LastEmber;

// Room-local runtime: waves, triggers and completion. Freed with its room.
public partial class EncounterController : Node
{
    public RunManager Run { get; set; } = null!;
    public DungeonRoom Plan { get; set; } = null!;
    private readonly RandomNumberGenerator _rng=new();
    private DungeonProp? _lesson;
    private int _waves;
    private float _delay=1.5f;
    private bool _triggered, _abilityUsed, _lessonCompleted;
    public bool RiskAvailable => Plan.Type==RoomType.RiskReward&&!_triggered;
    public string Objective => Plan.Encounter.Kind switch
    {
        EncounterKind.Ability => Run.Dungeon.Definition.Lesson,
        EncounterKind.Ambush when !_triggered => "APPROACH THE CENTER • EXPECT AN AMBUSH",
        EncounterKind.RiskReward when !_triggered => "E AT CACHE • FIGHT FOR LOOT, OR TAKE THE OPEN EXIT",
        EncounterKind.RiskReward => "DEFEAT THE GUARDS • CLAIM THE CACHE",
        _ => "CLEAR THE ROOM"
    };
    public override void _Ready()
    {
        _rng.Seed=Plan.Seed;
        _waves=Plan.Encounter.Waves;
        switch(Plan.Encounter.Kind)
        {
            case EncounterKind.None: case EncounterKind.Reward: case EncounterKind.Altar: return;
            case EncounterKind.Ability:
                _lesson=Run.Graph!.Biome.CreateLesson(Run.Room,Plan);return;
            case EncounterKind.Ambush: case EncounterKind.RiskReward: return;
            case EncounterKind.Boss:
                Run.Spawn(EnemyKind.Boss,Run.Room.FindFreePosition(new Vector2(.76f,.5f),110));_waves=0;return;
            case EncounterKind.Elite:
                Run.Spawn(EnemyKind.Torchbearer,Run.Room.FindFreePosition(new Vector2(.75f,.5f),70));break;
        }
        SpawnWave();
    }
    public void AbilityUsed()=>_abilityUsed=true;
    public bool TryChallenge()
    {
        if(Plan.Type!=RoomType.RiskReward||_triggered||Run.Player.Position.DistanceTo(Run.Room.ShrinePosition)>=100)return false;
        _triggered=true;Run.BeginRiskChallenge();Run.Graph!.Biome.ApplyRiskReward(Run.Room,Plan);
        SpawnWave();Run.Hud.Toast(Objective);return true;
    }
    public bool Tick(float dt)
    {
        if(Plan.Encounter.Kind==EncounterKind.Ability)
        {
            // Latch the successful interaction before temporary fire suppression expires.
            _lessonCompleted|=_lesson!=null?_lesson.Open:_abilityUsed;
            return _lessonCompleted&&Run.Enemies.Count==0;
        }
        if(Plan.Encounter.Kind==EncounterKind.Ambush&&!_triggered)
        {
            if(Run.Player.Position.X<Run.Room.Bounds.Position.X+Run.Room.Bounds.Size.X*.43f)return false;
            _triggered=true;SpawnWave();Run.Hud.Toast("AMBUSH • SHADOWS CLOSE IN");
        }
        if(Plan.Encounter.Kind==EncounterKind.RiskReward&&!_triggered)return false;
        if(Run.Enemies.Count>0)return false;
        _delay-=dt;
        if(_delay>0)return false;
        if(_waves>0){SpawnWave();_delay=1.5f;return false;}
        return true;
    }
    private void SpawnWave()
    {
        _waves--;
        int count=Plan.Encounter.EnemyCount+(Plan.Encounter.Kind==EncounterKind.Swarm?3:0);
        for(int i=0;i<count;i++)
        {
            Vector2? spawn=null;
            // Grid fallback guarantees deterministic placement without silently losing a wave.
            for(int attempt=0;attempt<80;attempt++)
            {
                var p=Run.Room.Bounds.Position+new Vector2(_rng.RandfRange(.12f,.88f),_rng.RandfRange(.15f,.85f))*Run.Room.Bounds.Size;
                if(Safe(p)){spawn=p;break;}
            }
            if(spawn==null)
                foreach(var p in Run.Room.FreePositions(48))if(Safe(p)){spawn=p;break;}
            if(spawn==null)throw new System.InvalidOperationException($"No safe enemy socket: {Plan.Geometry.Id}/{Plan.Encounter.Id}");
            var types=Run.Dungeon.Definition.Enemies;
            var kind=Plan.Encounter.Kind==EncounterKind.Swarm?EnemyKind.Ashling:
                types.Count>0?types[i%types.Count]:EnemyKind.Shade;
            // Torchbearer is reserved for the authored elite beat, not ordinary roster rolls.
            if(kind==EnemyKind.Torchbearer)kind=EnemyKind.FireWisp;
            // Teach one pilgrim role first; later large fights combine the trio.
            if(Run.Dungeon.Definition.DungeonType==DungeonType.Darkness&&Plan.Encounter.Kind!=EncounterKind.Swarm)
            {
                if(i==0)kind=Plan.Type==RoomType.RiskReward?EnemyKind.AshHunter:Plan.Type==RoomType.Elite?EnemyKind.AshPriest:EnemyKind.AshKnight;
                if(count>=5&&i==2)kind=EnemyKind.AshHunter;
                if(count>=5&&i==4)kind=EnemyKind.AshPriest;
            }
            Run.Spawn(kind,spawn.Value);
        }
    }
    private bool Safe(Vector2 p)=>Run.Room.IsFree(p,48)&&p.DistanceTo(Run.Player.Position)>240&&
        !Run.Enemies.Exists(e=>!e.Dead&&e.Position.DistanceTo(p)<85);
    public override void _ExitTree()=>_rng.Dispose();
}
