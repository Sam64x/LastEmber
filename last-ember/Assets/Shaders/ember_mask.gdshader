shader_type canvas_item;
render_mode unshaded;

// Remove bright chromatic painted fire; retain obsidian and protected eye region.
varying vec4 vertex_color;
void vertex() { vertex_color = COLOR; }
void fragment() {
    vec4 tex = texture(TEXTURE, UV);
    vec2 cell = fract(UV * vec2(3.0, 2.0));
    float column = floor(UV.x * 3.0);
    vec2 center = column > 0.5 && column < 1.5 ? vec2(0.63, 0.64) : vec2(0.5, 0.64);
    float eye_guard = 1.0 - smoothstep(0.16, 0.24, length((cell - center) * vec2(1.2, 1.5)));
    float chroma = abs(tex.r - tex.b);
    float brightness = max(tex.r, max(tex.g, tex.b));
    float painted_fire = smoothstep(0.08, 0.27, chroma) * smoothstep(0.22, 0.48, brightness);
    tex.a *= 1.0 - painted_fire * (1.0 - eye_guard);
    COLOR = tex * vertex_color;
}
