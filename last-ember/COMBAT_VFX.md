# Flame slashes and contact impacts

The supplied slash sequence defines a bright curved fire blade with a hot spine,
tapered ends and trailing cinders. The impact reference defines an asymmetric
white-hot burst with orange tongues and detached sparks. Their presentation
backgrounds are not copied into runtime textures. Slashes use a transparent painted
atlas; contact impacts use continuous shader fields and shaped geometry.

`FlameSlashRibbon` and `flame_slash.gdshader` replace flat polygon ribbon layers:
the broad painted crescent carries a creamy core, large flame tongues, torn tails
and charcoal chips. Four registered phases blend with premultiplied alpha, with
subtle continuous flame flow. Opening/return cuts mirror vertically; finishers
and full charge retain their larger profiles. Preparation, release, tail and cooldown use the existing
strike timeline. Body follow-through now includes a short return overshoot.
Three overlapping ribbon nodes are reused. Stroke palettes snapshot at ignition.
See `SLASH_REFERENCE_V2.md` for the atlas, generation prompt and updated previews.

`FireImpactFx` uses a painted flame burst plus directional hot sparks and
independent extra layers. `ImpactTraits` flags combine:

| Trait | Visual response |
| --- | --- |
| Normal | Compact hot burst and directional embers |
| Critical | Larger six-lobed rupture, crossed hot cuts, wider spark spread |
| Fully charged | Expanding pressure arc and dense debris |
| Combo finisher | Additional shorter pressure arc |
| Armor break | Faceted icy fragments with bright edges |
| Armored contact | Cold defensive arc |
| Ignite | Longer rising combustion embers |
| Projectile | Smaller particle population |
| Arc | Short branching discharge |
| Secondary wave | Pressure arc at each wave contact |

Melee flags are assembled from actual `StrikeHit`, full-charge state, combo step
and armor state. Armor break requires ice armor to have existed before the break.
Critical + charged + armor break can all appear together. Palette snapshots match
the real hit. Existing critical-lightning presentation and sounds remain intact.
Projectiles, Arc contacts and secondary waves dispatch their own traits without
calling melee crit/combo hooks. Damage, hitboxes, cooldowns, hit-stop and procs
are unchanged; misses do not create contact impacts.

The effects manager preallocates 24 impact nodes, reuses them and cancels them
when the room changes. Saturation replaces a visual in that bounded pool, never
a damage event. All geometry and particle storage are bounded. Cosmetic random
seeds do not consume dungeon or combat RNG. Animation runs at rendering cadence,
with pause/hit-stop inherited from the world.

## Verification and art preview

C# compilation, Godot editor import and Windows export. Actual production effect
classes and shaders rendered in an isolated OpenGL SubViewport: no RunManager,
level, enemies or damage code. `Tools/CombatArtPreview.cs` samples the timeline
explicitly and writes 96 frames. `../art/combat-vfx-60fps.webp` plays those frames
over 1.6s using 17/17/16ms durations; `../art/combat-vfx-contact.png` is a contact
snapshot. This is art verification, not gameplay capture or a game-FPS benchmark.
Gameplay and automated game tests were not launched under the existing user
restriction. In-game overlap/readability still needs user review.

To regenerate source frames with the development Godot executable, use the
project path and `--script res://Tools/render_combat_preview.gd`. This starts
only the art viewport and writes PNGs into the repository's ignored
`.tools/combat-preview` directory.
