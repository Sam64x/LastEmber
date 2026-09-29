using Godot;
namespace LastEmber;
public enum EncounterKind { None, Skirmish, Swarm, Reinforcements, Ambush, Ability, RiskReward, Elite, Altar, Reward, Boss }
[GlobalClass]
public partial class EncounterDefinition : Resource
{
    [Export] public string Id { get; set; } = "skirmish";
    [Export] public EncounterKind Kind { get; set; }
    [Export] public int EnemyCount { get; set; }
    [Export] public int Waves { get; set; } = 1;
}
