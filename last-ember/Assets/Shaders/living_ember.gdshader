shader_type canvas_item;
render_mode unshaded;

uniform float flame_time = 0.0;
uniform float blue_blend : hint_range(0.0, 1.0) = 0.0;
uniform float life : hint_range(0.0, 1.0) = 1.0;
uniform vec2 airflow = vec2(0.0);
uniform float surge = 0.0;
uniform float movement = 0.0;
uniform float turn = 0.0;

void vertex() {
    // Root remains attached to the mask; crown bends in any screen direction.
    float crown=clamp((18.0-VERTEX.y)/76.0,0.0,1.0);
    float flexibility=pow(crown,1.65);
    VERTEX-=airflow*vec2(20.0,24.0)*flexibility;
    VERTEX.x+=turn*sin(crown*3.0)*crown*5.0;
    VERTEX.y-=surge*5.0*crown;
}

float hash21(vec2 p) {
    p = fract(p * vec2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return fract(p.x * p.y);
}
float noise2(vec2 p) {
    vec2 i = floor(p), f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    return mix(mix(hash21(i), hash21(i + vec2(1.0, 0.0)), f.x),
        mix(hash21(i + vec2(0.0, 1.0)), hash21(i + vec2(1.0)), f.x), f.y);
}
float flow_noise(vec2 p) {
    return noise2(p) * 0.54 + noise2(p * 2.03 + 17.1) * 0.28
        + noise2(p * 4.07 + 31.7) * 0.13 + noise2(p * 8.13) * 0.05;
}
float plume(vec2 uv, float base_x, float width, float height, float seed) {
    float base_curve = pow((uv.x - base_x) / max(width, 0.05), 2.0);
    float base_y = 0.88 - min(base_curve * 0.08, 0.24);
    float rise = (base_y - uv.y) / height;
    float t = flame_time;
    vec2 advect = vec2(uv.x * 6.0 + seed, uv.y * 7.0 + t * 2.1);
    float broad = flow_noise(advect);
    float detail = flow_noise(advect * 1.83 + vec2(t * 0.32, t * 0.7));
    float bend = (broad - 0.5) * 0.18 * rise + sin(rise * 5.0 - t * 2.7 + seed) * rise * 0.025;
    bend += sin(rise*9.0-t*8.0+seed)*rise*movement*0.018;
    float taper = width * mix(1.0, 0.12, clamp(rise, 0.0, 1.0));
    float edge_distance = abs(uv.x - base_x - bend) / max(taper, 0.008);
    float edge = edge_distance + (detail - 0.5) * 0.85;
    float envelope = (1.0 - smoothstep(0.48, 1.12, edge))
        * smoothstep(-0.04, 0.14, rise)
        * (1.0 - smoothstep(0.66 + broad * 0.14, 1.0, rise));
    return envelope * (0.62 + detail * 0.55);
}
vec3 warm_color(float heat) {
    vec3 c = mix(vec3(0.6, 0.025, 0.002), vec3(1.0, 0.18, 0.008), smoothstep(0.0, 0.35, heat));
    c = mix(c, vec3(1.0, 0.62, 0.09), smoothstep(0.3, 0.72, heat));
    return mix(c, vec3(1.0, 0.97, 0.76), smoothstep(0.69, 1.0, heat));
}
vec3 cold_color(float heat) {
    vec3 c = mix(vec3(0.02, 0.06, 0.35), vec3(0.025, 0.36, 1.0), smoothstep(0.0, 0.35, heat));
    c = mix(c, vec3(0.12, 0.82, 1.0), smoothstep(0.3, 0.72, heat));
    return mix(c, vec3(0.87, 0.98, 1.0), smoothstep(0.69, 1.0, heat));
}
void fragment() {
    vec2 uv = UV;
    float density = plume(uv, 0.5, 0.19, 0.83, 0.0);
    density = max(density, plume(uv, 0.34, 0.11, 0.57, 8.3) * 0.86);
    density = max(density, plume(uv, 0.66, 0.12, 0.64, 19.2) * 0.91);
    float filaments = flow_noise(vec2(uv.x * 12.0, uv.y * 17.0 + flame_time * 5.3));
    float heat = clamp(density * (0.78 + filaments * 0.35), 0.0, 1.0);
    float alpha = smoothstep(0.025, 0.35, density) * (0.58 + heat * 0.42);
    alpha *= smoothstep(0.0, 0.035, uv.x) * (1.0 - smoothstep(0.965, 1.0, uv.x));
    alpha *= smoothstep(0.0, 0.025, uv.y) * (1.0 - smoothstep(0.91, 1.0, uv.y));
    COLOR = vec4(mix(warm_color(heat), cold_color(heat), blue_blend), alpha * life);
}
