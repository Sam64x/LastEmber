shader_type canvas_item;
uniform float frame_a=0.0;
uniform float frame_b=0.0;
uniform float pose_blend=0.0;
uniform float flash=0.0;
uniform float opacity=1.0;
uniform float clock=0.0;
uniform float attack_width=1.0;
uniform float idle_bottom=1.0;
uniform float windup_top=0.0;
vec4 pose(sampler2D atlas,vec2 uv,float frame){
    vec2 cell=vec2(mod(frame,3.0),floor(frame/3.0));
    uv=clamp(uv,vec2(.003),vec2(.997));
    if(frame>2.5&&frame<3.5)uv.y=mix(-windup_top,1.0,uv.y);
    if(frame>3.5&&frame<4.5)uv.x*=attack_width;
    vec4 pixel=texture(atlas,(uv+cell)/vec2(3.0,2.0));
    // Raised weapons in the next row must not leak into the idle cell.
    if(frame<.5)pixel.a*=1.0-smoothstep(idle_bottom-.006,idle_bottom,uv.y);
    return pixel;
}
void fragment(){
    vec2 uv=UV;
    // Cloth breathes subtly while the foot pivot stays stable.
    uv.x+=sin(clock*2.0+uv.y*7.0)*.002*(1.0-smoothstep(.3,.8,uv.y));
    vec4 a=pose(TEXTURE,uv,frame_a),b=pose(TEXTURE,uv,frame_b);
    float alpha=mix(a.a,b.a,pose_blend);
    vec3 rgb=mix(a.rgb*a.a,b.rgb*b.a,pose_blend)/max(alpha,.0001);
    COLOR=vec4(mix(rgb,vec3(1.0,.85,.65),flash*.65),alpha*opacity);
}
