using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
namespace LastEmber;

// A browseable dependency graph. Every node is visible, without allocating run upgrades.
public partial class TalentTreeView : Control
{
    public IReadOnlyList<BuildUpgradeData> Catalog {get;set;}=Array.Empty<BuildUpgradeData>();
    public event Action<BuildUpgradeData>? Selected;
    private readonly Dictionary<string,Vector2> _positions=new(StringComparer.Ordinal);
    private readonly HashSet<string> _ancestry=new(StringComparer.Ordinal);
    private float _zoom=1;
    private Vector2 _pan,_pressPosition;
    private bool _dragging,_moved;
    private string _selected="",_search="",_hover="";
    public int MatchCount=>Catalog.Count(Matches);
    public override void _Ready()
    {
        ClipContents=true;MouseFilter=MouseFilterEnum.Stop;
        BuildLayout();Fit();
    }
    public static Color NodeColor(BuildUpgradeData data)=>data.Synergy?new(.8f,.58f,1):
        data.Tags.Contains("Orbit")?new(.55f,.8f,1):data.Tags.Contains("Dash")?new(.45f,.92f,.72f):
        data.Tags.Contains("Projectile")?new(1,.46f,.3f):Hud.Amber;
    private void Place(string id,Vector2 point)
    {
        for(int i=0;i<60&&_positions.Values.Any(other=>other.DistanceTo(point)<110);i++)point+=new Vector2(0,115);
        _positions[id]=point;
    }
    private void BuildLayout()
    {
        _positions.Clear();
        var cores=Catalog.OfType<CoreData>().OrderBy(item=>item.Id).ToArray();
        var anchors=new Dictionary<string,Vector2>
        {
            ["melee_core"]=new(0,-240),["bolt_core"]=new(430,0),
            ["dash_core"]=new(0,430),["orbit_core"]=new(-430,0)
        };
        for(int i=0;i<cores.Length;i++)Place(cores[i].Id,anchors.GetValueOrDefault(cores[i].Id,Vector2.FromAngle(i*Mathf.Tau/Math.Max(1,cores.Length))*430));
        var subs=Catalog.OfType<SubCoreData>().OrderBy(item=>item.Id).ToArray();
        for(int i=0;i<subs.Length;i++)
        {
            var parent=subs[i].RequiredIds.FirstOrDefault()??"";
            var origin=_positions.GetValueOrDefault(parent);
            float angle=-Mathf.Pi*.5f+(i-(subs.Length-1)*.5f)*.6f;
            Place(subs[i].Id,origin+Vector2.FromAngle(angle)*410);
        }
        foreach(var group in Catalog.Where(item=>item is TalentData&&!item.Synergy).GroupBy(item=>item.RequiredIds.FirstOrDefault()??""))
        {
            var parent=_positions.GetValueOrDefault(group.Key);
            var children=group.OrderBy(item=>item.Id).ToArray();
            float direction=parent.LengthSquared()>1?parent.Angle():0;
            for(int i=0;i<children.Length;i++)Place(children[i].Id,parent+Vector2.FromAngle(direction+(i-(children.Length-1)*.5f)*.45f)*260);
        }
        foreach(var data in Catalog.Where(item=>item.Synergy).OrderBy(item=>item.Id))
        {
            var parents=data.RequiredIds.Where(_positions.ContainsKey).Select(id=>_positions[id]).ToArray();
            var point=parents.Length>0?parents.Aggregate(Vector2.Zero,(sum,p)=>sum+p)/parents.Length:Vector2.Zero;
            Place(data.Id,point+new Vector2(0,70));
        }
        foreach(var data in Catalog)if(!_positions.ContainsKey(data.Id))Place(data.Id,new Vector2(750,750));
    }
    public void Fit()
    {
        if(_positions.Count==0)return;
        var min=new Vector2(_positions.Values.Min(p=>p.X)-110,_positions.Values.Min(p=>p.Y)-100);
        var max=new Vector2(_positions.Values.Max(p=>p.X)+110,_positions.Values.Max(p=>p.Y)+100);
        var extent=max-min;
        _zoom=Mathf.Clamp(Mathf.Min(Size.X/extent.X,Size.Y/extent.Y),.25f,1.4f);
        _pan=Size*.5f-(min+max)*.5f*_zoom;QueueRedraw();
    }
    public void Zoom(float factor)=>ZoomAt(Size*.5f,factor);
    private void ZoomAt(Vector2 cursor,float factor)
    {
        var world=(cursor-_pan)/_zoom;
        _zoom=Mathf.Clamp(_zoom*factor,.25f,2.4f);_pan=cursor-world*_zoom;QueueRedraw();
    }
    public void Search(string text)
    {
        _search=text.Trim();
        var match=Catalog.FirstOrDefault(Matches);
        if(_search.Length>0&&match!=null)SelectNode(match,true);
        else if(_search.Length==0)
        {
            var current=Catalog.FirstOrDefault(data=>data.Id==_selected)??match;
            if(current!=null)SelectNode(current);
        }
        QueueRedraw();
    }
    private bool Matches(BuildUpgradeData data)=>_search.Length==0||
        (data.DisplayName+" "+data.Description+" "+string.Join(" ",data.Tags)).Contains(_search,StringComparison.OrdinalIgnoreCase);
    public void SelectNode(BuildUpgradeData data,bool focus=false)
    {
        _selected=data.Id;_ancestry.Clear();
        void Visit(string id)
        {
            if(!_ancestry.Add(id))return;
            var item=Catalog.FirstOrDefault(entry=>entry.Id==id);
            if(item!=null)foreach(var parent in item.RequiredIds)Visit(parent);
        }
        Visit(data.Id);
        if(focus&&_positions.TryGetValue(data.Id,out var p)){_zoom=Mathf.Max(_zoom,.9f);_pan=Size*.5f-p*_zoom;}
        Selected?.Invoke(data);QueueRedraw();
    }
    private BuildUpgradeData? Hit(Vector2 cursor)=>Catalog.Reverse().FirstOrDefault(data=>
        (_pan+_positions[data.Id]*_zoom).DistanceTo(cursor)<Mathf.Max(13,Radius(data)*_zoom+5));
    private static float Radius(BuildUpgradeData data)=>data is CoreData?38:data is SubCoreData?28:data.Synergy?25:19;
    public override void _GuiInput(InputEvent input)
    {
        if(input is InputEventMouseButton button)
        {
            if(button.Pressed&&button.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown)
            {ZoomAt(button.Position,button.ButtonIndex==MouseButton.WheelUp?1.15f:1/1.15f);AcceptEvent();return;}
            if(button.ButtonIndex is MouseButton.Left or MouseButton.Middle or MouseButton.Right)
            {
                if(button.Pressed){_dragging=true;_moved=false;_pressPosition=button.Position;}
                else
                {
                    if(_dragging&&!_moved&&button.ButtonIndex==MouseButton.Left&&Hit(button.Position) is {} data)SelectNode(data);
                    _dragging=false;
                }
                AcceptEvent();
            }
        }
        else if(input is InputEventMouseMotion motion)
        {
            if(_dragging&&!Input.IsMouseButtonPressed(MouseButton.Left)&&!Input.IsMouseButtonPressed(MouseButton.Middle)&&!Input.IsMouseButtonPressed(MouseButton.Right))_dragging=false;
            if(_dragging)
            {
                if(motion.Position.DistanceTo(_pressPosition)>6)_moved=true;
                if(_moved)_pan+=motion.Relative;
            }
            var hovered=Hit(motion.Position);_hover=hovered?.Id??"";
            TooltipText=hovered==null?"":hovered.DisplayName+"\n"+hovered.Description;
            MouseDefaultCursorShape=_dragging?CursorShape.Drag:hovered==null?CursorShape.Arrow:CursorShape.PointingHand;
            QueueRedraw();AcceptEvent();
        }
    }
    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero,Size),new Color(.022f,.028f,.041f));
        for(int x=0;x<Size.X;x+=48)for(int y=0;y<Size.Y;y+=48)DrawCircle(new Vector2(x,y),1,new Color(.24f,.28f,.34f,.22f));
        var center=_pan;
        for(int i=1;i<=3;i++)DrawArc(center,155*i*_zoom,0,Mathf.Tau,96,new Color(.34f,.29f,.23f,.2f),1,true);
        foreach(var data in Catalog)
        {
            var to=_pan+_positions[data.Id]*_zoom;
            foreach(var parent in data.RequiredIds)
            {
                if(!_positions.TryGetValue(parent,out var origin))continue;
                bool lit=_ancestry.Contains(data.Id)&&_ancestry.Contains(parent);
                DrawLine(_pan+origin*_zoom,to,lit?Hud.Amber:new Color(.32f,.3f,.38f,.55f),lit?3:1.5f,true);
            }
        }
        foreach(var data in Catalog)
        {
            var p=_pan+_positions[data.Id]*_zoom;float r=Radius(data)*_zoom;
            if(p.X < -100||p.Y < -100||p.X>Size.X+100||p.Y>Size.Y+100)continue;
            var tint=NodeColor(data);if(!Matches(data))tint=new Color(tint,.2f);
            bool active=data.Id==_selected||data.Id==_hover;
            if(active)DrawCircle(p,r+9,new Color(tint,.18f));
            DrawCircle(p,r,new Color(.045f,.05f,.07f));
            if(data.Synergy)
            {
                var points=new[]{p+Vector2.Up*r,p+Vector2.Right*r,p+Vector2.Down*r,p+Vector2.Left*r,p+Vector2.Up*r};
                DrawPolyline(points,tint,active?3:2,true);
            }
            else DrawArc(p,r,0,Mathf.Tau,32,tint,active?3:2,true);
            if(data is CoreData)DrawArc(p,r+5,0,Mathf.Tau,32,new Color(tint,.5f),2,true);
            var glyph=data.DisplayName[..Math.Min(2,data.DisplayName.Length)].ToUpperInvariant();
            int fontSize=Mathf.Clamp(Mathf.RoundToInt(r*.65f),11,22);
            DrawString(ThemeDB.FallbackFont,p+new Vector2(-r,r*.25f),glyph,HorizontalAlignment.Center,r*2,fontSize,tint);
            if(_zoom>=.65f||data is CoreData||active)
                DrawString(ThemeDB.FallbackFont,p+new Vector2(-85,r+18),data.DisplayName,HorizontalAlignment.Center,170,Mathf.Clamp(Mathf.RoundToInt(16*_zoom),12,20),tint);
        }
    }
}
