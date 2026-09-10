using System;
using System.Collections.Generic;

namespace LastEmber;

// Per-player progression. Shared Resources remain immutable throughout the run.
public sealed class BuildState
{
    private readonly Dictionary<string, int> _ranks = new(StringComparer.Ordinal);
    private readonly Dictionary<MeleeEffectKind, float> _effects = new();
    private readonly HashSet<string> _tags = new(StringComparer.Ordinal);
    private readonly List<BuildUpgradeData> _upgrades = new();
    public IReadOnlyList<BuildUpgradeData> Upgrades => _upgrades;
    public IEnumerable<string> Tags => _tags;
    public event Action? Changed;
    public int Rank(string id) => _ranks.GetValueOrDefault(id);
    public bool HasTag(string tag) => _tags.Contains(tag);
    public float Value(MeleeEffectKind effect) => _effects.GetValueOrDefault(effect);
    public bool Has(MeleeEffectKind effect) => Value(effect) > 0;
    public bool CanAcquire(BuildUpgradeData data)
    {
        if(string.IsNullOrWhiteSpace(data.Id) || Rank(data.Id)>=data.MaxRank)return false;
        foreach(var id in data.RequiredIds)if(Rank(id)==0)return false;
        foreach(var tag in data.RequiredTags)if(!HasTag(tag))return false;
        return true;
    }
    public bool Acquire(BuildUpgradeData data)
    {
        if(!CanAcquire(data))return false;
        if(Rank(data.Id)==0)_upgrades.Add(data);
        _ranks[data.Id]=Rank(data.Id)+1;
        foreach(var tag in data.Tags)_tags.Add(tag);
        foreach(var effect in data.Effects)_effects[effect.Kind]=Value(effect.Kind)+effect.Value;
        Changed?.Invoke();return true;
    }
    public void Clear()
    {
        _ranks.Clear();_effects.Clear();_tags.Clear();_upgrades.Clear();Changed?.Invoke();
    }
}
