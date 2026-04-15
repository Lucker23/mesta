using UnityEngine;

namespace LastEmberKnight
{
    /// <summary>
    /// Creates platform GameObjects procedurally with zone-appropriate visuals.
    /// Supports standard, one-way, and moving platform variants.
    /// </summary>
    public class PlatformBuilder : MonoBehaviour
    {
        // ── Singleton / Instance ──────────────────────────────────────────────
        public static PlatformBuilder Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("Texture Resolution")]
        [SerializeField] private int texWidth  = 64;
        [SerializeField] private int texHeight = 16;

        [Header("Layer Names")]
        [SerializeField] private string groundLayerName = "Ground";

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // =========================================================================
        //  Public API
        // =========================================================================

        /// <summary>
        /// Creates a platform at <paramref name="pos"/> with the given <paramref name="size"/>
        /// and zone-specific visuals.
        /// </summary>
        /// <param name="pos">World-space centre position.</param>
        /// <param name="size">Width × height in world units.</param>
        /// <param name="zone">Zone used to choose the visual style.</param>
        /// <param name="moving">Attach a <see cref="MovingPlatform"/> component if true.</param>
        /// <param name="oneWay">Use a PlatformEffector2D for pass-through behaviour if true.</param>
        /// <returns>The root platform GameObject.</returns>
        public GameObject CreatePlatform(Vector2 pos, Vector2 size, ZoneType zone,
                                         bool moving = false, bool oneWay = false)
        {
            GameObject go = new GameObject($"Platform_{zone}_{pos.x:F0}_{pos.y:F0}");
            go.transform.position = new Vector3(pos.x, pos.y, 0f);

            // --- SpriteRenderer -------------------------------------------------
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            Texture2D tex = GeneratePlatformTexture(zone, (int)(size.x * texWidth * 0.25f + texWidth), texHeight);
            sr.sprite = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                tex.width / size.x);   // pixels-per-unit so sprite matches world size
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = size;
            sr.sortingLayerName = "Default";
            sr.sortingOrder = 0;

            // --- Collider -------------------------------------------------------
            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.size = size;

            // --- Layer ----------------------------------------------------------
            int layerIdx = LayerMask.NameToLayer(groundLayerName);
            if (layerIdx >= 0) go.layer = layerIdx;
            go.tag = GameConstants.TagGround;

            // --- One-way --------------------------------------------------------
            if (oneWay)
            {
                PlatformEffector2D effector = go.AddComponent<PlatformEffector2D>();
                effector.useOneWay          = true;
                effector.surfaceArc         = 180f;
                col.usedByEffector          = true;
            }

            // --- Moving ----------------------------------------------------------
            if (moving)
            {
                MovingPlatform mp = go.AddComponent<MovingPlatform>();
                mp.SetMovementRange(2f, 2f);     // sane defaults; caller can override
            }

            return go;
        }

        // =========================================================================
        //  Texture generation
        // =========================================================================

        private Texture2D GeneratePlatformTexture(ZoneType zone, int width, int height)
        {
            Texture2D tex = new Texture2D(Mathf.Max(width, 4), Mathf.Max(height, 4),
                                          TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode   = TextureWrapMode.Repeat;

            Color baseColor, edgeColor, detailColor;
            GetZoneColors(zone, out baseColor, out edgeColor, out detailColor);

            int w = tex.width;
            int h = tex.height;

            System.Random rng = new System.Random((int)zone * 997 + w);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color c = SamplePlatformPixel(zone, x, y, w, h,
                                                  baseColor, edgeColor, detailColor, rng);
                    tex.SetPixel(x, y, c);
                }
            }

            tex.Apply();
            return tex;
        }

        private static Color SamplePlatformPixel(ZoneType zone,
            int x, int y, int w, int h,
            Color baseColor, Color edgeColor, Color detailColor,
            System.Random rng)
        {
            float fx = (float)x / w;
            float fy = (float)y / h;

            // Top surface edge highlight
            if (y == h - 1) return edgeColor;
            if (y == h - 2) return Color.Lerp(baseColor, edgeColor, 0.5f);

            // Bottom shadow
            if (y == 0) return Color.Lerp(baseColor, Color.black, 0.5f);

            switch (zone)
            {
                // Stone blocks with cracks
                case ZoneType.ShatteredRamparts:
                case ZoneType.Ashfields:
                {
                    bool crack = (x % (w / Mathf.Max(1, w / 16)) == 0) && fy < 0.7f;
                    if (crack) return Color.Lerp(baseColor, Color.black, 0.4f);
                    float noise = (float)(rng.NextDouble() * 0.06 - 0.03);
                    return new Color(baseColor.r + noise, baseColor.g + noise, baseColor.b + noise, 1f);
                }

                // Ember-veined stone (glowing crack lines)
                case ZoneType.EmberCrypts:
                case ZoneType.SlagPits:
                {
                    bool vein = (x % Mathf.Max(1, w / 8) < 2) && fy > 0.2f && fy < 0.8f;
                    if (vein) return Color.Lerp(detailColor, Color.white, 0.3f);
                    float noise = (float)(rng.NextDouble() * 0.04 - 0.02);
                    return new Color(baseColor.r + noise, baseColor.g + noise, baseColor.b + noise, 1f);
                }

                // Sand / bone – wavy horizontal bands
                case ZoneType.BoneWastes:
                {
                    float band = Mathf.Sin(fx * Mathf.PI * 4f + fy * 2f) * 0.05f;
                    return new Color(
                        Mathf.Clamp01(baseColor.r + band),
                        Mathf.Clamp01(baseColor.g + band),
                        Mathf.Clamp01(baseColor.b + band), 1f);
                }

                // Ice – flat with faint horizontal sheen
                case ZoneType.DrownedCitadel:
                {
                    float sheen = Mathf.Abs(Mathf.Sin(fy * Mathf.PI * 3f)) * 0.08f;
                    return new Color(
                        Mathf.Clamp01(baseColor.r + sheen),
                        Mathf.Clamp01(baseColor.g + sheen),
                        Mathf.Clamp01(baseColor.b + sheen), 1f);
                }

                // Obsidian – dark with purple sheen
                case ZoneType.ObsidianSpire:
                {
                    bool edge = (x == 0 || x == w - 1);
                    if (edge) return detailColor;
                    float gloss = (1f - fy) * 0.1f;
                    return new Color(
                        Mathf.Clamp01(baseColor.r + gloss * 0.5f),
                        Mathf.Clamp01(baseColor.g + gloss * 0.3f),
                        Mathf.Clamp01(baseColor.b + gloss), 1f);
                }

                // Wound – pulsing red-black
                case ZoneType.TheWound:
                {
                    float noise = (float)(rng.NextDouble() * 0.08 - 0.04);
                    bool vein2 = (y % Mathf.Max(1, h / 4) == 0);
                    if (vein2) return detailColor;
                    return new Color(
                        Mathf.Clamp01(baseColor.r + noise),
                        Mathf.Clamp01(baseColor.g + noise * 0.5f),
                        Mathf.Clamp01(baseColor.b + noise * 0.5f), 1f);
                }

                // Dragon / volcanic
                case ZoneType.DragonsApproach:
                case ZoneType.ThroneOfAsh:
                {
                    bool lava = (x % Mathf.Max(1, w / 6) < 1) && fy < 0.5f;
                    if (lava) return detailColor;
                    float noise = (float)(rng.NextDouble() * 0.05 - 0.025);
                    return new Color(
                        Mathf.Clamp01(baseColor.r + noise),
                        Mathf.Clamp01(baseColor.g + noise),
                        Mathf.Clamp01(baseColor.b + noise), 1f);
                }

                default:
                {
                    float n = (float)(rng.NextDouble() * 0.04 - 0.02);
                    return new Color(
                        Mathf.Clamp01(baseColor.r + n),
                        Mathf.Clamp01(baseColor.g + n),
                        Mathf.Clamp01(baseColor.b + n), 1f);
                }
            }
        }

        private static void GetZoneColors(ZoneType zone,
            out Color baseColor, out Color edgeColor, out Color detailColor)
        {
            switch (zone)
            {
                case ZoneType.Ashfields:
                    baseColor   = new Color(0.40f, 0.30f, 0.22f);
                    edgeColor   = new Color(0.70f, 0.55f, 0.35f);
                    detailColor = new Color(0.90f, 0.60f, 0.20f);
                    break;
                case ZoneType.EmberCrypts:
                    baseColor   = new Color(0.22f, 0.12f, 0.18f);
                    edgeColor   = new Color(0.65f, 0.30f, 0.40f);
                    detailColor = new Color(1.00f, 0.50f, 0.20f);
                    break;
                case ZoneType.ShatteredRamparts:
                    baseColor   = new Color(0.45f, 0.42f, 0.38f);
                    edgeColor   = new Color(0.72f, 0.68f, 0.60f);
                    detailColor = new Color(0.30f, 0.28f, 0.26f);
                    break;
                case ZoneType.SlagPits:
                    baseColor   = new Color(0.28f, 0.14f, 0.06f);
                    edgeColor   = new Color(0.60f, 0.30f, 0.10f);
                    detailColor = new Color(1.00f, 0.55f, 0.05f);
                    break;
                case ZoneType.DrownedCitadel:
                    baseColor   = new Color(0.18f, 0.28f, 0.48f);
                    edgeColor   = new Color(0.50f, 0.70f, 0.90f);
                    detailColor = new Color(0.70f, 0.88f, 1.00f);
                    break;
                case ZoneType.BoneWastes:
                    baseColor   = new Color(0.72f, 0.66f, 0.52f);
                    edgeColor   = new Color(0.92f, 0.88f, 0.76f);
                    detailColor = new Color(0.55f, 0.50f, 0.38f);
                    break;
                case ZoneType.ObsidianSpire:
                    baseColor   = new Color(0.10f, 0.08f, 0.14f);
                    edgeColor   = new Color(0.45f, 0.35f, 0.60f);
                    detailColor = new Color(0.60f, 0.45f, 0.80f);
                    break;
                case ZoneType.TheWound:
                    baseColor   = new Color(0.25f, 0.05f, 0.05f);
                    edgeColor   = new Color(0.80f, 0.15f, 0.15f);
                    detailColor = new Color(1.00f, 0.25f, 0.10f);
                    break;
                case ZoneType.DragonsApproach:
                    baseColor   = new Color(0.35f, 0.18f, 0.08f);
                    edgeColor   = new Color(0.75f, 0.45f, 0.18f);
                    detailColor = new Color(1.00f, 0.65f, 0.10f);
                    break;
                case ZoneType.ThroneOfAsh:
                    baseColor   = new Color(0.15f, 0.10f, 0.08f);
                    edgeColor   = new Color(0.45f, 0.28f, 0.18f);
                    detailColor = new Color(0.80f, 0.40f, 0.20f);
                    break;
                default:
                    baseColor   = new Color(0.4f, 0.4f, 0.4f);
                    edgeColor   = new Color(0.7f, 0.7f, 0.7f);
                    detailColor = new Color(0.6f, 0.6f, 0.6f);
                    break;
            }
        }
    }
}
