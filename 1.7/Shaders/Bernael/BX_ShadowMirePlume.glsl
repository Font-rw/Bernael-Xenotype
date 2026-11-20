// Flame profiles are stored across the texture width, one tongue per row.
// The same sections paint the rising plume and its shorter, broken reflection.
vec3 bxPlumeFields(sampler2D profiles, vec2 p)
{
    ivec2 size = textureSize(profiles, 0);
    float h = clamp((p.y + 0.10) / 1.24, 0.0, 1.0);
    float u = (0.5 + h * float(size.x - 1)) / float(size.x);
    vec3 field = vec3(0.12, 0.12, 0.0);
    for (int i = 0; i < size.y; i++) {
        vec4 row = textureLod(profiles, vec2(u, (float(i) + 0.5) / float(size.y)), 0.0);
        float dx = abs(p.x - 2.6 * (row.r - 0.5));
        vec2 d = dx + 0.12 - 0.50 * row.gb;
        field.xy = min(field.xy, d);
        field.z = max(field.z, row.a * (1.0 - smoothstep(0.018, 0.092, d.x)));
    }
    return field;
}
