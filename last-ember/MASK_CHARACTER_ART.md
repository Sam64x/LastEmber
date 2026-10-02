# Ember mask — active character design

The revised user references describe a compact floating obsidian mask with
almond-shaped emissive eyes. Fire ribbons form its entire silhouette. There is
no torso, cloak, scarf, hand or foot. Broad faceted shapes and clean flame edges
take precedence over fine surface noise. The second reference is the identity
reference; the first describes animation beats and separate combat effects.

## Production asset

`Assets/Characters/ember-mask-atlas-v2.png`: 1536 × 1024 RGBA, regular 3 × 2 grid.
Front, right three-quarter and back are the top row; corresponding blue/cyan/
white Last Ember poses are the bottom row. Side poses mirror for left-facing.
The old art is retained as an unused earlier revision. HUD uses the active atlas.

Production cells render at 64px with the mask center at the physics origin.
Transparent gutters and alpha checked; offline contact sheet is available at
`../art/ember-mask-preview-v2.png` and can be regenerated with
`Tools/preview_character_art.py`. This is an asset preview, not a gameplay capture.

## Animation direction and implementation

- Idle: slow vertical float, small breathing deformation; faster independent
  flame-wave motion above and beneath a stable mask. Sparks detach and rise.
- Travel: velocity-smoothed lean, reduced float, faster flame motion and
  direction-aware poses. The mask stays compact and readable.
- Strike: existing attack preparation, impulse, rotation and squash/stretch are
  inherited from the actual melee timeline, including charged attacks. Existing
  flame slashes remain independent of the body and hit timing.
- Dash: direction-dependent stretch/squash, two tapered flame ribbons, seven
  bounded world-positioned sprite ghosts, then exponential shape recovery.
  The old round ghost particles were removed from player dashes.
- Q: an activation-triggered 0.75s cosmetic sequence. The body gathers for 0.15s;
  concentric compressed rings and eight orbiting embers expand and settle. The
  palette snapshots the actual paid/blue cast state. This covers all attunements,
  not only abilities whose runtime reports `Reveals`.
- Hurt: short brightness pulse and directional recoil, without invulnerability
  flicker hiding the character.
- Death: the mask contracts and fades while sixteen faceted fragments and
  embers spread, descend and dissipate over 1.2s.
- Last Ember: warm and blue sprites crossfade, preserving shape and orientation.

The rig animates three illustrated directional poses continuously through a
strip mesh and layered VFX. It is not a six-frame hand-painted idle/walk atlas.
Gameplay timings, collision, damage, input and light coverage are unchanged.
Pause/reward/hit-stop freeze the rig; the death sequence can advance under the
game-over overlay. Transitions clear ghosts and transient action presentation.

## Generation provenance

Built-in imagegen, using only the two new supplied images as references. No CLI
or API fallback. The final image is copied into the repository with original
alpha preserved. Generation prompt:

> Use case: stylized-concept. Create a production transparent sprite atlas for Last Ember using the TWO attached NEW reference boards only. Exact subject: a TINY FLOATING EMBER MASK, a compact teardrop of black faceted obsidian with two big creamy glowing almond eyes, three pointed crown ridges, bright orange flowing flame above and around the mask and a little flame point below. NO BODY, NO CLOAK, NO SCARF, NO HANDS, NO ARMS, NO LEGS, NO FEET, NO ARMOR SUIT. Faithful to new reference2 top-down gameplay drawing and normal-state drawing, clean broad painted facets not heavily cracked noisy rocks. Elevated orthographic gameplay perspective, compact round/teardrop silhouette, spirited mischievous determined eyes. ONE precisely regular atlas: 3 columns x 2 rows, six equal square cells. Top row front, three-quarter facing right, back; bottom row matching poses in cold BLUE CYAN WHITE Last Ember palette. Every mask at same height/scale, center of mask at 60% cell height, flame envelope within central 80% of each cell with transparent padding. No text, no grid, no environment, no presentation board, no floor, no background glow clouds, absolutely no humanoid character. True alpha transparent backdrop and clean transparent gutters. Premium polished painterly 2D action roguelite art, punchy crisp contour, recognizable at48px. Make the small mask itself the entire character, surrounding flame ribbons provide all movement and silhouette. Preserve reference character shape, no extra body.

## Verification

C# build, Godot editor import and Windows release export. Alpha and 64px
silhouette inspected offline. No gameplay runs, automated game tests or gameplay
captures were launched, consistent with the existing user restriction.
