using System;
using Godot;

namespace LastEmber;

public sealed class FlamePool
{
    public const float LastEmberThreshold=5;
    public bool LastEmber => !Dead && Current<=LastEmberThreshold;
    public float LastEmberDamageMultiplier => LastEmber?1.4f:1;
    public float Current { get; private set; } = 100;
    public float Maximum { get; private set; } = 100;
    public float Ratio => Current / Maximum;
    public bool Dead => Current <= 0;
    public event Action? Changed;
    public event Action? Emptied;
    public event Action<bool>? LastEmberChanged;
    public event Action? DeathPrevented;
    private void Notify(bool wasLastEmber)
    {
        Changed?.Invoke();
        if(!Dead&&wasLastEmber!=LastEmber)LastEmberChanged?.Invoke(LastEmber);
    }

    public void Heal(float amount)
    {
        if (Dead) return;
        bool wasLastEmber=LastEmber;
        Current = Mathf.Clamp(Current + Mathf.Max(0, amount), 0, Maximum);
        Notify(wasLastEmber);
    }
    public void Damage(float amount)
    {
        if (Dead) return;
        bool wasLastEmber=LastEmber;
        bool saved=!wasLastEmber&&amount>=Current;
        Current = Mathf.Max(0, Current - Mathf.Max(0, amount));
        if(saved)Current=1;
        Notify(wasLastEmber);
        if(saved)DeathPrevented?.Invoke();
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
        bool wasLastEmber=LastEmber;
        Maximum += delta;
        Current = Mathf.Min(Maximum, Current + (fillGain ? Mathf.Max(0, delta) : 0));
        Notify(wasLastEmber);
        return true;
    }
}
