# Heavy Core charge artwork

`Assets/Characters/heavy-charge-atlas-v1.png` is a transparent 2x2 atlas created
with built-in imagegen. Four stages progress from loose inward fire tongues to
a compact bright core. `HeavyChargeFx` interpolates them using the real Heavy
Core charge ratio. Continuous rotation/flow animate holding; reaching full charge
adds a short flash and a small sustained pulse. Warm and Last Ember palettes use
the shared combat-art shader. The effect follows aim in front of the mask.

`FireStrikeFx` replaces its old charge lines/circles with this renderer. Release,
cancel, room reset and beginning a strike hide the charge effect. Combat timing
and charge mechanics are unchanged. Four painted phases are blended at render
cadence; they are not individually painted 60fps frames.

Verification: C# build, Godot import, isolated production-renderer preview and
Windows export. `Tools/render_charge_preview.gd` captures gathering, sustained
full charge and release in both palettes. No game scene is loaded.
Preview: `../art/heavy-charge-60fps.webp`.

## Imagegen prompt

Production game VFX sprite atlas, 2x2 equal square cells, four stages of charging a HEAVY FIRE ATTACK. Painterly 2D dark-fantasy ember magic, molten ivory gold core, rich orange red fire ribbons, small obsidian charcoal fragments. No character. Each effect centered EXACTLY in its cell. Upper left: thin loose asymmetrical tongues curling inward around a small ember. Upper right: thicker inward curling fiery crescent gathering into an orange core. Lower left: dense compact molten core enclosed by broad hooked flame tongues and tiny charcoal fragments. Lower right: fully charged bright ivory-gold core with a fierce compact flame corona, same size as lower left, distinct brighter ready state. ONE cohesive swirling charge, not three separate flames, not an explosion, no outward spike star, no geometric circles or neon rings. Four registered chronological stages, identical center and camera, every effect fits inside central 70 percent of its cell with generous transparent margins and gutters. Strong silhouette readable at 64 pixels. Actual alpha transparent background, no backdrop, no text, no grid or labels. Style matches hand-painted fiery crescent slash effects with textured hot cores and jagged orange edges.
