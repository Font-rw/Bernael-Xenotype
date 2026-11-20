#version 450
layout(location = 0) in vec2 vUv;
layout(location = 0) out vec4 outColor;
#include "BX_ShaderCommon.glsl"
#include "BX_ShadowMirePlume.glsl"

layout(set = 2, binding = 0) uniform MaterialUniforms {
    float _Phase;   // @default = 0.0
    float _Seed;    // @default = 0.0
    float _Opacity; // @default = 1.0
    float _PillarScale; // @default = 2.4
    vec4 _VentA;    // @default = vec4(0,0,-1,0)
    vec4 _VentB;    // @default = vec4(0,0,-1,0)
    vec4 _GroundScale; // @default = vec4(1,1,0,0)
} m;
layout(set = 2, binding = 1) uniform sampler2D _FlameA;
layout(set = 2, binding = 2) uniform sampler2D _FlameB;
layout(set = 2, binding = 3) uniform sampler2D _Shore;

const vec3 cobalt = vec3(0.105, 0.205, 0.340);
const vec3 ice = vec3(0.56, 0.76, 0.91);

// The pale outer meniscus marks the footprint. A smaller ripple contracts to the vent before it opens.
vec4 boil(vec2 p, float t, float aa, float widthScale, float sheen, sampler2D profiles)
{
    if (t < 0.0 || t >= 2.65 || dot(p,p)>1.30) return vec4(0.0);
    float a;
    if (t < 1.0) {
        float r = length(p);
        float wave = r + 0.055*(bxValueNoise(p*4.0 + vec2(m._Seed,t))-0.5);
        float glint = 0.56 + 0.44*bxValueNoise(p*4.0+m._Seed);
        float outer = 1.0-smoothstep(0.012,0.012+aa,abs(wave-(0.81+0.08*t)));
        float inner = 1.0-smoothstep(0.015,0.015+aa,abs(wave-mix(0.68,0.10,t)));
        a = (0.48*outer+0.88*inner)*glint + 0.43*t*(1.0-smoothstep(0.0,0.48,r));
    } else {
        if (widthScale <= 0.0 || p.y >= 0.08 || p.y <= -0.68) return vec4(0.0);
        float down = max(-p.y,0.0);
        float ripple = bxValueNoise(vec2(3.2*p.x,16.0*down-4.0*m._Phase)+m._Seed);
        float drift = (ripple-0.5)*(0.020+0.17*down);
        vec3 flame = bxPlumeFields(profiles,vec2(p.x*widthScale+drift,0.045+1.5*down));
        // The blue sides catch the oil's crests. Very little light comes from the navy heart.
        float rimLight = mix(0.18,1.0,smoothstep(-0.08,0.025,flame.y));
        float gleam = (1.0-smoothstep(-0.012,0.085,flame.x))*flame.z*rimLight;
        float glint = smoothstep(0.26,0.77,ripple+0.26*(sheen-0.5));
        float surface = (1.0-smoothstep(-0.028,0.08,p.y))*(1.0-smoothstep(0.02,0.68,down));
        a = gleam*surface*(0.07+0.39*glint);
    }
    a = clamp(a,0.0,1.0);
    return vec4(mix(cobalt,ice,0.59+0.35*min(t,1.0))*a,a);
}

vec4 oil(vec2 p, float bank)
{
    float t = m._Phase;
    vec2 offset = vec2(m._Seed*0.17, m._Seed*0.23);
    float swell = bxValueNoise(3.5*p+offset+vec2(t*0.12,0.0));
    float edge = length(p) + 0.075*(swell-0.5) + 0.026*(bxValueNoise(p*10.0+offset)-0.5);
    float shore = mix(0.20,0.995,smoothstep(0.0,0.27,t));
    if (t>1.0) shore *= mix(0.69,1.0,m._Opacity);
    // Both distances are measured in feather widths. Round the junction where the outer shore meets cover.
    float rimDepth = (shore-edge)/0.044;
    float bankDepth = (bank+0.12*(swell-0.5))/0.30;
    float overlap = max(1.8-abs(rimDepth-bankDepth),0.0);
    float depth = min(rimDepth,bankDepth)-overlap*overlap/(4.0*1.8);
    float coverage = smoothstep(-0.18,1.0,depth);
    float aa = max(0.009,fwidth(p.x)*m._PillarScale);
    if (coverage <= 0.0) return vec4(0.0);
    vec2 flow = p + vec2(0.30*swell,0.24*bxValueNoise(p*2.7+offset.yx-t*0.08));
    float film = bxValueNoise(flow*3.8+offset+vec2(0.0,t*0.10));
    float fold = 12.0*film + 4.5*dot(flow,vec2(0.38,0.92)) + swell;
    float highlight = smoothstep(0.94,0.998,sin(fold))*smoothstep(0.42,0.82,swell);
    float glancing = 0.5+0.5*sin(fold+0.85);
    vec3 rgb = mix(vec3(0.005,0.009,0.018),vec3(0.033,0.056,0.103),
                   0.12+0.40*film+0.14*glancing*glancing);
    rgb += mix(vec3(0.051,0.051,0.087),vec3(0.069,0.105,0.153),film)*highlight;
    float lip = 1.0-smoothstep(0.0,0.43,abs(depth-0.98));
    rgb += vec3(0.082,0.121,0.17)*lip*smoothstep(0.32,0.76,film);
    float sheen = clamp(0.62*glancing+0.38*film+0.55*highlight,0.0,1.0);
    vec4 a = boil((p-m._VentA.xy)*m._PillarScale,m._VentA.z,aa,m._VentA.w,sheen,_FlameA);
    vec4 b = boil((p-m._VentB.xy)*m._PillarScale,m._VentB.z,aa,m._VentB.w,sheen,_FlameB);
    float alpha = 0.964+0.029*film;
    vec4 result = vec4(rgb*alpha,alpha);
    result = a+result*(1.0-a.a);
    result = b+result*(1.0-b.a);
    return result*(coverage*m._Opacity);
}

void main()
{
    vec2 maskUv = 0.5+(vUv-0.5)*m._GroundScale.y;
    float bank = 2.0*tex2D(_Shore,maskUv).a-1.0;
    outColor = oil((2.0*vUv-1.0)*m._GroundScale.x,bank);
}
