# Reference-based slash revision

The previous thin luminous ribbon missed the supplied broad flame crescent.
The replacement has a thick ivory-yellow cutting belly, orange flame tongues,
torn trails and charcoal fragments, following the supplied four-phase sheet.

`Assets/Characters/slash-crescent-atlas-v2.png` is a transparent 1254x1254 RGBA
atlas: four 627x627 cells, read left-to-right then top-to-bottom. The builtin
imagegen tool generated it using the user's slash sheet as the visual reference.
No reference background or labels are part of the runtime sprite.

The shader blends adjacent phases using premultiplied alpha and applies subtle
UV flow each rendered frame. The existing attack timeline drives the sequence;
this is continuous playback of four painted phases, not 24 unique drawn frames.
The return cut mirrors around its local center; the finisher and full-charge
profiles are wider. The bright crescent starts on release after the windup bead.
Warm and blue palettes, captured stroke origins and bounded node reuse remain.
Old geometric cleave triangles and line cinders were removed from the slash.

Validation: C# build, Godot shader rendering in the isolated production-effects
viewport, and Windows export. `../art/slash-reference-v2-60fps.webp` is a 96-frame
60fps art preview, with `../art/slash-reference-v2-contact.png` as a snapshot.
It does not load a game scene or measure gameplay FPS. Gameplay verification
remains under the existing user restriction.

## Generation prompt

Use case: stylized-concept. Asset type: production transparent fire-slash animation atlas. Input image is the EXACT design reference for the slash silhouette, painterly flame tongues, thickness, bright ivory yellow core, orange rim and torn burning trail. Match this reference closely; do not reinterpret as neon lines or uniform circular arcs. Create ONE perfectly regular 2 by 2 square-cell sprite atlas, 4 chronological frames of the SAME slash, no text or digits, no grid or backdrop. Each equal square cell contains one complete broad fiery CRESCENT, open to the LEFT, bulging cutting edge on the RIGHT, upper and lower horns curling back left. Strong asymmetry: wide creamy yellow-orange flame belly on right, large organic flame tongues sweeping inward and trailing left, jagged tapering trails, a few detached embers and tiny dark charcoal chips. A short thick fiery scythe slash like reference, not a clean geometric C curve, not laser, not rainbow, not glossy ribbon. Frame1 upperleft: fullest powerful bright crescent. Frame2 upperright: same position, continuing sweeping flame with slightly receding core and broken fiery tails. Frame3 lowerleft: thinner ember-orange crescent with separated tapered flames. Frame4 lowerright: dissipating narrower orange tongues with scattered sparks, recognizable final trace. All frames have identical virtual center, orientation, camera scale and effect bounds; do not translate, rotate or grow from cell to cell. Keep effect within middle80percent of each equal square cell with at least10percent transparent padding all sides; do not cross cell borders. Warm orange/red/yellow/ivory palette of supplied reference. Painterly high-end 2D top-down game VFX. NO character, NO UI, NO text, NO environment, NO impactburst. Actual alpha transparent background, absolutely no opaque colored background or painted checkerboard. Preserve the broad thick flame silhouette of the reference.
