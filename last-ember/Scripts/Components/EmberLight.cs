using Godot;

namespace LastEmber;

// Gameplay radius is explicit; it never depends on a rendering readback.
public partial class EmberLight : PointLight2D
{
    private static Texture2D? _texture;
    public float Radius { get; private set; } = 300;
    public float TargetRadius { get; set; } = 300;
    public bool Lit { get; set; } = true;
    public bool Contains(Vector2 point) => Lit && GlobalPosition.DistanceSquaredTo(point) < Radius * Radius;

    public override void _Ready()
    {
        if (_texture == null)
        {
            var gradient = new Gradient();
            gradient.SetColor(0, Colors.White);
            gradient.SetColor(1, new Color(1, 1, 1, 0));
            _texture = new GradientTexture2D
            {
                Width = 256, Height = 256, Gradient = gradient,
                Fill = GradientTexture2D.FillEnum.Radial,
                FillFrom = new Vector2(.5f, .5f), FillTo = new Vector2(1, .5f)
            };
        }
        Texture = _texture;
        Color = new Color(1, .63f, .30f);
        Energy = 1.35f;
        ShadowEnabled = false;
    }
    public override void _Process(double delta)
    {
        Radius = Mathf.Lerp(Radius, TargetRadius, 1 - Mathf.Exp(-(float)delta * 8));
        TextureScale = Radius / 128;
        Enabled = Lit;
    }
}
