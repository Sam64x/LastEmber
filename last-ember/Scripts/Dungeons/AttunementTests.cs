using System;
using System.Threading.Tasks;
using Godot;
namespace LastEmber;
public partial class AttunementTests : Node
{
    public RunManager Run { get; set; } = null!;
    private int _checks;
    private void Check(bool ok,string name){if(!ok)throw new Exception(name);_checks++;GD.Print("PASS: "+name);}
    private async Task Frames(int n){for(int i=0;i<n;i++)await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);}
    private DungeonProp Prop(string name)=>(DungeonProp)Run.Room.FindChild(name,true,false);
    public override async void _Ready()
    {
        try
        {
            Run.StartRun(42);await Frames(3);var p=Run.Player;
            Check(Run.Dungeons.Count>=3,"three definitions load");
            Check(p.Attunement is RevealAbility,"Darkness strategy");
            Check(p.TryDungeonAbility()&&p.Flame.Current==95,"Reveal costs 5 Flame");
            Check(!p.TryDungeonAbility(),"cooldown rejects repeat");
            await Frames(40);Check(p.RevealLight.Radius>1500,"Reveal expands across room");
            var build=p.Build;Run.SwitchDungeon(1);await Frames(3);
            Check(Run.Player==p&&p.Build==build&&p.Flame.Current==95,"switch preserves player, build and Flame");
            Check(!p.RevealLight.Lit&&!Run.Lights.Contains(p.RevealLight),"Reveal cleanup on exit");
            Check(p.Attunement is ThermalShockAbility,"Frost strategy");
            var wall=Prop("Wall");p.Position=new Vector2(740,556);
            var captive=Prop("Captive");
            int count=Run.Enemies.Count;
            Check(!Run.Room.IsFree(wall.GlobalPosition,1),"ice blocks navigation");
            Check(p.TryDungeonAbility(),"Thermal Shock activates");await Frames(3);
            foreach(var e in Run.Enemies)e.FrozenForTest=true;
            Check(wall.Open&&captive.Open&&Run.Enemies.Count==count+1,"shatter barrier also releases captive");
            Check(Run.Room.IsFree(wall.GlobalPosition,1),"shatter updates navigation");
            var ice=Run.Spawn(EnemyKind.IceGuard,p.Position+new Vector2(90,0));ice.FrozenForTest=true;
            Check(ice.IceArmored,"ice guard armor");
            p.Flame.Damage(p.Flame.Current-5);Check(p.Flame.LastEmber,"Last Ember threshold");
            Check(p.TryDungeonAbility()&&p.Flame.Current==5,"Blue Shatter free despite normal cooldown");
            Check(!ice.IceArmored,"Blue Shatter removes armor");
            Run.SwitchDungeon(2);await Frames(3);p.Flame.Heal(100);
            Check(p.Attunement is BackdraftAbility,"Inferno strategy");
            p.Position=new Vector2(600,556);var fire=Prop("Corridor");
            var projectile=new Projectile {Run=Run,Fire=true,Position=p.Position+new Vector2(100,0)};Run.Room.AddChild(projectile);
            var enemy=Run.Spawn(EnemyKind.FireWisp,p.Position+new Vector2(110,0));enemy.FrozenForTest=true;
            Check(p.TryDungeonAbility()&&fire.Open,"Backdraft opens safe corridor");
            Check(projectile.IsQueuedForDeletion(),"fire projectile absorbed");
            float hp=enemy.Health;await Frames(60);
            Check(enemy.Health<hp&&enemy.Weakened>0,"delayed blast overloads fire enemy");
            await Frames(180);Check(!fire.Open,"fire returns after safe window");
            p.Flame.Damage(p.Flame.Current-5);p.Position=new Vector2(690,556);
            Check(p.TryDungeonAbility()&&fire.Open&&p.Flame.Current==5,"Cold Flame free extinguish");
            Run.SwitchDungeon(0);await Frames(3);
            Check(p.Attunement is RevealAbility&&p.Flame.Current==5,"round trip preserves Last Ember");
            Check(p.TryDungeonAbility()&&p.BluePulseActive,"free Blue Pulse");
            Run.TogglePause();float remaining=p.RevealCooldown;await Frames(30);
            Check(p.RevealCooldown==remaining,"pause freezes cooldown");Run.TogglePause();
            p.Flame.Heal(100);await Frames(3);Check(!p.Revealing,"Last Ember transition cancels effect");
            Run.SwitchDungeon(2);p.TryDungeonAbility();Run.SwitchDungeon(1);await Frames(100);
            Check(p.Attunement is ThermalShockAbility,"pending Backdraft cancelled on switch");
            p.Flame.Heal(100);p.Position=new Vector2(640,460);
            Check(p.TryDungeonAbility()&&Prop("Treasure").Open,"frozen treasure releases pickup");
            await Frames(85);p.Flame.Damage(p.Flame.Current-5);
            p.Position=new Vector2(1090,660);
            Check(p.TryDungeonAbility()&&Prop("Slippery").Open,"Blue Shatter temporarily melts surface");
            Check(!Prop("Door").Open,"Blue radius does not reach distant door");
            await Frames(130);Check(Run.Dungeon.IsSlippery(new Vector2(1090,740)),"surface refreezes");
            p.Flame.Heal(100);Run.SwitchDungeon(1);await Frames(3);
            p.Position=new Vector2(1380,556);Check(p.TryDungeonAbility()&&Prop("Door").Open,"frozen passage opens");
            var boss=Run.Spawn(EnemyKind.Boss,new Vector2(1200,650));boss.FrozenForTest=true;
            Check(boss is ElementalBoss {Frost:true,IceArmored:true},"Frost boss comes from definition");
            boss.React(DungeonImpact.Heat,3,p.Position,false);Check(!boss.IceArmored,"boss armor shatters");
            boss.TakeDamage(new DamageInfo(10000,p.Position));await Frames(4);
            Check(Run.DungeonIndex==2 && Run.Player==p,"boss defeat advances attunement without new player");
            boss=Run.Spawn(EnemyKind.Boss,new Vector2(1200,650));boss.FrozenForTest=true;
            Check(boss is ElementalBoss {Frost:false,FireAligned:true},"Inferno boss comes from definition");
            boss.React(DungeonImpact.Suction,3,p.Position,false);Check(boss.Weakened>0,"boss attack window opens");
            p.Flame.Damage(p.Flame.Current-5);p.Position=new Vector2(700,556);
            var ordinary=new Projectile {Run=Run,Position=p.Position+new Vector2(100,0)};Run.Room.AddChild(ordinary);
            Check(p.TryDungeonAbility()&&!ordinary.IsQueuedForDeletion(),"Cold Flame cannot absorb ordinary projectiles");
            await Frames(65);float blueHp=boss.Health;await Frames(10);Check(boss.Health==blueHp,"Cold Flame has no damage blast");
            Run.SwitchDungeon(0);p.Flame.Heal(100);p.Flame.Damage(90);
            Check(p.TryDungeonAbility()&&p.Flame.Current==5&&!p.BluePulseActive,"paid cast snapshots mode before threshold crossing");
            await Frames(220);p.Flame.Damage(100);Check(!p.TryDungeonAbility(),"dead player cannot activate Q");
            Run.StartRun(42);p=Run.Player;
            for(int biome=0;biome<3;biome++)
            {
                if(biome>0)Run.SwitchDungeon(biome);
                while(Run.StageIndex<2){Run.Room.Cleared=true;Run.AdvanceStage();}
                await Frames(3);
                Check(GetTree().GetNodesInGroup("dungeon_reactive").Count==0,$"reward room has no combat props: {biome}");
                p.Position=Run.Room.ShrinePosition;
                Run._UnhandledInput(new InputEventAction {Action="interact",Pressed=true});
                Check(Run.State==RunState.Reward,$"E opens shrine: {biome}");
                Check(Run.ChooseRestore()&&Run.Playing,$"shrine restores and unpauses: {biome}");
                while(Run.StageIndex<5){Run.Room.Cleared=true;Run.AdvanceStage();}
                await Frames(3);
                Check(Run.Room.IsFree(Run.Room.Bounds.GetCenter())&&GetTree().GetNodesInGroup("dungeon_reactive").Count==0,$"altar approach is clear and safe: {biome}");
                p.Position=Run.Room.Bounds.GetCenter();
                Run._UnhandledInput(new InputEventAction {Action="interact",Pressed=true});
                Check(Run.State==RunState.Altar&&GetTree().Paused,$"E opens altar: {biome}");
                var choice=(Button)Run.Hud.Modal!.FindChild("Altar0",true,false);
                Check(!choice.Disabled,$"new altar offers another sacrifice: {biome}");
                choice.EmitSignal(BaseButton.SignalName.Pressed);
                Check(p.Build.AltarUsed&&p.Flame.Maximum==100-15*(biome+1)&&Run.Playing&&!GetTree().Paused&&Run.Room.Cleared,"sacrifice stacks cost and opens exit");
                Check(Mathf.IsEqualApprox(p.Build.AttackSpeed,Mathf.Pow(1.25f,biome+1)),"attack speed stacks between altars");
                Check(!Run.ChooseAltar(0),"same altar cannot charge twice");
                Run.OpenAltar();Check(Run.Playing,"used altar cannot reopen");
            }
            var stacked=new BuildStats();var pool=new FlamePool();
            Check(stacked.Sacrifice(2,pool)&&stacked.Sacrifice(2,pool)&&stacked.DashExplosionDamage==52,"repeated Rupture increases explosion damage");
            Check(!stacked.Sacrifice(2,pool)&&pool.Maximum==40&&stacked.DashExplosionDamage==52,"insufficient maximum Flame changes nothing");
            Check(stacked.Sacrifice(0,pool)&&pool.Maximum==25&&!stacked.Sacrifice(0,pool),"maximum Flame floor remains 25");
            GD.Print($"ATTUNEMENT PASS: {_checks} assertions");
            Run.ShowMenu();Run.Audio.StopAll();Run.Music.StopAll();await Frames(5);GetTree().Quit(0);
        }
        catch(Exception e){GD.PushError(e.ToString());GetTree().Quit(1);}
    }
}
