# Responsive hero presentation

`PlayerVisual` samples actual post-collision velocity (`GetRealVelocity`) and aim
independently. Movement into a wall therefore settles into idle; strafing keeps
the mask looking toward the cursor while fire responds to travel direction.

`HeroMotion` maintains separate damped springs for body lean and flame drag,
including a bounded acceleration response. Both axes participate continuously,
including diagonals and arbitrary angles. The flame crown lags against velocity;
its root stays attached. Turning and stopping carry a short residual sway.
Springs use bounded 120Hz substeps; presentation clocks follow the existing
pause/hit-stop rules and reset motion history when entering a room.

The idle loop combines slow hovering, complementary squash/stretch, subtle eye
brightness and uninterrupted turbulent combustion. Movement blends in a
distance-driven hover gait, lateral sway and faster combustion. Its amplitude
eases to zero after stopping. There are no discrete animation-frame limits:
deformation and fire run at rendering cadence (actual FPS depends on hardware).

The existing front, three-quarter and back mask artwork is registered to a shared
pivot, mirrored for left views and blended over short angle intervals. Continuous
facing, parallax and lean connect these authored views; this is a 2D rig, not a
new 3D mesh or an atlas with a separately drawn view for every angle. Warm/blue
color transitions mix in the mask shader without overlapping translucent nodes.

The live flame uses a reusable 12x16 subdivided mesh. Vertex bending moves its
crown in both screen axes, avoiding texture-boundary clipping. A separate
continuous combustion clock changes speed smoothly without jumping noise phase.
Sparks follow the same airflow. Dash stretching uses the movement axis (including
diagonals), followed by recovery. Existing strike, hurt, cast and death poses
compose with the locomotion rig.

## Validation

C# build, actual Godot GPU rendering of production `PlayerVisual`, and Windows
export. `Tools/render_hero_preview.gd` loads only `HeroArtPreview`: eight movement
directions, warm/blue palettes, idle, a continuous orbit, reversal and diagonal
dash/recovery. It writes 192 frames at 60 samples/second to `.tools/hero-preview`.
The preview contains no Player, RunManager, level or combat execution.

`../art/hero-responsive-60fps.webp` is the 3.2-second animated art preview;
`../art/hero-responsive-contact.png` is a full-resolution contact frame.
Gameplay and automated game tests were not run under the existing restriction.
In-game interaction and readability still need user play-through.
