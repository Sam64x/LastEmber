using Godot;
namespace LastEmber;

// A child overlay also works on bosses that override Enemy._Draw().
public partial class EnemyStatusFx : Node2D
{
    public Enemy Enemy { get; set; }=null!;
    public override void _Ready(){ZIndex=3;}
    public override void _Process(double delta){if(Enemy.Run.Playing)QueueRedraw();}
    public override void _Draw()
    {
        if(Enemy.Dead)return;
        float r=Enemy.BodyRadius;
        if(Enemy.Heat>0)
        {
            DrawRect(new Rect2(-r,r+8,r*2,3),new Color(.2f,.12f,.08f));
            DrawRect(new Rect2(-r,r+8,r*2*Enemy.Heat/100,3),FlamePalette.Fire(Enemy.Run.Player.Flame.LastEmber));
        }
        if(Enemy.WeakPointRemaining>0 && Enemy.Run.Player.Progression.Has(MeleeEffectKind.CritChance))
        {
            DrawArc(Vector2.Zero,r+14,0,Mathf.Tau,32,new Color(1,.95f,.3f),2);
            DrawString(ThemeDB.FallbackFont,new Vector2(-18,-r-30),"WEAK",HorizontalAlignment.Left,-1,12,new Color(1,.95f,.3f));
        }
        if(Enemy.TrainingDummy)DrawString(ThemeDB.FallbackFont,new Vector2(-22,r+28),"DUMMY",HorizontalAlignment.Left,-1,12,Colors.White);
    }
}
