using System;
using System.Threading.Tasks;
using Godot;
namespace LastEmber;
public partial class AttunementCapture : Node
{
	public RunManager Run { get; set; }=null!;
	private async Task Frames(int count){for(int i=0;i<count;i++)await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);}
	public override async void _Ready()
	{
		try
		{
			for(int i=0;i<3;i++)
			{
				Run.StartRun(42,i);Run.Player.Automated=true;
				Run.Player.Position=new Vector2(650,556);await Frames(20);
				Run.Player.TryDungeonAbility();await Frames(i==2?15:25);
				await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
				var folder=ProjectSettings.GlobalizePath("res://TestResults/attunement");System.IO.Directory.CreateDirectory(folder);
				GetViewport().GetTexture().GetImage().SavePng(folder+"/"+i+".png");
			}
			Run.ShowMenu();await Frames(3);GetTree().Quit();
		}
		catch(Exception e){GD.PushError(e.ToString());GetTree().Quit(1);}
	}
}
