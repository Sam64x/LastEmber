shader_type canvas_item;
render_mode unshaded;
uniform float effect_age=0.0;
uniform float seed=0.0;
uniform float phase=0.0;
uniform float blue_blend=0.0;
uniform float opacity=1.0;
uniform float power=1.0;

vec4 sample_frame(sampler2D atlas,vec2 uv,float frame){
    vec2 cell=vec2(mod(frame,2.0),floor(frame/2.0));
    return texture(atlas,(clamp(uv,vec2(0.005),vec2(0.995))+cell)*0.5);
}
void fragment(){
    vec2 uv=UV;
    // Low-amplitude flow animates illustrated tongues without replacing the art.
    float trail=1.0-smoothstep(0.25,0.75,uv.x);
    uv.y+=sin(uv.x*18.0-effect_age*19.0+seed)*0.004*trail;
    uv.x+=sin(uv.y*21.0-effect_age*13.0)*0.003;
    float frame=floor(phase), blend=smoothstep(0.0,1.0,fract(phase));
    vec4 a=sample_frame(TEXTURE,uv,frame),b=sample_frame(TEXTURE,uv,min(3.0,frame+1.0));
    float alpha=mix(a.a,b.a,blend);
    vec3 rgb=mix(a.rgb*a.a,b.rgb*b.a,blend)/max(alpha,0.0001);
    float bright=max(rgb.r,max(rgb.g,rgb.b));
    vec3 cold=vec3(rgb.b*0.85,rgb.g*0.85+0.12,rgb.r);
    rgb=mix(rgb,cold,blue_blend*smoothstep(0.2,0.4,bright));
    COLOR=vec4(rgb*mix(0.92,1.02,power),alpha*opacity);
}
