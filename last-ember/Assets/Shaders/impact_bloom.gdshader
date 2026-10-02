shader_type canvas_item;
render_mode unshaded;
uniform float effect_age=0.0;
uniform float seed=0.0;
uniform float blue_blend=0.0;
uniform float critical=0.0;
uniform float strength=1.0;
float hash(vec2 p){return fract(sin(dot(p,vec2(127.1,311.7)))*43758.5453);}
float noise(vec2 p){
    vec2 i=floor(p),f=fract(p);f=f*f*(3.0-2.0*f);
    return mix(mix(hash(i),hash(i+vec2(1.0,0.0)),f.x),mix(hash(i+vec2(0.0,1.0)),hash(i+vec2(1.0)),f.x),f.y);
}
void fragment(){
    vec2 p=UV-vec2(0.5);float r=length(p);float angle=atan(p.y,p.x);
    float age=effect_age;
    float expansion=0.38+0.62*(1.0-exp(-age*70.0));
    float flare=pow(max(0.0,sin(angle*(4.0+critical*2.0)+seed)),5.0);
    float turbulence=noise(p*23.0+vec2(seed,age*9.0))*0.65+noise(p*47.0-age*13.0)*0.35;
    float radius=(0.19+flare*0.15+(turbulence-0.5)*0.07)*expansion*(0.8+strength*0.2)*(1.0+critical*0.22);
    float density=1.0-smoothstep(radius*0.72,radius,r);
    float heat=clamp((1.0-r/max(radius,0.001))*1.5+turbulence*0.35,0.0,1.0);
    vec3 warm=mix(vec3(0.9,0.065,0.004),vec3(1.0,0.58,0.06),smoothstep(0.1,0.6,heat));
    warm=mix(warm,vec3(1.0,0.99,0.85),smoothstep(0.6,0.95,heat));
    vec3 cold=mix(vec3(0.02,0.21,0.95),vec3(0.18,0.83,1.0),smoothstep(0.1,0.6,heat));
    cold=mix(cold,vec3(0.9,0.99,1.0),smoothstep(0.6,0.95,heat));
    float fade=1.0-smoothstep(0.03,0.16+critical*0.07,age);
    COLOR=vec4(mix(warm,cold,blue_blend),density*fade);
}
