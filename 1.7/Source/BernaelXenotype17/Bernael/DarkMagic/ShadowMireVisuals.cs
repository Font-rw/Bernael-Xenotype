using UnityEngine;
using UnityEngine.Rendering;
using Verse;

namespace Bernael_Xenotype
{
    [StaticConstructorOnStartup]
    internal sealed class ShadowMireVisuals
    {
        internal const float MaxPillarHeight = ShadowMirePlume.MaxHeight * ShadowMirePlume.Top;
        private static readonly Material mireMaterial;
        private static readonly Material fallbackOil;
        private static readonly Material pillarMaterial;
        private static readonly bool paintPillar;
        private readonly ShadowMirePlume lead = new ShadowMirePlume();
        private readonly ShadowMirePlume answer = new ShadowMirePlume();
        private readonly MaterialPropertyBlock properties = new MaterialPropertyBlock();
        private readonly ShadowMireShore shore = new ShadowMireShore();

        static ShadowMireVisuals()
        {
            Shader oil = DefDatabase<ShaderDef>.GetNamed("BX_ShadowMire").Shader;
            if (oil != null && oil.isSupported)
                mireMaterial = new Material(oil) { name = "Shadow mire" };
            else
                fallbackOil = new Material(ShaderDatabase.Transparent) { name = "Shadow mire oil" };

            Shader pillar = DefDatabase<ShaderDef>.GetNamed("BX_ShadowMirePillar").Shader;
            paintPillar = pillar == null || !pillar.isSupported;
            pillarMaterial = new Material(paintPillar ? ShaderDatabase.Transparent : pillar) { name = "Shadow mire pillar" };
        }

        public void SetGround(MireGround[] ground, int width, float radius) =>
            shore.Rebuild(ground, width, radius, mireMaterial == null);

        public void Draw(Vector3 at, IntVec3 center, ShadowMireDef settings, int age, int id,
            IntVec3 first, IntVec3 second)
        {
            if (shore.Width == 0) return;
            int wave = age / ShadowMire.WaveTicks;
            int phase = age % ShadowMire.WaveTicks;
            int secondPhase = phase - ShadowMire.SecondPillarDelay;
            bool firstLit = Prepare(lead, id, wave, first, false, phase, settings.pillarRadius);
            bool secondLit = Prepare(answer, id, wave, second, true, secondPhase, settings.pillarRadius);
            properties.Clear();
            float groundSize = shore.Width;
            if (mireMaterial != null)
            {
                // Leave room for the outermost swell without drawing the sampling margin.
                groundSize = Mathf.Min(groundSize, 2.12f * settings.radius);
                float opacity = Mathf.Min(Mathf.Clamp01((age + 2) / 14f), Mathf.Clamp01((settings.durationTicks - age) / 28f));
                properties.SetFloat("_Phase", age / 60f);
                properties.SetFloat("_Seed", id % 997);
                properties.SetFloat("_Opacity", opacity);
                properties.SetFloat("_PillarScale", settings.radius / (settings.pillarRadius + 0.18f));
                properties.SetVector("_GroundScale", new Vector4(groundSize / (2f * settings.radius), groundSize / shore.Width, 0f, 0f));
                properties.SetTexture("_Shore", shore.Texture);
                properties.SetVector("_VentA", Vent(first, center, settings, phase, firstLit ? lead.Width : 0f));
                properties.SetVector("_VentB", Vent(second, center, settings, secondPhase, secondLit ? answer.Width : 0f));
                properties.SetTexture("_FlameA", firstLit ? lead.Profile : Texture2D.blackTexture);
                properties.SetTexture("_FlameB", secondLit ? answer.Profile : Texture2D.blackTexture);
            }
            else
                properties.SetTexture("_MainTex", shore.Texture);
            Draw(MeshPool.plane10, mireMaterial ?? fallbackOil, at, new Vector3(groundSize, 1f, groundSize));
            DrawPillar(first, phase, settings.pillarRadius, lead, firstLit);
            DrawPillar(second, secondPhase, settings.pillarRadius, answer, secondLit);
        }

        private static bool Prepare(ShadowMirePlume plume, int id, int wave, IntVec3 cell, bool answering,
            int phase, float radius)
        {
            int burning = phase - ShadowMire.WarningTicks;
            if (!cell.IsValid || burning < 0 || burning >= ShadowMirePlume.BurnTicks) return false;
            plume.Prepare(ShadowMirePlume.SeedFor(id, wave, cell, answering), burning, radius, paintPillar);
            return true;
        }

        private static Vector4 Vent(IntVec3 cell, IntVec3 center, ShadowMireDef settings, int phase, float width)
        {
            if (!cell.IsValid) return new Vector4(0, 0, -1, 0);
            return new Vector4((cell.x - center.x) / settings.radius, (cell.z - center.z) / settings.radius,
                phase / (float)ShadowMire.WarningTicks, width > 0f ? (settings.pillarRadius + 0.18f) / width : 0f);
        }

        private void DrawPillar(IntVec3 cell, int phase, float radius, ShadowMirePlume plume, bool lit)
        {
            if (!lit)
            {
                if (mireMaterial == null && cell.IsValid && phase >= 0 && phase < ShadowMire.WarningTicks)
                    GenDraw.DrawRadiusRing(cell, radius);
                return;
            }
            Vector3 at = cell.ToVector3ShiftedWithAltitude(AltitudeLayer.MoteOverheadLow);
            at.z += plume.Height * (ShadowMirePlume.Top + ShadowMirePlume.Bottom) * 0.5f;
            properties.Clear();
            properties.SetTexture("_MainTex", paintPillar ? plume.Painted : plume.Profile);
            if (!paintPillar)
            {
                properties.SetFloat("_Phase", (phase - ShadowMire.WarningTicks) / 60f);
                properties.SetFloat("_Seed", plume.Seed & 65535);
            }
            Draw(MeshPool.plane10, pillarMaterial, at, new Vector3(plume.Width * ShadowMirePlume.CanvasWidth, 1f,
                plume.Height * (ShadowMirePlume.Top - ShadowMirePlume.Bottom)));
        }

        private void Draw(Mesh mesh, Material material, Vector3 at, Vector3 size) =>
            Graphics.DrawMesh(mesh, Matrix4x4.TRS(at, Quaternion.identity, size), material, 0, null, 0,
                properties, ShadowCastingMode.Off, false);

        public void Dispose()
        {
            shore.Dispose();
            lead.Dispose();
            answer.Dispose();
        }
    }
}
