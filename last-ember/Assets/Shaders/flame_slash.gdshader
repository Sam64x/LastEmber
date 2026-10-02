shader_type canvas_item;
render_mode unshaded;
uniform float effect_age = 0.0;
uniform float seed = 0.0;
uniform float blue_blend = 0.0;
uniform float opacity = 1.0;
uniform float power = 1.0;

float hash(vec2 p) { return fract(sin(dot(p, vec2(127.1,311.7))) * 43758.5453); }
float flow(vec2 p) {
    vec2 cell=floor(p), f=fract(p);f=f*f*(3.0-2.0*f);
    return mix(mix(hash(cell),hash(cell+vec2(1.0,0.0)),f.x),
        mix(hash(cell+vec2(0.0,1.0)),hash(cell+vec2(1.0)),f.x),f.y);
}
void fragment() {
    float t=UV.x, across=UV.y;
    vec2 p=vec2(t*19.0-effect_age*13.0+seed, across*4.5+effect_age*3.0);
    float turbulence=flow(p)*0.65+flow(p*2.07+17.3)*0.25+flow(p*4.1)*0.1;
    float spine=0.43+sin(t*11.0+seed-effect_age*10.0)*0.035;
    float radial=abs(across-spine);
    float tongues=pow(max(0.0,sin(t*29.0-effect_age*18.0+seed)),4.0)*across*0.1;
    float edge=radial+(turbulence-0.5)*0.38-tongues;
    float envelope=(1.0-smoothstep(0.23,0.5,edge))*smoothstep(0.0,0.12,t)*(1.0-smoothstep(0.98,1.0,t));
    float core=exp(-radial*radial*55.0)*(0.68+turbulence*0.32);
    float filaments=smoothstep(0.62,0.9,turbulence)*0.26;
    float heat=clamp(core+filaments+(1.0-radial*2.0)*0.2,0.0,1.0);
    vec3 warm=mix(vec3(0.85,0.06,0.005),vec3(1.0,0.58,0.08),smoothstep(0.0,0.6,heat));
    warm=mix(warm,vec3(1.0,0.98,0.78),smoothstep(0.58,0.95,heat));
    vec3 cold=mix(vec3(0.015,0.2,0.85),vec3(0.12,0.78,1.0),smoothstep(0.0,0.6,heat));
    cold=mix(cold,vec3(0.88,0.98,1.0),smoothstep(0.58,0.95,heat));
    float torn_tail=mix(0.62,1.0,smoothstep(0.18,0.5,turbulence));
    COLOR=vec4(mix(warm,cold,blue_blend),envelope*torn_tail*opacity*power);
}
