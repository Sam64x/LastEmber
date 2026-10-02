# Visual atmosphere

Every generated room has a cosmetic layer below actors and attack telegraphs:

- Darkness: drifting ash, ground scuffs and low wisps visible in existing light.
- Frost: falling snow, fine ice cracks and cold ground mist.
- Inferno: rising embers, warm floor seams and drifting haze.

The layer uses an independent RNG seeded by room geometry. It does not change
encounters, collision, damage, navigation or gameplay light coverage. Animation
stops when gameplay is paused. Particle storage is bounded to 96 motes per room.

At the safe entrance to each biome, its title and lesson fade in and out over
three seconds. The intro accepts no input and clears when leaving the room.

Main menu SETTINGS contains music, effects, atmosphere density (default 75%) and
torch flicker. Density zero disables the entire atmosphere layer. Torch flicker
only changes boss-arena torch brightness slightly; their radius and extinguish
rules remain the same. Visual settings persist in `user://visual_settings.cfg`.

Verification is limited to C# compilation, Godot editor import and Windows export.
Do not launch gameplay, automated game tests or captures without user permission.
