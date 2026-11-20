using System;
using UnityEngine;
using Verse;

namespace Bernael_Xenotype
{
    // Each row of the profile texture is a tongue of fire. Along the row, texels describe
    // its horizontal center, outer radius, dark core radius and light at successive heights.
    internal sealed class ShadowMirePlume
    {
        internal const int BurnTicks = 40;
        internal const float MaxHeight = 7.5f;
        internal const float CanvasWidth = 2.6f;
        internal const float Bottom = -0.10f, Top = 1.14f;
        private const int Samples = 112, FuelCount = 10, PulseCount = 15;
        private const float Band = 0.12f, RadiusRange = 0.50f;

        internal float Height { get; private set; }
        internal float Width { get; private set; }
        internal int Seed { get; private set; }
        internal Texture2D Profile { get; private set; }
        internal Texture2D Painted { get; private set; }

        private struct Pulse
        {
            public float birth, life, reach, girth, side, bend, phase, feedEnd, neck, pinch;
        }

        private readonly Pulse[] pulses = new Pulse[PulseCount];
        private readonly Color32[] rows = new Color32[Samples * PulseCount];
        private readonly float[] spine = new float[Samples];
        private Color32[] paintedPixels;
        private float lean, curl, frequency, spread, forkHeight, windPhase, windSpeed;
        private int lastTick = -1;

        internal static int SeedFor(int id, int wave, IntVec3 cell, bool answering)
        {
            int key = Gen.HashCombineInt(Gen.HashCombineInt(id, wave), cell.x);
            return Gen.HashCombineInt(Gen.HashCombineInt(key, cell.z), answering ? 1 : 0);
        }

        private static float Unit(int seed, int salt)
        {
            unchecked
            {
                uint h = (uint)seed * 0x8da6b343u ^ (uint)salt * 0xd8163841u;
                h = (h ^ (h >> 16)) * 0x7feb352du;
                h = (h ^ (h >> 15)) * 0x846ca68bu;
                return ((h ^ (h >> 16)) >> 8) * (1f / 16777216f);
            }
        }

        private static float Smooth(float lo, float hi, float x)
        {
            float t = Mathf.Clamp01((x - lo) / (hi - lo));
            return t * t * (3f - 2f * t);
        }

        private void Configure(int seed, float radius)
        {
            Seed = seed;
            lastTick = -1;
            Height = Mathf.Lerp(4.9f, MaxHeight, Unit(seed, 0));
            Width = (2f * radius + 0.52f) * Mathf.Lerp(0.77f, 1.25f, Unit(seed, 1));
            lean = Mathf.Lerp(-0.32f, 0.32f, Unit(seed, 2));
            curl = Mathf.Lerp(-0.23f, 0.23f, Unit(seed, 3));
            frequency = Mathf.Lerp(4.3f, 10.5f, Unit(seed, 4));
            spread = Mathf.Lerp(0.12f, 0.55f, Unit(seed, 5));
            forkHeight = Mathf.Lerp(0.05f, 0.37f, Unit(seed, 6));
            windPhase = 6.2832f * Unit(seed, 7);
            windSpeed = Mathf.Lerp(0.10f, 0.24f, Unit(seed, 8));
            float cadence = Mathf.Lerp(2.35f, 3.05f, Unit(seed, 9));
            for (int i = 0; i < pulses.Length; i++)
            {
                int k = 16 + 12 * i;
                float birth = (i - 0.70f + 0.62f * (Unit(seed, k) - 0.5f)) * cadence;
                if (i >= FuelCount)
                    birth = 3f + (i - FuelCount + Unit(seed, k)) * 4.7f;
                pulses[i] = new Pulse {
                    birth = birth,
                    life = Mathf.Min(BurnTicks - birth, Mathf.Lerp(15f, 27f, Unit(seed, k + 1))),
                    reach = Mathf.Lerp(0.45f, 1.01f, Unit(seed, k + 2)),
                    girth = Mathf.Lerp(0.10f, 0.225f, Unit(seed, k + 3)),
                    side = 2f * Unit(seed, k + 4) - 1f,
                    bend = Mathf.Lerp(-0.25f, 0.25f, Unit(seed, k + 5)),
                    phase = 6.2832f * Unit(seed, k + 6),
                    feedEnd = Mathf.Lerp(0.30f, 0.76f, Unit(seed, k + 7)),
                    neck = Mathf.Lerp(0.41f, 0.77f, Unit(seed, k + 8)),
                    pinch = Unit(seed, k + 9)
                };
            }
        }

        private float Updraft(float y, float tick)
        {
            float phase = windPhase - windSpeed * tick;
            return lean * y + y * (curl * Mathf.Sin(frequency * y + phase)
                + 0.065f * Mathf.Sin(1.76f * frequency * y + 1.23f * phase));
        }

        private float Branch(Pulse p, float y, float tick)
        {
            float lift = Smooth(forkHeight, forkHeight + 0.39f, y);
            return 0.085f * p.side + lift * (spread * p.side
                + p.bend * Mathf.Sin(frequency * y - windSpeed * tick + p.phase));
        }

        internal void Prepare(int seed, int tick, float radius, bool paint)
        {
            bool newPaint = paint && Painted == null;
            if (newPaint)
            {
                Painted = Texture(96, Samples, "Shadow mire flame", false);
                paintedPixels = new Color32[Painted.width * Samples];
            }
            if (Profile == null)
            {
                Profile = Texture(Samples, PulseCount, "Shadow mire flame profiles", true);
                Configure(seed, radius);
            }
            else if (seed != Seed) Configure(seed, radius);
            if (tick == lastTick)
            {
                if (newPaint) PaintFallback();
                return;
            }
            lastTick = tick;
            Array.Clear(rows);
            for (int j = 0; j < Samples; j++)
            {
                float y = Mathf.Lerp(Bottom, Top, j / (Samples - 1f));
                spine[j] = Updraft(y, tick);
            }
            for (int i = 0; i < pulses.Length; i++)
            {
                Pulse pulse = pulses[i];
                float age = (tick - pulse.birth) / pulse.life;
                if (age <= 0f || age >= 1f) continue;
                if (i < FuelCount) WriteTongue(i, pulse, tick, age);
                else WriteCinder(i, pulse, age);
            }
            Profile.SetPixels32(rows);
            Profile.Apply(false, false);
            if (paint) PaintFallback();
        }

        private void WriteTongue(int i, Pulse p, int tick, float age)
        {
            float supply = 1f - Smooth(14f, 30f, p.birth);
            float reach = p.reach * Mathf.Lerp(0.65f, 1f, supply);
            float release = Mathf.Clamp01((age - p.feedEnd) / (1f - p.feedEnd));
            float tip = 0.06f + reach * (1f - (1f - age) * (1f - age));
            float tail = 0.012f + reach * release * (0.27f + 0.66f * release);
            // A feeding tongue extends below the oil. Once it tears free its whole tail rises.
            float lowerEnd = tail - 0.075f * (1f - Smooth(0f, 0.25f, release));
            float oilLine = -0.025f + 0.015f * p.side + 0.008f * Mathf.Sin(p.phase - windSpeed * tick);
            float ignition = Smooth(0f, 0.14f, age);
            float width = p.girth * Mathf.Lerp(0.50f, 1f, supply) * ignition
                * (1f - 0.82f * Smooth(0.53f, 1f, age));
            float tear = p.pinch * Smooth(0.33f, 0.80f, age);
            float ink = (0.46f + 0.26f * supply) * (1f - Smooth(0.01f, 0.82f, release));
            float light = ignition * (1f - Smooth(0.68f, 1f, age));
            float aspect = Height / Width;
            for (int j = 0; j < Samples; j++)
            {
                float y = Mathf.Lerp(Bottom, Top, j / (Samples - 1f));
                float beyond = Mathf.Max(lowerEnd - y, 0f) * (2.4f * aspect) + Mathf.Max(y - tip, 0f) * aspect;
                if (beyond >= Band) continue;
                float s = Mathf.Clamp01((y - tail) / (tip - tail));
                float neck = (s - p.neck) / 0.13f;
                float head = (s - 0.79f) / 0.15f;
                float radius = width * ((1f - s) * (0.67f - 0.59f * release + 2.75f * s * (1f - 0.53f * s))
                    + 0.35f * tear * Mathf.Exp(-head * head) - 1.52f * tear * Mathf.Exp(-neck * neck));
                float exposed = Smooth(oilLine, oilLine + 0.096f, y);
                float submerged = 1f - exposed;
                float x = spine[j] + Branch(p, y, tick) - 0.045f * p.side * submerged;
                radius *= 1f - 0.24f * submerged;
                Store(i, j, x, radius - beyond, radius * ink * (1f - Smooth(0.50f, 0.90f, s)) - 0.016f - beyond, light * exposed);
            }
        }

        private void WriteCinder(int i, Pulse p, float age)
        {
            Pulse parent = pulses[i - FuelCount];
            float parentAge = Mathf.Clamp01((p.birth - parent.birth) / parent.life);
            float launchHeight = 0.05f + parent.reach * parentAge * (2f - parentAge) * (0.47f + 0.34f * p.pinch);
            float start = Updraft(launchHeight, p.birth) + Branch(parent, launchHeight, p.birth);
            float tip = launchHeight + (0.12f + 0.28f * p.pinch) * age;
            float length = (0.033f + 0.065f * p.girth / 0.21f) * (1f - 0.45f * age);
            float light = Smooth(0f, 0.18f, age) * (1f - Smooth(0.5f, 1f, age));
            for (int j = 0; j < Samples; j++)
            {
                float y = Mathf.Lerp(Bottom, Top, j / (Samples - 1f));
                float s = (y - tip + length) / length;
                float along = Mathf.Clamp01(s);
                float radius = 0.048f * light * Mathf.Sqrt(4f * along * (1f - along)) * (1f - 0.66f * along)
                    - Mathf.Abs(s - along) * length * Height / Width;
                if (radius <= -Band) continue;
                float x = start + age * (0.15f * p.side + 0.09f * Mathf.Sin(p.phase + 3f * age))
                    + p.side * (0.055f * (along - 0.5f) + 0.029f * Mathf.Sin(3.1416f * along));
                Store(i, j, x, radius, -Band, light);
            }
        }

        private static byte Byte(float value) => (byte)Mathf.RoundToInt(255f * Mathf.Clamp01(value));

        private void Store(int stream, int sample, float x, float radius, float core, float light)
        {
            x /= 1f + 0.18f * Mathf.Abs(x);
            rows[stream * Samples + sample] = new Color32(Byte(0.5f + x / CanvasWidth),
                Byte((radius + Band) / RadiusRange), Byte((core + Band) / RadiusRange), Byte(light));
        }

        private static Texture2D Texture(int width, int height, string name, bool linear) =>
            new Texture2D(width, height, TextureFormat.RGBA32, false, linear) {
                name = name, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp
            };

        // Render the same sections into a color texture when the pillar shader is unavailable.
        private void PaintFallback()
        {
            int width = Painted.width;
            for (int y = 0; y < Samples; y++)
            for (int x = 0; x < width; x++)
            {
                float u = (x + 0.5f) / width;
                float edge = Band, core = Band, light = 0f;
                for (int i = 0; i < PulseCount; i++)
                {
                    Color32 row = rows[i * Samples + y];
                    if (row.a == 0) continue;
                    float dx = Mathf.Abs(u - row.r / 255f) * CanvasWidth;
                    float d = dx + Band - RadiusRange * row.g / 255f;
                    edge = Mathf.Min(edge, d);
                    core = Mathf.Min(core, dx + Band - RadiusRange * row.b / 255f);
                    light = Mathf.Max(light, (1f - Smooth(0.02f, 0.09f, d)) * row.a / 255f);
                }
                Color color = Color.Lerp(Color.white, new Color(0.62f, 0.79f, 0.90f), 1f - Smooth(-0.007f, 0.007f, edge));
                color = Color.Lerp(color, new Color(0.086f, 0.133f, 0.169f), 1f - Smooth(-0.006f, 0.006f, core));
                color.a = light * (1f - Smooth(0.008f, 0.040f, edge));
                paintedPixels[y * width + x] = color;
            }
            Painted.SetPixels32(paintedPixels);
            Painted.Apply(false, false);
        }

        internal void Dispose()
        {
            if (Profile != null) UnityEngine.Object.Destroy(Profile);
            if (Painted != null) UnityEngine.Object.Destroy(Painted);
            Profile = Painted = null;
        }
    }
}
