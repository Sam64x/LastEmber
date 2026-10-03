using System.Collections.Generic;
using Godot;
namespace LastEmber;

// Danger markings alone ignore darkness. Preview and damage use the same fixed sockets.
public partial class GroundAttackTelegraph : Node2D
{
    public readonly record struct Area(Vector2 Center, float Radius, float InnerRadius = 0)
    {
        public bool Contains(Vector2 point)
        {
            float distance = Center.DistanceTo(point);
            return distance < Radius && distance >= InnerRadius;
        }
    }
    public List<Area> Areas { get; } = new();
    public List<Vector2> ShotDirections { get; } = new();
    public Vector2 ShotOrigin { get; set; }
    public float ShotLength { get; set; } = 360;
    public Color Tint { get; set; } = new(1, .45f, .2f);
    public float Progress { get; set; }
    public override void _Ready()
    {
        TopLevel = true; GlobalPosition = Vector2.Zero; ZIndex = 6;
        Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };
        Hide();
    }
    public void Clear()
    {
        Areas.Clear(); ShotDirections.Clear(); Progress = 0; Hide(); QueueRedraw();
    }
    public override void _Draw()
    {
        foreach (var area in Areas)
        {
            if (area.InnerRadius <= 0) DrawCircle(area.Center, area.Radius, new Color(Tint, .08f + Progress * .13f));
            else for (int i = 0; i < 64; i++)
            {
                var a = Vector2.FromAngle(i * Mathf.Tau / 64);
                var b = Vector2.FromAngle((i + 1) * Mathf.Tau / 64);
                DrawColoredPolygon(new[] { area.Center + a * area.InnerRadius, area.Center + a * area.Radius,
                    area.Center + b * area.Radius, area.Center + b * area.InnerRadius }, new Color(Tint, .08f + Progress * .13f));
            }
            DrawArc(area.Center, area.Radius, 0, Mathf.Tau, 64, Tint, 3, true);
            if (area.InnerRadius > 0) DrawArc(area.Center, area.InnerRadius, 0, Mathf.Tau, 64, Tint, 3, true);
            float sweep = Mathf.Lerp(area.InnerRadius, area.Radius, Mathf.Clamp(Progress, 0, 1));
            if (sweep > 1) DrawArc(area.Center, sweep, 0, Mathf.Tau, 64, new Color(Tint, .7f), 2, true);
        }
        foreach (var direction in ShotDirections)
        {
            var end = ShotOrigin + direction * ShotLength;
            DrawLine(ShotOrigin + direction * 48, end, new Color(Tint, .35f + Progress * .5f), 2 + Progress * 2, true);
            DrawLine(end, end - direction.Rotated(.5f) * 18, Tint, 2, true);
            DrawLine(end, end - direction.Rotated(-.5f) * 18, Tint, 2, true);
        }
    }
}
