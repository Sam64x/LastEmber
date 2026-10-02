shader_type canvas_item;
render_mode unshaded;
uniform float effect_time=0.0;
uniform float blue_blend=0.0;
float hash(vec2 p){return fract(sin(dot(p,vec2(127.1,311.7)))*43758.5453);}
float noise(vec2 p){
    vec2 i=floor(p),f=fract(p);f=f*f*(3.0-2.0*f);
    return mix(mix(hash(i),hash(i+vec2(1,0)),f.x),mix(hash(i+vec2(0,1)),hash(i+vec2(1,1)),f.x),f.y);
}
void fragment(){
    float opacity=COLOR.a;
    vec2 flow=vec2(UV.x*7.0-effect_time*6.0,UV.y*4.0);
    float n=noise(flow)*.7+noise(flow*2.13)*.3;
    float edge=abs(UV.y-.5)*2.0;
    float shape=1.0-smoothstep(.35+n*.28,.78+n*.22,edge);
    float heat=clamp((1.0-edge)*(.6+n*.5),0.0,1.0);
    vec3 warm=mix(vec3(.95,.12,.008),vec3(1.0,.67,.12),heat);
    warm=mix(warm,vec3(1,.94,.66),smoothstep(.75,1.0,heat));
    vec3 cold=mix(vec3(.02,.22,.85),vec3(.56,.94,1),heat);
    COLOR=vec4(mix(warm,cold,blue_blend),shape*opacity*.8);
}
