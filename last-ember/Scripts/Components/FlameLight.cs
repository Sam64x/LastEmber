using Godot;

namespace LastEmber;

public static class FlameLight
{
    // Absolute Flame, not a percentage: Coal Heart genuinely buys more light.
    public static float Radius(float flame,float multiplier=1)
    {
        float radius=flame<=1?52:
            flame<=5?Mathf.Lerp(52,75,(flame-1)/4):
            flame<=20?Mathf.Lerp(75,150,(flame-5)/15):
            flame<=50?Mathf.Lerp(150,250,(flame-20)/30):
            flame<=100?Mathf.Lerp(250,380,(flame-50)/50):
            Mathf.Min(460,380+(flame-100)*1.5f);
        return Mathf.Max(48,radius*multiplier);
    }
}
