#version 450
layout(location = 0) in vec2 vUv;
layout(location = 0) out vec4 outColor;
#include "BX_ShaderCommon.glsl"
#include "BX_ShadowMirePlume.glsl"

layout(set = 2, binding = 0) uniform MaterialUniforms {
    float _Phase; // @default = 0.0
    float _Seed;  // @default = 0.0
} m;
layout(set = 2, binding = 1) uniform sampler2D _MainTex;

const vec3 navy = vec3(22.0,34.0,43.0)/255.0;
const vec3 deepBlue = vec3(130.0,181.0,219.0)/255.0;
const vec3 paleBlue = vec3(185.0,217.0,237.0)/255.0;

void main()
{
    vec2 p = vec2(2.6*(vUv.x-0.5), mix(-0.10,1.14,vUv.y));
    vec3 fire = bxPlumeFields(_MainTex, p);
    vec2 flow = vec2(p.x*5.6, (p.y-1.85*m._Phase)*5.1) + vec2(mod(m._Seed,71.0),mod(m._Seed,53.0));
    float fold = bxValueNoise(flow)-0.5;
    float fringe = bxValueNoise(1.9*flow+vec2(1.7*fold,-0.7*m._Phase))-0.5;
    float edge = fire.x + smoothstep(0.12,0.63,p.y)*(0.033*fold+0.015*fringe);
    float heart = max(fire.y+0.017*fold,edge+0.024);
    float aa = clamp(0.65*fwidth(edge),0.001,0.016);
    float light = smoothstep(0.06,0.87,fire.z);
    float line = mix(0.007,0.012,light)+0.36*aa;
    float outline = 1.0-smoothstep(line-aa,line+aa,edge);
    float fill = 1.0-smoothstep(-aa,aa,edge);
    float coreAA = clamp(0.65*fwidth(heart),0.001,0.016);
    float core = 1.0-smoothstep(-coreAA,coreAA,heart);
    vec3 blue = mix(deepBlue,paleBlue,smoothstep(0.08,0.75,p.y+0.14*fold)*(0.73+0.27*light));
    vec3 color = mix(mix(paleBlue,vec3(1.0),light),mix(blue,navy,core),fill);
    float distanceToRim = max(edge-line,0.0)/(0.025+aa);
    float halo = (0.12+0.20*light)*exp(-distanceToRim*distanceToRim)*(1.0-outline);
    outColor = vec4(color*outline+vec3(halo),outline+halo)*fire.z;
}
