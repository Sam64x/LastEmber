using System;
using Godot;

namespace LastEmber;

public sealed class FlamePool
{
    public float Current { get; private set; } = 100;
    public float Maximum { get; private set; } = 100;
    public float Ratio => Current / Maximum;
    public bool Dead => Current <= 0;
    public event Action? Changed;
    public event Action? Emptied;

    public void Heal(float amount)
    {
        if (Dead) return;
        Current = Mathf.Clamp(Current + Mathf.Max(0, amount), 0, Maximum);
        Changed?.Invoke();
    }
    public void Damage(float amount)
    {
        if (Dead) return;
        Current = Mathf.Max(0, Current - Mathf.Max(0, amount));
        Changed?.Invoke();
        if (Dead) Emptied?.Invoke();
    }
    public bool Spend(float amount)
    {
        // Never allow an ability to kill its caster.
        if (amount < 0 || Current <= amount) return false;
        Damage(amount);
        return true;
    }
    public bool ChangeMaximum(float delta, bool fillGain = false)
    {
        if (Maximum + delta < 25) return false;
        Maximum += delta;
        Current = Mathf.Min(Maximum, Current + (fillGain ? Mathf.Max(0, delta) : 0));
        Changed?.Invoke();
        return true;
    }
}
