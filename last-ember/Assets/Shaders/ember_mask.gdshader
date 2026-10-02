shader_type canvas_item;
render_mode unshaded;
uniform vec2 facing=vec2(0.0,1.0);
uniform float breath=0.0;
uniform float blue_blend=0.0;
varying vec4 vertex_color;
void vertex() { vertex_color=COLOR; }

vec4 mask_view(sampler2D atlas,vec2 cell,float column,float row,float mirror) {
    // Register the three-quarter mask to the same pivot as front/back.
    if(mirror<0.0)cell.x=1.0-cell.x;
    if(column>0.5&&column<1.5)cell.x+=0.095;
    vec2 source=clamp(cell,vec2(0.002),vec2(0.998));
    vec4 tex=texture(atlas,(source+vec2(column,row))/vec2(3.0,2.0));
    vec2 center=column>0.5&&column<1.5?vec2(0.63,0.64):vec2(0.5,0.64);
    float eye_guard=1.0-smoothstep(0.16,0.24,length((source-center)*vec2(1.2,1.5)));
    float chroma=abs(tex.r-tex.b);
    float brightness=max(tex.r,max(tex.g,tex.b));
    float painted_fire=smoothstep(0.08,0.27,chroma)*smoothstep(0.22,0.48,brightness);
    tex.a*=1.0-painted_fire*(1.0-eye_guard);
    tex.rgb*=1.0+breath*0.035*eye_guard;
    tex.rgb*=tex.a;
    return tex;
}
void fragment() {
    vec2 cell=fract(UV*vec2(3.0,2.0));
    float yaw=acos(clamp(facing.y,-1.0,1.0))/3.14159265;
    float mirror=facing.x<0.0?-1.0:1.0;
    // Continuous local perspective between registered authored views.
    cell.x-=facing.x*0.022;
    cell.y+=facing.y*0.012;
    // The authored side view is three-quarter: hold it at the diagonals.
    // Short transitions avoid double eyes when holding a diagonal aim.
    float a=yaw<0.5?0.0:1.0;
    float weight=yaw<0.5?smoothstep(0.07,0.21,yaw):smoothstep(0.64,0.72,yaw);
    vec4 warm=mix(mask_view(TEXTURE,cell,a,0.0,mirror),mask_view(TEXTURE,cell,a+1.0,0.0,mirror),weight);
    vec4 cold=mix(mask_view(TEXTURE,cell,a,1.0,mirror),mask_view(TEXTURE,cell,a+1.0,1.0,mirror),weight);
    vec4 color=mix(warm,cold,blue_blend);
    color.rgb/=max(color.a,0.0001);
    COLOR=color*vertex_color;
}
