using System;
using System.Linq;
using Godot;
namespace LastEmber;

public partial class CollectionScreen : Control
{
    public RunManager Run {get;set;}=null!;
    public Action Back {get;set;}=null!;
    private Control _body=null!;
    private LineEdit _search=null!;
    private Label _count=null!;
    private PanelContainer? _details;
    private TalentTreeView? _tree;
    private readonly Button[] _tabs=new Button[3];
    private int _tab;
    private bool _switching;
    private static readonly Color Surface=new(.055f,.061f,.079f);
    public override void _Ready()
    {
        MouseFilter=MouseFilterEnum.Stop;SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(new ColorRect {Size=new Vector2(1920,1080),Color=new Color(.024f,.027f,.038f),MouseFilter=MouseFilterEnum.Ignore});
        Label(this,"КОЛЛЕКЦИЯ",new Rect2(70,55,1300,75),52,Hud.Cream);
        Label(this,"Все записи открыты • изучайте врагов, биомы и сочетания Cores",new Rect2(74,138,1650,36),23,Hud.Muted);
        string[] names={"МОНСТРЫ","ДАНЖИ","ДЕРЕВО ТАЛАНТОВ"};
        for(int i=0;i<3;i++)
        {
            int selected=i;
            _tabs[i]=MakeButton(this,names[i],new Rect2(70+i*320,200,300,52),()=>SwitchTab(selected),"CollectionTab"+i);
        }
        _search=new LineEdit {Name="CollectionSearch",Position=new Vector2(1060,200),Size=new Vector2(800,52),PlaceholderText="Поиск по имени, описанию или тегам…"};
        _search.AddThemeFontSizeOverride("font_size",22);AddChild(_search);
        _search.TextChanged+=unused=>
        {
            if(_switching)return;
            if(_tree==null){Populate();return;}
            _tree.Search(_search.Text);
            _count.Text=$"НАЙДЕНО {_tree.MatchCount} / {Run.Rewards.Catalog.Count}   •   Все таланты доступны";
            if(_tree.MatchCount==0)Empty();
        };
        _count=Label(this,"",new Rect2(75,972,1220,45),20,Hud.Muted);
        MakeButton(this,"В ГЛАВНОЕ МЕНЮ  /  ESC",new Rect2(1390,968,470,62),Back,"CollectionBack");
        SwitchTab(0);
    }
    private void SwitchTab(int tab)
    {
        _tab=tab;_switching=true;_search.Text="";_switching=false;
        for(int i=0;i<3;i++)_tabs[i].AddThemeStyleboxOverride("normal",Box(i==tab?new Color(.19f,.12f,.075f):Surface,i==tab?Hud.Amber:new Color(.23f,.25f,.3f)));
        Populate();
    }
    private bool Matches(string value)=>value.Contains(_search.Text,StringComparison.OrdinalIgnoreCase);
    private void Populate()
    {
        if(GodotObject.IsInstanceValid(_body)){RemoveChild(_body);_body.QueueFree();}
        _tree=null;_details=null;
        _body=new Control {Position=new Vector2(70,275),Size=new Vector2(1790,675),MouseFilter=MouseFilterEnum.Ignore};AddChild(_body);
        if(_tab==2){ShowTree();return;}
        var scroll=new ScrollContainer {Name="CollectionCards",Size=new Vector2(1230,675),HorizontalScrollMode=ScrollContainer.ScrollMode.Disabled};_body.AddChild(scroll);
        var grid=new GridContainer {Columns=_tab==0?4:3,SizeFlagsHorizontal=SizeFlags.ExpandFill};
        var group=new ButtonGroup();
        grid.AddThemeConstantOverride("h_separation",14);grid.AddThemeConstantOverride("v_separation",14);scroll.AddChild(grid);
        if(_tab==0)
        {
            var entries=CollectionCatalog.Monsters.Where(entry=>Matches(entry.Name+" "+entry.Habitat+" "+entry.Role+" "+entry.Behavior)).ToArray();
            foreach(var entry in entries)
            {
                var card=Card(grid,entry.Name,entry.Role,new Vector2(286,260),()=>ShowMonster(entry),entry.Tint);
                card.ToggleMode=true;card.ButtonGroup=group;card.ButtonPressed=entry==entries[0];
                card.AddChild(new CollectionPortrait {Position=new Vector2(25,16),Size=new Vector2(236,130),Kind=entry.Kind,Tint=entry.Tint,Boss=entry.Boss,MouseFilter=MouseFilterEnum.Ignore});
            }
            _count.Text=$"МОНСТРЫ  {entries.Length} / {CollectionCatalog.Monsters.Length}   •   Все карточки доступны";
            if(entries.Length>0)ShowMonster(entries[0]);else Empty();
        }
        else
        {
            var entries=Run.Dungeons.Where(entry=>Matches(entry.DisplayName+" "+entry.BossName+" "+entry.Ability.DisplayName+" "+CollectionCatalog.DungeonDescription(entry.DungeonType))).ToArray();
            foreach(var entry in entries)
            {
                var card=Card(grid,entry.DisplayName.ToUpperInvariant(),entry.Ability.DisplayName,new Vector2(390,420),()=>ShowDungeon(entry),entry.Ability.Tint);
                card.ToggleMode=true;card.ButtonGroup=group;card.ButtonPressed=entry==entries[0];
                card.AddChild(new CollectionPortrait {Position=new Vector2(25,24),Size=new Vector2(340,260),Dungeon=true,Tint=entry.Ability.Tint,MouseFilter=MouseFilterEnum.Ignore});
            }
            _count.Text=$"ДАНЖИ  {entries.Length} / {Run.Dungeons.Count}   •   Все биомы доступны";
            if(entries.Length>0)ShowDungeon(entries[0]);else Empty();
        }
    }
    private static Button Card(Control parent,string title,string subtitle,Vector2 size,Action clicked,Color tint)
    {
        var button=new Button {Name="Card"+parent.GetChildCount(),CustomMinimumSize=size,MouseDefaultCursorShape=CursorShape.PointingHand};
        button.AddThemeStyleboxOverride("normal",Box(Surface,new Color(tint,.4f)));
        button.AddThemeStyleboxOverride("hover",Box(new Color(.095f,.09f,.11f),tint,2));
        button.AddThemeStyleboxOverride("pressed",Box(new Color(.11f,.09f,.10f),tint,2));
        button.AddThemeStyleboxOverride("focus",Box(new Color(0,0,0,0),Hud.Cream,2));
        button.Pressed+=clicked;parent.AddChild(button);
        Label(button,title,new Rect2(18,size.Y-105,size.X-36,60),23,Hud.Cream,true);
        Label(button,subtitle,new Rect2(18,size.Y-43,size.X-36,32),17,tint,true);
        return button;
    }
    private VBoxContainer Details(string category,string title,Color tint)
    {
        if(_details!=null){_body.RemoveChild(_details);_details.QueueFree();}
        _details=new PanelContainer {Position=new Vector2(1260,0),Size=new Vector2(530,675)};
        _details.AddThemeStyleboxOverride("panel",new StyleBoxFlat {BgColor=Surface,BorderColor=new Color(tint,.4f),BorderWidthTop=2,ContentMarginLeft=24,ContentMarginRight=24,ContentMarginTop=22,ContentMarginBottom=22});_body.AddChild(_details);
        var scroll=new ScrollContainer {HorizontalScrollMode=ScrollContainer.ScrollMode.Disabled};_details.AddChild(scroll);
        var rows=new VBoxContainer {SizeFlagsHorizontal=SizeFlags.ExpandFill};rows.AddThemeConstantOverride("separation",16);scroll.AddChild(rows);
        Paragraph(rows,category,18,tint);Paragraph(rows,title,32,Hud.Cream);Paragraph(rows,"ДОСТУПНО",16,new Color(.5f,.86f,.68f));
        return rows;
    }
    private void ShowMonster(MonsterCard entry)
    {
        var rows=Details(entry.Boss?"КАРТОЧКА БОССА":"КАРТОЧКА МОНСТРА",entry.Name,entry.Tint);
        rows.AddChild(new CollectionPortrait {CustomMinimumSize=new Vector2(450,170),Kind=entry.Kind,Tint=entry.Tint,Boss=entry.Boss,MouseFilter=MouseFilterEnum.Ignore});
        Paragraph(rows,$"{entry.Role}\n{entry.Health} HP • {entry.Habitat}",22,Hud.Cream);
        Paragraph(rows,"ПОВЕДЕНИЕ",18,entry.Tint);Paragraph(rows,entry.Behavior);
        Paragraph(rows,"ТАКТИКА",18,entry.Tint);Paragraph(rows,entry.Tactics);
    }
    private void ShowDungeon(DungeonDefinition entry)
    {
        var rows=Details("КАРТОЧКА ДАНЖА",entry.DisplayName.ToUpperInvariant(),entry.Ability.Tint);
        Paragraph(rows,CollectionCatalog.DungeonDescription(entry.DungeonType));
        var ability=entry.Ability;
        Paragraph(rows,"СПОСОБНОСТЬ  /  Q",18,ability.Tint);
        Paragraph(rows,$"{ability.DisplayName}\n{ability.Cost:0} Flame • {ability.Cooldown:0.#} с • {ability.Radius:0} px");
        Paragraph(rows,$"Last Ember: {ability.BlueName}\nБесплатно • {ability.BlueCooldown:0.#} с • {ability.Radius*ability.BlueRadiusScale:0} px");
        Paragraph(rows,"ОСНОВНЫЕ ПРОТИВНИКИ",18,ability.Tint);
        Paragraph(rows,string.Join(" / ",entry.Enemies.Distinct().Select(kind=>CollectionCatalog.Monsters.FirstOrDefault(monster=>monster.Kind==kind&&!monster.Boss)?.Name??kind.ToString())));
        Paragraph(rows,"БОСС",18,ability.Tint);Paragraph(rows,entry.BossName);
        Paragraph(rows,"МАРШРУТ",18,ability.Tint);Paragraph(rows,string.Join(" → ",DungeonDirector.MvpRoute));
    }
    private void ShowTree()
    {
        _tree=new TalentTreeView {Name="TalentTree",Size=new Vector2(1230,605),Catalog=Run.Rewards.Catalog};
        _tree.Selected+=ShowTalent;_body.AddChild(_tree);
        MakeButton(_body,"−",new Rect2(0,623,60,48),()=>_tree.Zoom(1/1.2f),"TreeZoomOut");
        MakeButton(_body,"+",new Rect2(75,623,60,48),()=>_tree.Zoom(1.2f),"TreeZoomIn");
        MakeButton(_body,"ВСЁ ДЕРЕВО",new Rect2(150,623,240,48),()=>_tree.Fit(),"TreeFit");
        Label(_body,"Колесо: масштаб • перетаскивание: карта • клик: описание",new Rect2(415,632,815,40),19,Hud.Muted);
        _count.Text=$"{Run.Rewards.Catalog.Count} УЗЛОВ   •   Core: круги • гибриды: ромбы • все таланты доступны";
        var first=Run.Rewards.Catalog.FirstOrDefault(item=>item.Id=="melee_core")??Run.Rewards.Catalog.FirstOrDefault();
        if(first!=null)_tree.SelectNode(first);else Empty();
    }
    private void ShowTalent(BuildUpgradeData data)
    {
        var tint=TalentTreeView.NodeColor(data);var rows=Details(data.Category,data.DisplayName,tint);
        Paragraph(rows,data.Description);
        Paragraph(rows,"ТЕГИ",18,tint);Paragraph(rows,string.Join(" / ",data.Tags));
        Paragraph(rows,$"Максимальный ранг: {data.MaxRank}");
        if(data.RequiredIds.Length>0)
        {
            Paragraph(rows,"СВЯЗИ В БИЛДЕ",18,tint);
            foreach(var id in data.RequiredIds)
            {
                var parent=Run.Rewards.Catalog.FirstOrDefault(item=>item.Id==id);
                if(parent==null){Paragraph(rows,id);continue;}
                var button=new Button {Text=parent.DisplayName,CustomMinimumSize=new Vector2(0,48),MouseDefaultCursorShape=CursorShape.PointingHand};
                button.AddThemeFontSizeOverride("font_size",21);button.Pressed+=()=>_tree?.SelectNode(parent,true);rows.AddChild(button);
            }
        }
        var descendants=Run.Rewards.Catalog.Where(item=>item.RequiredIds.Contains(data.Id)).ToArray();
        if(descendants.Length>0){Paragraph(rows,"ОТКРЫВАЕТ ВЕТКИ",18,tint);Paragraph(rows,string.Join(" / ",descendants.Select(item=>item.DisplayName)));}
    }
    private void Empty(){var rows=Details("ПОИСК","Ничего не найдено",Hud.Amber);Paragraph(rows,"Измените запрос или очистите поле поиска.");}
    private static void Paragraph(Control parent,string text,int fontSize=21,Color? tint=null)
    {
        var label=new Label {Text=text,AutowrapMode=TextServer.AutowrapMode.WordSmart,SizeFlagsHorizontal=SizeFlags.ExpandFill,MouseFilter=MouseFilterEnum.Ignore};
        label.AddThemeFontSizeOverride("font_size",fontSize);label.AddThemeColorOverride("font_color",tint??Hud.Cream);parent.AddChild(label);
    }
    private static Label Label(Control parent,string text,Rect2 rect,int fontSize,Color tint,bool wrap=false)
    {
        var label=new Label {Text=text,Position=rect.Position,Size=rect.Size,AutowrapMode=wrap?TextServer.AutowrapMode.WordSmart:TextServer.AutowrapMode.Off,MouseFilter=MouseFilterEnum.Ignore};
        label.AddThemeFontSizeOverride("font_size",fontSize);label.AddThemeColorOverride("font_color",tint);parent.AddChild(label);return label;
    }
    private static StyleBoxFlat Box(Color fill,Color border,int width=1)=>new() {BgColor=fill,BorderColor=border,BorderWidthTop=width,BorderWidthBottom=width,BorderWidthLeft=width,BorderWidthRight=width};
    private static Button MakeButton(Control parent,string text,Rect2 rect,Action action,string name)
    {
        var button=new Button {Name=name,Text=text,Position=rect.Position,Size=rect.Size,MouseDefaultCursorShape=CursorShape.PointingHand};
        button.AddThemeFontSizeOverride("font_size",21);button.AddThemeStyleboxOverride("normal",Box(Surface,new Color(.23f,.25f,.3f)));
        button.AddThemeStyleboxOverride("hover",Box(new Color(.16f,.11f,.09f),Hud.Amber,2));button.AddThemeStyleboxOverride("focus",Box(new Color(0,0,0,0),Hud.Cream,2));
        button.Pressed+=action;parent.AddChild(button);return button;
    }
}
