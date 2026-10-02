# Continuous mask fire

The mask design and directional atlas are preserved. Main-character rendering
now separates the obsidian mask/eyes from the fire instead of animating the
entire painted sprite as one object.

- `Assets/Shaders/ember_mask.gdshader` suppresses bright chromatic painted flames
  outside a protected eye region, retaining dark shell facets and emissive eyes.
- `Assets/Shaders/living_ember.gdshader` produces three tapered, rising plumes
  with advected four-octave value noise, layered turbulence, fine heat filaments,
  soft changing contours and density-based color/opacity. Temperature-style
  gradients range from deep red through amber to an ivory core. Last Ember blends
  into blue/cyan/white using the same continuously evolving field.
- `LivingFlame` draws one bounded 80 × 86 local quad. Flow bends opposite lateral
  velocity, adjusts height for vertical movement and accelerates during a dash.
- `PlayerVisual` supplies an explicit render-frame clock, airflow, color blend
  and death fade. The mask retains strike, dash, casting and hurt body transforms.
  Pauses and hit-stop freeze shader time; built-in shader `TIME` is not used.

Animation updates through `_Process` at every rendered frame; it has no sprite
frame counter, 6/8-frame loop or 24fps cap. At a rendering rate of 24, 60 or 120fps
the flame receives that many clock updates per second. This does not force a
minimum hardware frame rate; actual FPS has not been measured. GPU work and
allocation are bounded; no per-frame image generation or texture uploads occur.

Existing short dash ghosts use the atlas/strip mesh. HUD portraits remain static
icons. Collision, damage, gameplay lights and attack timing are unchanged.

Verification: C# compilation, Godot editor import and Windows export. Both
shader assets also compiled and rendered in an isolated OpenGL asset viewport
(no game scene, player controller or level loaded). Sixty samples form the
two-second 30fps preview at `../art/living-flame-30fps.gif`, showing enlarged and
production-size warm/blue fire. Preview durations alternate 33/33/34ms.
Gameplay, automated game tests and gameplay capture were not launched under the
existing user restriction. In-game composition and actual game FPS still need
review; the preview playback rate is not a game-performance measurement.
