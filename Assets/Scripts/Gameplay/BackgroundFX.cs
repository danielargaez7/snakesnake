using UnityEngine;

namespace BellyFull
{
    /// <summary>
    /// Adds visual depth to the static background:
    ///   • Three layers of floating sparkle/petal particles at different speeds/sizes
    ///     — slow+small = far away, fast+large = close up
    ///   • A soft radial vignette darkens the edges so the play field pops forward
    /// </summary>
    public class BackgroundFX : MonoBehaviour
    {
        [Header("Field size (should match FieldManager bounds)")]
        [SerializeField] private float fieldW = 18f;
        [SerializeField] private float fieldH = 11f;

        [Header("Sparkle layers")]
        [SerializeField] private int particlesPerLayer = 20;
        [SerializeField] private Color nearColor  = new Color(1.00f, 0.97f, 0.70f, 0.80f); // warm gold
        [SerializeField] private Color farColor   = new Color(1.00f, 1.00f, 1.00f, 0.35f); // cool white

        [Header("Vignette")]
        [SerializeField] private float vignetteAlpha = 0.52f;  // 0 = off, 1 = heavy

        private void Start()
        {
            BuildVignette();
            BuildParticleLayers();
        }

        // ── Vignette ─────────────────────────────────────────────────────────

        private void BuildVignette()
        {
            const int size = 256;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var center = new Vector2(size * 0.5f, size * 0.5f);
            float radius = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center) / radius;
                    float a    = Mathf.Clamp01(Mathf.Pow(dist, 1.8f)) * vignetteAlpha;
                    tex.SetPixel(x, y, new Color(0f, 0f, 0f, a));
                }
            }
            tex.Apply();

            // Fit to the camera view (ortho size 5 → height 10, assume 16:9)
            var sprite = Sprite.Create(tex,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                size / fieldH);   // pixels-per-unit so height = fieldH

            var go = new GameObject("Vignette", typeof(SpriteRenderer));
            go.transform.SetParent(transform.parent);
            go.transform.position = new Vector3(0f, 0f, 0.5f); // just in front of bg sprite (z=1)

            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite       = sprite;
            sr.sortingOrder = -9;   // behind game objects, in front of bg
            // Stretch width to cover field
            float aspect = fieldW / fieldH;
            go.transform.localScale = new Vector3(aspect, 1f, 1f);
        }

        // ── Particle layers ───────────────────────────────────────────────────

        private void BuildParticleLayers()
        {
            // layer 0 = far  (small, slow, cool white)
            // layer 1 = mid
            // layer 2 = near (large, fast, warm gold)
            float[] zPos    = {  0.8f,  0.4f,  0.0f };
            float[] minSize = { 0.03f, 0.055f, 0.09f };
            float[] maxSize = { 0.06f, 0.09f,  0.15f };
            float[] minSpd  = { 0.10f, 0.20f,  0.35f };
            float[] maxSpd  = { 0.20f, 0.35f,  0.55f };
            float[] rateOvT = { 2.5f,  3.5f,   2.0f  };

            for (int i = 0; i < 3; i++)
            {
                float t = i / 2f; // 0..1
                Color col = Color.Lerp(farColor, nearColor, t);

                var go = new GameObject($"SparkleLayer_{i}");
                go.transform.SetParent(transform.parent);
                go.transform.position = new Vector3(0f, 0f, zPos[i]);

                var ps   = go.AddComponent<ParticleSystem>();
                var rend = go.GetComponent<ParticleSystemRenderer>();

                // ── main ──────────────────────────────────────────────────────
                var main = ps.main;
                main.loop             = true;
                main.startLifetime    = new ParticleSystem.MinMaxCurve(5f, 10f);
                main.startSpeed       = new ParticleSystem.MinMaxCurve(minSpd[i], maxSpd[i]);
                main.startSize        = new ParticleSystem.MinMaxCurve(minSize[i], maxSize[i]);
                main.startColor       = new ParticleSystem.MinMaxGradient(
                    new Color(col.r, col.g, col.b, col.a * 0.4f),
                    new Color(col.r, col.g, col.b, col.a));
                main.startRotation    = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
                main.maxParticles     = particlesPerLayer;
                main.simulationSpace  = ParticleSystemSimulationSpace.World;
                main.gravityModifier  = 0f;

                // ── emission ──────────────────────────────────────────────────
                var em = ps.emission;
                em.rateOverTime = rateOvT[i];

                // ── shape — spawn across the whole field ──────────────────────
                var sh = ps.shape;
                sh.enabled    = true;
                sh.shapeType  = ParticleSystemShapeType.Rectangle;
                sh.scale      = new Vector3(fieldW, fieldH, 1f);

                // ── velocity — gentle upward drift + sway ─────────────────────
                var vel = ps.velocityOverLifetime;
                vel.enabled = true;
                vel.space   = ParticleSystemSimulationSpace.World;
                // All three axes must use the same curve mode (TwoConstants)
                vel.x = new ParticleSystem.MinMaxCurve(-0.08f, 0.08f);
                vel.y = new ParticleSystem.MinMaxCurve(0.05f, 0.18f);  // float up
                vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);

                // ── size over lifetime — fade in tiny, pop, fade out ──────────
                var sizeOL = ps.sizeOverLifetime;
                sizeOL.enabled = true;
                var sizeCurve  = new AnimationCurve(
                    new Keyframe(0f,    0f),
                    new Keyframe(0.15f, 1f),
                    new Keyframe(0.85f, 1f),
                    new Keyframe(1f,    0f));
                sizeOL.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

                // ── colour over lifetime — fade out at end ────────────────────
                var colOL = ps.colorOverLifetime;
                colOL.enabled = true;
                var grad = new Gradient();
                grad.SetKeys(
                    new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                    new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.1f),
                            new GradientAlphaKey(1f, 0.85f), new GradientAlphaKey(0f, 1f) });
                colOL.color = new ParticleSystem.MinMaxGradient(grad);

                // ── renderer — render just behind game objects ────────────────
                rend.sortingOrder      = -8 + i;   // -8, -7, -6  (all above bg at -10)
                rend.renderMode        = ParticleSystemRenderMode.Billboard;
                rend.minParticleSize   = 0.001f;
                rend.maxParticleSize   = 0.5f;
            }
        }
    }
}
