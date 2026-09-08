using Godot;

namespace LastEmber;

public static class FlamePalette
{
    public static Color Fire(bool blue)=>blue?new Color(.12f,.62f,1):new Color(1,.5f,.12f);
    public static Color Light(bool blue)=>blue?new Color(.3f,.72f,1):new Color(1,.63f,.30f);
    public static Color Shift(Color warm,bool blue)
        =>blue?new Color(warm.B*.6f+.1f,Mathf.Clamp(.3f+warm.G*.7f,0,1),1,warm.A):warm;
}
