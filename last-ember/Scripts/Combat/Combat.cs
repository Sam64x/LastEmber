using Godot;

namespace LastEmber;

public readonly record struct DamageInfo(float Amount, Vector2 Origin, float Knockback = 110, bool Burn = false, bool IsBurnTick = false, bool Strike = false);

public interface IDamageable
{
    bool Dead { get; }
    void TakeDamage(DamageInfo hit);
}

// A geometric hitbox is evaluated once per strike, independently of render/physics frequency.
public static class Combat
{
    public static bool InArc(Vector2 origin, Vector2 aim, Vector2 target, float radius, float cosine = .12f)
    {
        var offset = target - origin;
        return offset.LengthSquared() <= radius * radius &&
               (offset.LengthSquared() < 400 || aim.Dot(offset.Normalized()) >= cosine);
    }
}

public sealed class BurnStatus
{
    public float Remaining { get; private set; }
    private float _tick;
    public bool Active => Remaining > 0;
    public void Apply(float duration = 3) { if(!Active)_tick=0;Remaining = Mathf.Max(Remaining, duration); }
    public bool Tick(float delta)
    {
        if (!Active) return false;
        Remaining -= delta;
        _tick += delta;
        if (_tick < .5f) return false;
        _tick -= .5f;
        return true;
    }
}
