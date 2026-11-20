using System;
using UnityEngine;

namespace Bernael_Xenotype
{
    internal enum MireGround : byte { Unsampled, Blocked, Sheltered, Wet }

    internal sealed class ShadowMireShore
    {
        private const int SamplesPerCell = 8;
        private static readonly int[] kernel = { 1, 4, 6, 4, 1 };
        private float[] depths;
        private float[] row;
        private Color32[] pixels;
        private Texture2D texture;
        private bool open;
        public int Width { get; private set; }
        public Texture2D Texture => open ? Texture2D.whiteTexture : texture;

        public void Rebuild(MireGround[] ground, int width, float radius, bool paint)
        {
            Width = width;
            open = !paint && Array.IndexOf(ground, MireGround.Sheltered) < 0 && Array.IndexOf(ground, MireGround.Blocked) < 0;
            if (open) return;

            int size = width * SamplesPerCell;
            if (texture == null || texture.width != size)
            {
                Dispose();
                texture = new Texture2D(size, size, TextureFormat.RGBA32, false, !paint) {
                    name = "Shadow mire shoreline", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp
                };
                depths = new float[ground.Length];
                row = new float[width];
                pixels = new Color32[size * size];
            }

            FitBanks(ground, width);
            for (int y = 0; y < size; y++)
            {
                float z = (y + 0.5f) / SamplesPerCell - 0.5f;
                for (int x = 0; x < width; x++)
                    row[x] = Sample(depths, x, width, width, z);
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / SamplesPerCell - 0.5f;
                    pixels[y * size + x].a = Encode(Sample(row, 0, 1, width, u));
                }
            }
            for (int i = 0; i < ground.Length; i++)
                if (ground[i] == MireGround.Blocked) ClipToFace(i % width, i / width, size);
            if (paint) PaintFallback(size, radius);
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
        }

        // Fit the open ground on both sides of the shadow. The solid faces are clipped separately.
        private void FitBanks(MireGround[] ground, int width)
        {
            for (int z = 0; z < width; z++)
            for (int x = 0; x < width; x++)
            {
                int total = 0, wet = 0;
                for (int dz = -2; dz <= 2; dz++)
                for (int dx = -2; dx <= 2; dx++)
                {
                    int sx = x + dx, sz = z + dz;
                    if ((uint)sx >= width || (uint)sz >= width) continue;
                    MireGround cell = ground[sz * width + sx];
                    if (cell != MireGround.Wet && cell != MireGround.Sheltered) continue;
                    int weight = kernel[dz + 2] * kernel[dx + 2];
                    total += weight;
                    if (cell == MireGround.Wet) wet += weight;
                }
                float depth = total == 0 ? 1f : (wet / (float)total - 0.5f) * 2.6f;
                int i = z * width + x;
                if (ground[i] == MireGround.Wet) depth = Mathf.Max(0.24f, depth);
                else if (ground[i] == MireGround.Sheltered) depth = Mathf.Min(-0.24f, depth);
                depths[i] = depth;
            }
        }

        // Signed distance to the solid square gives the face and its two exposed corners one continuous lip.
        private void ClipToFace(int cellX, int cellZ, int size)
        {
            int xMin = Mathf.Max(0, (cellX - 1) * SamplesPerCell), xMax = Mathf.Min(size, (cellX + 2) * SamplesPerCell);
            int zMin = Mathf.Max(0, (cellZ - 1) * SamplesPerCell), zMax = Mathf.Min(size, (cellZ + 2) * SamplesPerCell);
            for (int y = zMin; y < zMax; y++)
            {
                float dz = Mathf.Abs((y + 0.5f) / SamplesPerCell - cellZ - 0.5f) - 0.5f;
                for (int x = xMin; x < xMax; x++)
                {
                    float dx = Mathf.Abs((x + 0.5f) / SamplesPerCell - cellX - 0.5f) - 0.5f;
                    float ex = Mathf.Max(dx, 0f), ez = Mathf.Max(dz, 0f);
                    float distance = Mathf.Sqrt(ex * ex + ez * ez) + Mathf.Min(Mathf.Max(dx, dz), 0f);
                    int index = y * size + x;
                    pixels[index].a = Math.Min(pixels[index].a, Encode(2.4f * (distance - 0.015f)));
                }
            }
        }

        private static byte Encode(float depth) => (byte)Mathf.RoundToInt(255f * Mathf.Clamp01(0.5f + 0.5f * depth));

        private void PaintFallback(int size, float radius)
        {
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f - size * 0.5f) / SamplesPerCell, dz = (y + 0.5f - size * 0.5f) / SamplesPerCell;
                int index = y * size + x;
                float inside = Mathf.Min(pixels[index].a / 127.5f - 1f, radius - Mathf.Sqrt(dx * dx + dz * dz));
                float alpha = Mathf.SmoothStep(0f, 0.97f, Mathf.InverseLerp(-0.04f, 0.25f, inside));
                pixels[index] = new Color32(4, 7, 13, (byte)Mathf.RoundToInt(alpha * 255f));
            }
        }

        // Catmull-Rom passes through the cell centers and gives neighboring stretches a shared tangent.
        private static float Sample(float[] values, int start, int stride, int count, float at)
        {
            int i = Mathf.FloorToInt(at);
            float t = at - i;
            float a = values[start + Mathf.Max(0, i - 1) * stride];
            float b = values[start + Mathf.Clamp(i, 0, count - 1) * stride];
            float c = values[start + Mathf.Min(i + 1, count - 1) * stride];
            float d = values[start + Mathf.Min(i + 2, count - 1) * stride];
            return b + 0.5f * t * (c - a + t * (2f * a - 5f * b + 4f * c - d + t * (3f * (b - c) + d - a)));
        }

        public void Dispose()
        {
            if (texture != null) UnityEngine.Object.Destroy(texture);
            texture = null;
        }
    }
}
