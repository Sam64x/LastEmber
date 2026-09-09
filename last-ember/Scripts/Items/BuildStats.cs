using System.Collections.Generic;
using Godot;

namespace LastEmber;

public sealed class BuildStats
{
    public List<ArtifactData> Artifacts { get; } = new();
    private readonly Dictionary<ArtifactEffect, float> _effects = new();
    public float AttackSpeed { get; private set; } = 1;
    public float BurstCost { get; private set; } = 10;
    public bool DashExplosion => DashExplosionDamage > 0;
    public float DashExplosionDamage { get; private set; }
    public bool AltarUsed { get; private set; }
    public float Get(ArtifactEffect effect) => _effects.GetValueOrDefault(effect);
    public bool Has(ArtifactEffect effect) => _effects.ContainsKey(effect);
    public bool Owns(string id) => Artifacts.Exists(a => a.Id == id);
    public float DamageMultiplier(FlamePool flame) => (1 + Get(ArtifactEffect.Glass)) *
        (flame.Ratio < .3f ? 1 + Get(ArtifactEffect.LowDamage) : 1)*flame.LastEmberDamageMultiplier;
    public float SpeedMultiplier(FlamePool flame) => flame.Current < 15 ? 1 + Get(ArtifactEffect.LowSpeed) : 1;
    public float LightMultiplier => 1 - Get(ArtifactEffect.DarkRewards);

    public bool Apply(ArtifactData data, FlamePool flame)
    {
        if (!data.Stackable && Owns(data.Id)) return false;
        if (data.Effect == ArtifactEffect.MaxFlame && !flame.ChangeMaximum(data.Value)) return false;
        if (data.Effect == ArtifactEffect.Glass && !flame.ChangeMaximum(-25)) return false;
        _effects[data.Effect] = Get(data.Effect) + data.Value;
        Artifacts.Add(data);
        return true;
    }
    public bool Sacrifice(int option, FlamePool flame)
    {
        if (option < 0 || option > 2) return false;
        if (!flame.ChangeMaximum(-new[] { 15, 20, 30 }[option])) return false;
        if (option == 0) AttackSpeed *= 1.25f;
        if (option == 1) _effects[ArtifactEffect.LowSpeed] = Get(ArtifactEffect.LowSpeed) + .25f;
        if (option == 2) DashExplosionDamage += 26;
        AltarUsed = true;
        return true;
    }
}
