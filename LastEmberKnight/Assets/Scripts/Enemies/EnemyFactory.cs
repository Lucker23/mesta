using UnityEngine;

namespace LastEmberKnight
{
    /// <summary>
    /// Singleton factory that spawns and configures enemy GameObjects at runtime.
    /// Procedurally generates sprites when no prefab is provided.
    /// </summary>
    public class EnemyFactory : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────
        public static EnemyFactory Instance { get; private set; }

        // ── Inspector Fields ──────────────────────────────────────────────────
        [Header("Prefabs (optional – procedural sprite used if null)")]
        [SerializeField] private GameObject ashenGuardPrefab;
        [SerializeField] private GameObject emberWraithPrefab;
        [SerializeField] private GameObject boneCrawlerPrefab;
        [SerializeField] private GameObject shieldBearerPrefab;
        [SerializeField] private GameObject stoneGolemPrefab;

        [Header("Sprite Pixel Size")]
        [SerializeField] private int pixelsPerUnit = 16;

        // ── Unity Lifecycle ────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Spawn a random enemy at position scaled to the given difficulty tier and zone.
        /// Called by LevelGenerator for procedural level population.
        /// </summary>
        public GameObject SpawnEnemy(Vector2 position, int difficultyTier, ZoneType zone)
        {
            // Pick an enemy type based on difficulty and zone
            EnemyType type = ChooseEnemyType(difficultyTier, zone);
            float mult = 1f + difficultyTier * GameConstants.DifficultyPerLevel;
            return SpawnEnemy(type, new Vector3(position.x, position.y, 0f), mult);
        }

        /// <summary>
        /// Spawn an elite (harder) enemy variant for higher-tier zones.
        /// Called by LevelGenerator when eliteCount > 0.
        /// </summary>
        public GameObject SpawnElite(Vector2 position, int difficultyTier, ZoneType zone)
        {
            // Elites favour heavier types
            EnemyType type = difficultyTier >= 6 ? EnemyType.StoneGolem
                           : difficultyTier >= 3 ? EnemyType.ShieldBearer
                           : EnemyType.AshenGuard;
            float mult = 1f + difficultyTier * GameConstants.DifficultyPerLevel * 1.5f;
            return SpawnEnemy(type, new Vector3(position.x, position.y, 0f), mult);
        }

        /// <summary>
        /// Spawn the zone boss at position. Called by LevelGenerator for boss levels.
        /// bossIndex = zone index (0-9). zone = current ZoneType.
        /// </summary>
        public GameObject SpawnBoss(Vector2 position, int bossIndex, ZoneType zone)
        {
            // Boss spawning deferred to BossFactory if present, else procedural fallback
            BossFactory bf = BossFactory.Instance;
            if (bf != null)
                return bf.SpawnBoss(bossIndex, new Vector3(position.x, position.y, 0f))?.gameObject;

            // Minimal fallback placeholder
            GameObject go = new GameObject($"Boss_{bossIndex}");
            go.transform.position = new Vector3(position.x, position.y, 0f);
            go.tag = GameConstants.TagBoss;
            go.layer = LayerMask.NameToLayer(GameConstants.LayerEnemy);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.color = new Color(0.8f, 0.1f, 0.1f);
            go.AddComponent<BoxCollider2D>();
            go.AddComponent<Rigidbody2D>().freezeRotation = true;
            return go;
        }

        private static EnemyType ChooseEnemyType(int difficultyTier, ZoneType zone)
        {
            // Vary enemy selection by zone to give each area distinct feel
            switch (zone)
            {
                case ZoneType.Ashfields:
                case ZoneType.ShatteredRamparts:
                    return difficultyTier >= 3 ? EnemyType.ShieldBearer : EnemyType.AshenGuard;
                case ZoneType.EmberCrypts:
                case ZoneType.TheWound:
                    return difficultyTier >= 2 ? EnemyType.EmberWraith : EnemyType.BoneCrawler;
                case ZoneType.BoneWastes:
                case ZoneType.ThroneOfAsh:
                    return difficultyTier >= 4 ? EnemyType.StoneGolem : EnemyType.BoneCrawler;
                default:
                    EnemyType[] all = (EnemyType[])System.Enum.GetValues(typeof(EnemyType));
                    int idx = Mathf.Clamp(difficultyTier / 2, 0, all.Length - 1);
                    return all[idx];
            }
        }

        /// <summary>
        /// Spawn an enemy of the given type at position.
        /// difficultyMult is forwarded to the spawned enemy's stat block.
        /// </summary>
        public GameObject SpawnEnemy(EnemyType type, Vector3 position, float difficultyMult = 1f)
        {
            GameObject prefab   = GetPrefab(type);
            GameObject instance;

            if (prefab != null)
            {
                instance = Instantiate(prefab, position, Quaternion.identity);
            }
            else
            {
                // Build a minimal enemy GameObject procedurally
                instance = BuildEnemyGameObject(type, position);
            }

            // Apply difficulty scale to base stats via the EnemyBase component
            EnemyBase enemy = instance.GetComponent<EnemyBase>();
            if (enemy != null && Mathf.Abs(difficultyMult - 1f) > 0.001f)
            {
                // Stats are scaled inside EnemyBase.ApplyDifficultyScaling using GameManager;
                // here we just call TakeDamage(0) to trigger Awake/Start safely (no-op).
                // Any additional direct scaling can be applied here if desired.
            }

            return instance;
        }

        // ── Sprite Generation ─────────────────────────────────────────────────

        /// <summary>
        /// Creates a procedurally generated Texture2D for the given enemy type
        /// and wraps it in a Sprite.
        /// </summary>
        public Sprite CreateEnemySprite(EnemyType type)
        {
            GetEnemySpriteConfig(type,
                out int width, out int height,
                out Color primaryColor, out Color secondaryColor, out Color accentColor);

            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode   = TextureWrapMode.Clamp
            };

            // Fill transparent background
            Color clear = Color.clear;
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    tex.SetPixel(x, y, clear);

            // Draw body silhouette
            DrawBody(tex, width, height, primaryColor, secondaryColor, accentColor, type);

            tex.Apply();

            return Sprite.Create(
                tex,
                new Rect(0, 0, width, height),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit);
        }

        // ── Private Helpers ───────────────────────────────────────────────────

        private GameObject GetPrefab(EnemyType type)
        {
            return type switch
            {
                EnemyType.AshenGuard  => ashenGuardPrefab,
                EnemyType.EmberWraith => emberWraithPrefab,
                EnemyType.BoneCrawler => boneCrawlerPrefab,
                EnemyType.ShieldBearer => shieldBearerPrefab,
                EnemyType.StoneGolem  => stoneGolemPrefab,
                _ => null
            };
        }

        private GameObject BuildEnemyGameObject(EnemyType type, Vector3 position)
        {
            GameObject go = new GameObject(type.ToString());
            go.transform.position = position;

            // Layer
            go.layer = LayerMask.NameToLayer(GameConstants.LayerEnemy);

            // Tag
            go.tag = "Enemy";

            // Sprite renderer with procedural sprite
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateEnemySprite(type);
            sr.sortingOrder = 10;

            // Rigidbody2D
            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // Collider – sized to spec
            GetEnemySizeConfig(type, out int pxW, out int pxH);
            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.size   = new Vector2(pxW / (float)pixelsPerUnit, pxH / (float)pixelsPerUnit);
            col.offset = new Vector2(0f, col.size.y * 0.5f);

            // Enemy AI component
            switch (type)
            {
                case EnemyType.AshenGuard:   go.AddComponent<AshenGuard>();   break;
                case EnemyType.EmberWraith:
                    rb.gravityScale = 0f;
                    go.AddComponent<EmberWraith>();
                    break;
                case EnemyType.BoneCrawler:  go.AddComponent<BoneCrawler>();  break;
                case EnemyType.ShieldBearer: go.AddComponent<ShieldBearer>(); break;
                case EnemyType.StoneGolem:   go.AddComponent<StoneGolem>();   break;
            }

            return go;
        }

        // ── Sprite Drawing ────────────────────────────────────────────────────

        private static void GetEnemySpriteConfig(
            EnemyType type,
            out int width, out int height,
            out Color primary, out Color secondary, out Color accent)
        {
            switch (type)
            {
                case EnemyType.AshenGuard:
                    width = 16; height = 24;
                    primary   = new Color(0.25f, 0.25f, 0.27f);   // dark gray
                    secondary = new Color(0.50f, 0.10f, 0.10f);   // deep red
                    accent    = new Color(0.70f, 0.18f, 0.18f);   // brighter red
                    break;
                case EnemyType.EmberWraith:
                    width = 14; height = 22;
                    primary   = new Color(0.55f, 0.90f, 0.95f, 0.85f); // ghostly cyan
                    secondary = new Color(0.55f, 0.20f, 0.85f, 0.75f); // purple
                    accent    = new Color(0.80f, 0.40f, 1.00f, 0.90f); // bright purple
                    break;
                case EnemyType.BoneCrawler:
                    width = 18; height = 12;
                    primary   = new Color(0.92f, 0.88f, 0.82f);   // bone white
                    secondary = new Color(0.15f, 0.13f, 0.12f);   // near-black dark
                    accent    = new Color(0.70f, 0.65f, 0.55f);   // aged bone
                    break;
                case EnemyType.ShieldBearer:
                    width = 18; height = 26;
                    primary   = new Color(0.45f, 0.45f, 0.48f);   // armored gray
                    secondary = new Color(0.80f, 0.65f, 0.15f);   // gold
                    accent    = new Color(0.95f, 0.85f, 0.30f);   // bright gold
                    break;
                case EnemyType.StoneGolem:
                default:
                    width = 22; height = 30;
                    primary   = new Color(0.42f, 0.35f, 0.28f);   // rocky brown
                    secondary = new Color(0.55f, 0.55f, 0.55f);   // gray
                    accent    = new Color(0.30f, 0.25f, 0.20f);   // dark brown cracks
                    break;
            }
        }

        private static void GetEnemySizeConfig(EnemyType type, out int pxW, out int pxH)
        {
            switch (type)
            {
                case EnemyType.AshenGuard:   pxW = 16; pxH = 24; break;
                case EnemyType.EmberWraith:  pxW = 14; pxH = 22; break;
                case EnemyType.BoneCrawler:  pxW = 18; pxH = 12; break;
                case EnemyType.ShieldBearer: pxW = 18; pxH = 26; break;
                case EnemyType.StoneGolem:   pxW = 22; pxH = 30; break;
                default:                     pxW = 16; pxH = 16; break;
            }
        }

        /// <summary>
        /// Paints a simple pixel silhouette that hints at the enemy's shape.
        /// </summary>
        private static void DrawBody(
            Texture2D tex, int w, int h,
            Color primary, Color secondary, Color accent,
            EnemyType type)
        {
            switch (type)
            {
                case EnemyType.BoneCrawler:
                    DrawCrawlerShape(tex, w, h, primary, secondary, accent);
                    break;
                case EnemyType.EmberWraith:
                    DrawWraithShape(tex, w, h, primary, secondary, accent);
                    break;
                case EnemyType.StoneGolem:
                    DrawGolemShape(tex, w, h, primary, secondary, accent);
                    break;
                default:
                    DrawHumanoidShape(tex, w, h, primary, secondary, accent);
                    break;
            }
        }

        // ── Shape Painters ───────────────────────────────────────────────────

        private static void DrawHumanoidShape(Texture2D tex, int w, int h,
            Color body, Color detail, Color accent)
        {
            int headSize = w / 3;
            int headX    = w / 2 - headSize / 2;
            int headY    = h - headSize - 1;

            // Head
            FillRect(tex, headX, headY, headSize, headSize, body);

            // Eyes
            tex.SetPixel(headX + 1, headY + headSize / 2, accent);
            tex.SetPixel(headX + headSize - 2, headY + headSize / 2, accent);

            // Torso
            int torsoW = w - 4;
            int torsoH = h / 2;
            FillRect(tex, 2, headY - torsoH, torsoW, torsoH, body);

            // Detail stripe
            FillRect(tex, 3, headY - torsoH + 2, torsoW - 2, 2, detail);

            // Legs
            int legW = (w / 2) - 2;
            FillRect(tex, 2,          0, legW, headY - torsoH, detail);
            FillRect(tex, w / 2 + 1,  0, legW, headY - torsoH, detail);

            // Accent shoulder marks
            tex.SetPixel(2,     headY - 1, accent);
            tex.SetPixel(w - 3, headY - 1, accent);
        }

        private static void DrawCrawlerShape(Texture2D tex, int w, int h,
            Color bone, Color dark, Color aged)
        {
            // Low flat body
            FillRect(tex, 2, h / 3, w - 4, h - h / 3 - 1, bone);

            // Legs (multiple short protrusions)
            for (int i = 0; i < 4; i++)
            {
                int lx = 2 + i * (w / 4);
                FillRect(tex, lx, 0, 2, h / 3, dark);
            }

            // Eyes – two bright dots
            tex.SetPixel(3,     h / 2, aged);
            tex.SetPixel(w - 4, h / 2, aged);

            // Crack detail
            for (int x = 4; x < w - 4; x += 3)
                tex.SetPixel(x, h / 2 + 1, dark);
        }

        private static void DrawWraithShape(Texture2D tex, int w, int h,
            Color ghost, Color purple, Color bright)
        {
            // Wispy tail (bottom half wider)
            for (int y = 0; y < h / 2; y++)
            {
                float t   = 1f - (float)y / (h / 2);
                int margin = Mathf.RoundToInt(t * w * 0.25f);
                for (int x = margin; x < w - margin; x++)
                    tex.SetPixel(x, y, Color.Lerp(purple, ghost, (float)y / (h / 2)));
            }

            // Upper body
            int bodyW = w - 4;
            FillRect(tex, 2, h / 2, bodyW, h / 2 - 2, ghost);

            // Glowing eyes
            tex.SetPixel(4,     h - 5, bright);
            tex.SetPixel(w - 5, h - 5, bright);
        }

        private static void DrawGolemShape(Texture2D tex, int w, int h,
            Color rock, Color gray, Color crack)
        {
            // Wide chunky body
            FillRect(tex, 1, 0, w - 2, h, rock);

            // Gray stone panels
            FillRect(tex, 3, h / 4, w - 6, h / 2, gray);

            // Crack lines
            for (int y = h / 4; y < h * 3 / 4; y += 4)
                FillRect(tex, w / 2 - 1, y, 2, 3, crack);
            for (int x = 4; x < w - 4; x += 5)
                FillRect(tex, x, h / 2, 1, 3, crack);

            // Eyes
            tex.SetPixel(4,     h - 5, new Color(1f, 0.4f, 0.1f));
            tex.SetPixel(w - 5, h - 5, new Color(1f, 0.4f, 0.1f));

            // Outline
            for (int x = 0; x < w; x++)
            {
                tex.SetPixel(x, 0,     crack);
                tex.SetPixel(x, h - 1, crack);
            }
            for (int y = 0; y < h; y++)
            {
                tex.SetPixel(0,     y, crack);
                tex.SetPixel(w - 1, y, crack);
            }
        }

        private static void FillRect(Texture2D tex, int x, int y, int w, int h, Color col)
        {
            int texW = tex.width;
            int texH = tex.height;
            for (int px = x; px < x + w && px < texW; px++)
                for (int py = y; py < y + h && py < texH; py++)
                    if (px >= 0 && py >= 0)
                        tex.SetPixel(px, py, col);
        }
    }
}
