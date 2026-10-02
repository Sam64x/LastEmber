# Unified flame combat artwork

The hero's obsidian mask, warm ivory core, orange flame tongues and charcoal
fragments define the visual family. Slash crescents, airborne embers and radial
splashes now share that painted language. Last Ember remaps the hot flame to
blue while retaining the dark fragments. Hostile projectiles retain their tint.

## Assets and animation

- `Assets/Characters/ember-projectile-atlas-v1.png`: four flight-loop phases in
  a 2x2 RGBA atlas. Built-in imagegen generated it and then corrected its spacing.
  The shader registers the measured bright-core centroid of each cell to a fixed
  pivot, preventing a vertical wobble between rows. The loop blends phase 3 to 0.
- `Assets/Characters/ember-splash-atlas-v1.png`: four phases of ignition, expanding
  fire, breakup and embers, generated with built-in imagegen. Expansion lasts
  120ms, with the tail ending at 580ms. Contact impacts use a short 260ms burst
  from the same artwork, with existing critical/charged/armor/ignite layers.
- Existing `slash-crescent-atlas-v2.png`: now unfurls over 35ms, sweeps through
  a short angular follow-through, then breaks up. Opening/return cuts mirror;
  width and charge distinguish finishers. Secondary melee waves reuse this art.

These are four authored phases with render-frame interpolation and subtle UV
flow, not 24 or 60 separately painted frames. Assets preserve generated alpha.

## Integration

Core bolts, lances, hostile shots and orbiting embers use the projectile renderer.
Range expiration fades friendly bolts; wall contacts produce a cosmetic impact.
The projectile head remains aligned with its collision position. Existing speed,
collision radius, piercing, damage and target selection are unchanged.

Explosion, Dash landing, Wildfire and Backdraft use pooled painted splashes.
Twelve concurrent splash slots and the existing 24 impact slots bound the cost;
room cleanup cancels both pools. Hostile warning telegraphs remain distinct.

Dash now folds the single hero flame along travel, compresses/stretches briefly,
and recovers over 180ms. `DashWake` is connected to actual world-position samples;
its trail stays on the traveled path and fades in 220ms. It replaces painted
whole-hero ghosts and the old two-line comet. Room changes clear the trail.

## Verification

C# build, Godot import, production shaders in isolated art viewports, Windows
export. `Tools/render_family_preview.gd` renders 192 frames at 60 samples/second:
four slash styles, four projectile variants and contacts, warm/blue splash, Dash
and idle hero. `art/combat-family-60fps.webp` (repository root) plays those frames
over 3.2 seconds; `art/combat-family-contact.png` is the static contact sheet.
The existing combat-art preview also checks critical, charge, armor and ignite
layers. No game scene or automated gameplay test is loaded. Gameplay overlap
and final balance readability still need user play-through.

## Built-in imagegen prompts

### Projectile

Production 2D game VFX transparent sprite atlas, matching the supplied reference image ONLY for painterly flame style, palette and tiny charcoal facets. New asset: a burning ember projectile, travelling RIGHT. ONE perfectly regular 2x2 square-cell atlas, four phases of a seamlessly looping flight animation, read row major. In every cell the SAME bright compact pointed ivory-yellow molten core centered exactly at local x=72%, y=50%; long torn orange-red flame tail streams to the LEFT ending near x=12%. Entire effect within cell x=8..90%, y=27..73%. Slightly different twisting flame tongues and drifting tiny charcoal chips in each frame, same core pivot, scale and camera. Warm orange, gold, cream and obsidian. A fierce magical ember spear, no face, no eyes, no character, no mechanical arrow, no perfectly straight laser line. Strong clear silhouette readable at 50 pixels. Rich hand-painted flame tongues like the reference crescent, small amount of ember fragments; NO large rocks. No labels, numbers, grid, scene, ground or backdrop. Actual transparent alpha background.

### Projectile registration correction

Correct this projectile animation sprite atlas for production registration. Keep same painterly flame projectile, orange/ivory colors, small charcoal shards and four phase variations. Make the grid PERFECTLY REGULAR 2x2, each cell identical square size. Critical correction: shrink ALL four sprites to occupy ONLY the central 65% of each cell. Maintain a wide clear transparent gutter between cells. In EVERY cell, put the bright core/tip at exactly local (75%,50%); trail extends left to x=15%; sprite vertical bounds y=32% to y=68%. All cores must have exactly same y=50% within their own cell. No part crosses cell boundary, ample alpha space on all sides. Row 1 and row 2 must be IDENTICALLY registered vertically. Preserve actual alpha transparency. No text, labels or grid. This is a precision layout correction, not a new design.

### Splash

Create a production transparent 2D top-down game fire EXPLOSION animation atlas in exactly four equal square cells, 2x2 grid. Reference image only supplies painterly style, orange/ivory flame palette, obsidian ember fragments. New subject: radial circular flame splash with jagged organic tongues and a few small dark charcoal fragments. Not a slash, not a projectile. Top left ignition: bright compact ivory-orange asymmetric fire flower filling 50% of cell width. Top right expansion: broad circular curling orange fire front filling 74% of cell width, molten bright rim and partially hollow center. Bottom left breakup: same-sized ragged interrupted ring of orange flame tongues, thin warm wisps and scattered small embers, center empty. Bottom right dissipation: scattered orange ember streaks in the same circular footprint, no solid ring. All four share exact center (50%,50%), same camera, fully circular top down, no ground plane or perspective. Each effect must be COMPLETELY INSIDE its own cell with at least 12% fully transparent margin on ALL sides. Nothing crosses the vertical or horizontal center lines of the entire canvas. Flame is detailed, sharp painterly shapes, no neon lines, no smooth geometric rings, no large rocks. Actual alpha transparency, no black background, no checkerboard, no glow filling empty areas, no text, no numbers, no grid.
