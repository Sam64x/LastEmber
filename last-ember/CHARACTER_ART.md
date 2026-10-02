# Ember spirit: character art and presentation

This is the archived first character revision. The current design removes the
cloak and body entirely; see `MASK_CHARACTER_ART.md` for the active asset and rig.

## Reference analysis

The first user-provided board establishes animation states and readability at a
small gameplay scale. Its attack effects are separate from the body: this is
important for maintaining clear silhouettes and the existing combat timings.

The second board is the primary art direction: a basalt crown framing a hollow
face, two luminous eyes, layered torn ash cloth, an exposed molten core and a
large flame crown. The design hierarchy is silhouette first, eyes second,
material texture third. Warm amber/orange contrasts against charcoal; Last Ember
uses blue/cyan/white while preserving the character's identity.

The supplied presentation sheets are references, not uniform animation atlases.
Their text, environment and irregular spacing make them unsuitable for direct
frame slicing. A dedicated transparent production atlas was generated instead.

## Implemented

- `Assets/Characters/ember-spirit-atlas.png`: RGBA, 1536 × 1024; six equal 512px
  cells, three columns and two rows. Front, three-quarter and back in the first
  row; their blue equivalents in the second. Side facing is mirrored.
- `PlayerVisual`: 76px cell size in the world, velocity-based lean and gait bob,
  breathing, continuous strip-mesh flame/cloth sway, detached animated sparks,
  existing melee squash/rotation/impulse,
  seven bounded world-positioned dash ghosts, hurt flash and death collapse.
- Warm/blue art blends gradually. A low ellipse shadow and ground glow anchor
  the character. Reveal adds a compressed ring at the feet.
- `EmberPortrait`: HUD portrait follows the actual Last Ember state.
- Animation freezes for pause, reward selection and hit-stop. Death presentation
  can continue under the game-over overlay. Room transitions clear old trails.

This uses three directional poses with procedural animation, not the six/eight
hand-painted animation frames pictured in the reference. Separate animation
atlases and independent cloak/flame layers remain a future art expansion.

Character presentation is unshaded to preserve its emissive identity. Collision
radius, movement, aim, combat origins, attack timing, invulnerability and gameplay
light coverage are unchanged. No added RNG touches encounter generation.

## Generation provenance

Created with the built-in imagegen tool using both supplied boards as references,
then a background-extraction edit using the generated atlas. Source generation
remains under the Codex generated-images directory; the final alpha asset is
copied into this repository. No API/CLI fallback was used.

Final generation prompt:

> Use case: stylized-concept. Asset type: production game character directional atlas for Last Ember, transparent PNG. The attached images are design references only, especially image 2. Create ONE perfectly regular 3-column by 2-row atlas, six isolated full-body sprites of the SAME tiny flame spirit, with uniform size and identical ground anchor in every cell. Top row orange normal state: front facing south, three-quarter facing southeast, back facing north. Bottom row exactly corresponding identical designs and poses, but icy blue Last Ember state. Each sprite centered within its equal cell, large clear transparent gutters; absolutely no overlap, no text, no grid, no environment, no floor shadow, no checkerboard painted into image. Body compact, large jagged charcoal basalt crown mask, hollow dark face with two luminous almond eyes (not on back), ragged layered ash cloak and scarf, exposed radiant flame core below mask, small hands, no weapons. Flame crown rises above rocky head, flowing fire tapered tips, bright creamy yellow inner tongues, orange edge lighting on fractured basalt and tattered cape; lower row blue cyan white flame, cool dark slate cloak. Premium painterly 2D roguelite art with crisp readable silhouette and refined material edges, like reference2, slightly elevated orthographic gameplay camera, NOT side view, NO realistic human. All six occupy same proportional bounding box, base of cloak at 85% cell height, flame tip at15% cellheight. Detailed but readable at 64px in-game. True alpha transparent background, no opaque background whatsoever. Sprite atlas only, no presentation board.

Final background-extraction prompt:

> Use case: background-extraction. Edit target: the provided six-sprite atlas. Remove ONLY the colored orange/blue opaque backdrop and its large blurry light clouds. Output a true RGBA PNG with alpha=0 throughout the background and the gaps between sprites. Preserve the six character sprites exactly: their position, scale, orientation, rocky dark material, eyes, flame crown, cloak, hands, lower flame and tiny detached ember particles. Preserve the original 3-column x2-row layout and canvas proportions. Character edge fire can have antialiased alpha but no large diffuse backdrop glow. No white/black/color/checkerboard solid background. This is a texture used directly inside Godot and a solid colored backdrop would be a defect. TRANSPARENT BACKGROUND REQUIRED. Keep the same artwork and six cells, change only background transparency.

## Verification

Alpha channel and cell dimensions checked offline; opaque character interiors
and transparent gutters inspected against a dark background. Preview contact
sheet includes production scale. C# compilation, Godot editor import and Windows
export only; no gameplay, automated game tests or gameplay captures launched.
