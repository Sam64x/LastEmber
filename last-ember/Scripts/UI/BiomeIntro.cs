using Godot;
namespace LastEmber;

public partial class BiomeIntro : Control
{
    public RunManager Run {get;set;}=null!;
    public string Title {get;set;}="";
    public string Subtitle {get;set;}="";
    public Color Tint {get;set;}=Hud.Amber;
    private float _age;
    public override void _Ready(){MouseFilter=MouseFilterEnum.Ignore;Size=new Vector2(1920,1080);}
    public override void _Process(double delta)
    {
        if(Run.State==RunState.Menu){QueueFree();return;}
        if(!Run.Playing)return;
        _age+=(float)delta;if(_age>3){QueueFree();return;}QueueRedraw();
    }
    public override void _Draw()
    {
        float opacity=Mathf.SmoothStep(0,1,Mathf.Clamp(_age/.5f,0,1))*(1-Mathf.SmoothStep(0,1,Mathf.Clamp((_age-2.1f)/.9f,0,1)));
        float y=430+18*(1-opacity);
        var color=new Color(Tint,opacity);
        DrawLine(new Vector2(780,y-65),new Vector2(1140,y-65),new Color(Tint,.45f*opacity),1,true);
        DrawString(ThemeDB.FallbackFont,new Vector2(360,y-85),$"D E S C E N T   {Run.DungeonIndex+1:00}",HorizontalAlignment.Center,1200,20,color);
        DrawString(ThemeDB.FallbackFont,new Vector2(260,y),Title.ToUpperInvariant(),HorizontalAlignment.Center,1400,68,new Color(Hud.Cream,opacity));
        DrawString(ThemeDB.FallbackFont,new Vector2(310,y+48),Subtitle,HorizontalAlignment.Center,1300,22,new Color(Hud.Muted,opacity));
    }
}
