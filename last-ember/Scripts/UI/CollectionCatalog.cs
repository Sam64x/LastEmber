using Godot;
namespace LastEmber;

// Encyclopedia text is separate from runtime actors: opening it never spawns enemies.
public sealed record MonsterCard(string Id,string Name,EnemyKind Kind,int Health,string Role,string Habitat,string Behavior,string Tactics,Color Tint,bool Boss=false);
public static class CollectionCatalog
{
    public static readonly MonsterCard[] Monsters =
    {
        new("ashling","Ashling",EnemyKind.Ashling,40,"Ближний бой","Все биомы / стаи","Преследует игрока и готовит короткий контактный удар.","Разделяйте стаю, используйте широкий Strike и урон по области.",new(.7f,.5f,.34f)),
        new("moth","Moth",EnemyKind.Moth,25,"Охотник на свет","Darkness","Просыпается от света. Любой Reveal слышен всем Moth в комнате и начинает преследование.","Перед Q приготовьте путь отхода. После импульса используйте Dash, чтобы не попасть в окружение.",new(.8f,.77f,.7f)),
        new("shade","Shade",EnemyKind.Shade,60,"Преследователь","Darkness","Движется быстрее в темноте и медленнее на свету. Его контактный удар на свету слабее.","Держите Shade в своём свете. Отступайте перед подготовленным ударом.",new(.65f,.48f,.86f)),
        new("watcher","Watcher",EnemyKind.Watcher,45,"Стрелок","Darkness","Неподвижен. На свету фиксирует цель, показывает линию и через секунду стреляет.","Меняйте позицию после появления линии. Стены перекрывают выстрел.",new(.9f,.37f,.4f)),
        new("leech","Leech",EnemyKind.Leech,35,"Поглощение Flame","Darkness","Вблизи привязывается к игроку и периодически отнимает Flame.","Dash разрывает связь. Также помогают дистанция свыше 260 px и препятствие между вами.",new(.8f,.35f,.57f)),
        new("torchbearer","Torchbearer",EnemyKind.Torchbearer,340,"Элита","Все биомы / Elite","Несёт собственный свет. Медленно сближается и наносит тяжёлый удар с увеличенной дальностью.","Используйте длительную подготовку удара для отхода, затем атакуйте слабую точку.",new(1,.61f,.23f)),
        new("stalker","Stalker",EnemyKind.Stalker,45,"Засада","Darkness / Frost","Держится у границы света. При Flame ≤20 периодически бросается к игроку; Reveal подавляет этот рывок.","Следите за звуками за границей света. Q раскрывает позицию, Dash создаёт дистанцию.",new(.45f,.7f,.75f)),
        new("ice_guard","IceGuard",EnemyKind.IceGuard,40,"Ледяная броня","Frost","Пока броня цела, получает лишь 30% обычного урона.","Thermal Shock, полный Heavy или Ember Lance разбивают броню. Атакуйте после её разрушения.",new(.57f,.85f,1)),
        new("fire_wisp","FireWisp",EnemyKind.FireWisp,40,"Огненный стрелок","Inferno","Держит дистанцию 180–300 px. За 0.75 с готовит выстрел по отмеченному направлению.","Backdraft и Cold Flame прерывают подготовку и ослабляют врага. Сближайтесь через Dash.",new(1,.4f,.16f)),
        new("extinguisher","The Extinguisher",EnemyKind.Boss,1800,"Босс / 3 фазы","Darkness","Чередует удар по отмеченной области, прямой рывок и удар вокруг себя. При 70% и 40% HP меняет фазу и тушит свет арены.","Не стойте в разметке. После атаки используйте окно слабой точки; в последней фазе берегите собственный свет.",new(.79f,.42f,.58f),true),
        new("rime_warden","The Rime Warden",EnemyKind.Boss,950,"Босс / 3 фазы","Frost","Icefall, Shard Fan и Frozen Ring. Фазы меняются при 65% и 30% HP. Icefall восстанавливает броню.","Q разбивает броню. У кольца безопасен центр радиусом 100 px и пространство за внешней границей.",new(.57f,.85f,1),true),
        new("cinder_heart","The Cinder Heart",EnemyKind.Boss,950,"Босс / 3 фазы","Inferno","Радиальный залп с двумя просветами, направленный веер и Eruption. Фазы меняются при 65% и 30% HP.","Ищите просветы между снарядами. Q прерывает подготовку огненной атаки и открывает окно для ответа.",new(1,.4f,.16f),true)
    };
    public static string DungeonDescription(DungeonType type)=>type switch
    {
        DungeonType.Frost=>"Ледяные проходы, замёрзшие тайники и бронированные враги. Thermal Shock меняет пространство: разрушает лёд и снимает броню.",
        DungeonType.Inferno=>"Огненные зоны и стреляющие ловушки. Backdraft втягивает огонь, поглощает огненные снаряды и выпускает взрыв. Тушение временное — выбирайте момент для прохода.",
        _=>"Полная темнота, скрытые угрозы и пепельные ловушки. Reveal раскрывает пространство, но привлекает Moth и будит Watcher. Flame определяет радиус вашего света."
    };
}

// Procedural portraits match the game's geometric visual language.
public partial class CollectionPortrait : Control
{
    public EnemyKind Kind {get;set;}
    public Color Tint {get;set;}=Hud.Amber;
    public bool Boss {get;set;}
    public bool Dungeon {get;set;}
    public override void _Draw()
    {
        var c=Size*.5f;float r=Mathf.Min(Size.X,Size.Y)*.28f;
        DrawCircle(c,r*1.5f,new Color(Tint,.07f));
        DrawArc(c,r*1.55f,0,Mathf.Tau,64,new Color(Tint,.28f),1,true);
        if(Dungeon)
        {
            DrawRect(new Rect2(c-new Vector2(r,r),new Vector2(r*2,r*2)),new Color(Tint,.15f));
            DrawPolyline(new[]{c+new Vector2(-r,r),c+new Vector2(-r,-r*.6f),c+new Vector2(0,-r*1.2f),c+new Vector2(r,-r*.6f),c+new Vector2(r,r)},Tint,4,true);
            DrawRect(new Rect2(c-new Vector2(r*.35f,-r*.1f),new Vector2(r*.7f,r)),Tint,false,3);
        }
        else if(Kind==EnemyKind.Moth)
        {
            DrawColoredPolygon(new[]{c,c+new Vector2(-r,-r),c+new Vector2(-r,r)},Tint);
            DrawColoredPolygon(new[]{c,c+new Vector2(r,r),c+new Vector2(r,-r)},Tint);
        }
        else if(Kind==EnemyKind.Watcher)
        {
            DrawColoredPolygon(new[]{c+Vector2.Left*r,c+Vector2.Up*r,c+Vector2.Right*r,c+Vector2.Down*r},Tint);
            DrawCircle(c,r*.3f,new Color(.04f,.04f,.06f));DrawCircle(c,r*.14f,Colors.White);
        }
        else if(Kind==EnemyKind.Leech)
            for(int i=0;i<4;i++)DrawCircle(c+new Vector2((i-1.5f)*r*.45f,Mathf.Sin(i)*r*.2f),r*(.42f-i*.05f),Tint.Darkened(i*.12f));
        else if(Kind==EnemyKind.FireWisp)
        {
            DrawCircle(c,r*.55f,Tint);
            for(int i=0;i<3;i++)DrawCircle(c+Vector2.FromAngle(i*Mathf.Tau/3)*r,r*.2f,Tint);
            DrawCircle(c,r*.18f,Colors.White);
        }
        else
        {
            DrawColoredPolygon(new[]{c+new Vector2(-r,r),c+new Vector2(-r*.75f,-r*.7f),c+new Vector2(-r*.3f,-r*1.15f),c+new Vector2(0,-r*.8f),c+new Vector2(r*.6f,-r),c+new Vector2(r,r)},Tint);
            DrawLine(c+new Vector2(-r*.4f,-r*.2f),c+new Vector2(-r*.1f,-r*.15f),Colors.White,3,true);
            DrawLine(c+new Vector2(r*.1f,-r*.15f),c+new Vector2(r*.4f,-r*.2f),Colors.White,3,true);
            if(Kind==EnemyKind.IceGuard)DrawArc(c,r*1.25f,0,Mathf.Tau,8,Tint,4,true);
            if(Kind==EnemyKind.Torchbearer){DrawLine(c+new Vector2(r, r),c+new Vector2(r,-r),Tint,4);DrawCircle(c+new Vector2(r,-r),r*.25f,Hud.Amber);}
        }
        if(Boss)for(int i=0;i<3;i++)DrawCircle(c+new Vector2((i-1)*r*.5f,-r*1.55f),4,Tint);
    }
}
