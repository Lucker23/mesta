using UnityEngine;

namespace LastEmberKnight
{
    /// <summary>
    /// Generates all game sprites at runtime via Texture2D.SetPixel.
    /// No external art assets — everything is pixel art built in code.
    /// </summary>
    public static class ProceduralSpriteGenerator
    {
        // Nearest-neighbor pixel-perfect scaling
        private static Sprite MakeSprite(Texture2D tex, float pixelsPerUnit = 16f)
        {
            tex.filterMode = FilterMode.Point;
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), pixelsPerUnit);
        }

        private static Texture2D NewTex(int w, int h)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Point;
            // Clear to transparent
            Color clear = Color.clear;
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                    t.SetPixel(x, y, clear);
            return t;
        }

        // ─── PLAYER ──────────────────────────────────────────────────────────
        // 18x26 pixel art: dark knight with glowing eyes, cape, ember sword
        public static Sprite GeneratePlayerSprite()
        {
            var t = NewTex(18, 26);
            Color body     = new Color(0.15f, 0.12f, 0.10f);  // Dark charcoal
            Color armor    = new Color(0.22f, 0.20f, 0.18f);  // Dark metal
            Color accent   = new Color(0.70f, 0.30f, 0.05f);  // Ember orange trim
            Color eyeGlow  = new Color(1.00f, 0.55f, 0.00f);  // Bright orange eyes
            Color cape     = new Color(0.45f, 0.05f, 0.05f);  // Dark red cape
            Color sword    = new Color(0.80f, 0.40f, 0.10f);  // Ember-lit sword
            Color swordGlow= new Color(1.00f, 0.70f, 0.20f);  // Sword edge glow
            Color outline  = new Color(0.04f, 0.03f, 0.03f);  // Near-black outline
            Color hornClr  = new Color(0.55f, 0.50f, 0.45f);  // Helmet horns

            // Helmet horns (top)
            t.SetPixel(7,  25, hornClr); t.SetPixel(9,  25, hornClr);
            t.SetPixel(7,  24, hornClr); t.SetPixel(9,  24, hornClr);
            t.SetPixel(7,  23, outline); t.SetPixel(9,  23, outline);

            // Helmet (rows 20-22)
            for (int x = 5; x <= 12; x++) t.SetPixel(x, 22, armor);
            for (int x = 4; x <= 13; x++) t.SetPixel(x, 21, armor);
            for (int x = 4; x <= 13; x++) t.SetPixel(x, 20, armor);
            // Visor slit
            t.SetPixel(5, 21, outline); t.SetPixel(6, 21, outline);
            t.SetPixel(7, 21, eyeGlow); t.SetPixel(8, 21, eyeGlow);
            t.SetPixel(9, 21, eyeGlow); t.SetPixel(10, 21, eyeGlow);
            t.SetPixel(11, 21, outline); t.SetPixel(12, 21, outline);
            // Helmet bottom trim
            for (int x = 4; x <= 13; x++) t.SetPixel(x, 20, accent);

            // Neck
            t.SetPixel(8, 19, body); t.SetPixel(9, 19, body);

            // Chest / torso (rows 14-18)
            for (int y = 14; y <= 18; y++)
                for (int x = 4; x <= 13; x++)
                    t.SetPixel(x, y, armor);
            // Chest emblem (ember cross)
            t.SetPixel(8, 17, accent); t.SetPixel(9, 17, accent);
            t.SetPixel(8, 16, accent); t.SetPixel(9, 16, accent);
            t.SetPixel(7, 16, accent); t.SetPixel(10, 16, accent);
            // Pauldrons
            for (int y = 16; y <= 18; y++)
            { t.SetPixel(3, y, armor); t.SetPixel(14, y, armor); }

            // Cape (behind — rows 6-18, left side)
            for (int y = 6; y <= 18; y++)
            {
                t.SetPixel(2,  y, cape);
                t.SetPixel(15, y, cape);
            }
            for (int y = 6; y <= 12; y++)
            {
                t.SetPixel(1,  y, cape);
                t.SetPixel(16, y, cape);
            }

            // Belt
            for (int x = 4; x <= 13; x++) t.SetPixel(x, 13, accent);

            // Hips / lower armor (rows 10-12)
            for (int y = 10; y <= 12; y++)
                for (int x = 5; x <= 12; x++)
                    t.SetPixel(x, y, armor);

            // Left leg (rows 4-9)
            for (int y = 4; y <= 9; y++)
            {
                t.SetPixel(5, y, body); t.SetPixel(6, y, body);
                t.SetPixel(7, y, body);
            }
            // Right leg
            for (int y = 4; y <= 9; y++)
            {
                t.SetPixel(10, y, body); t.SetPixel(11, y, body);
                t.SetPixel(12, y, body);
            }
            // Knee guards
            t.SetPixel(5, 7, armor); t.SetPixel(6, 7, armor); t.SetPixel(7, 7, armor);
            t.SetPixel(10, 7, armor); t.SetPixel(11, 7, armor); t.SetPixel(12, 7, armor);

            // Boots
            for (int x = 4; x <= 8; x++)  t.SetPixel(x, 3, armor);
            for (int x = 4; x <= 8; x++)  t.SetPixel(x, 2, armor);
            for (int x = 9; x <= 13; x++) t.SetPixel(x, 3, armor);
            for (int x = 9; x <= 13; x++) t.SetPixel(x, 2, armor);

            // Sword (right hand — col 14-16, rows 11-19)
            t.SetPixel(15, 19, swordGlow); t.SetPixel(15, 18, sword);
            t.SetPixel(15, 17, sword);     t.SetPixel(15, 16, sword);
            t.SetPixel(15, 15, sword);     t.SetPixel(15, 14, swordGlow);
            t.SetPixel(15, 13, swordGlow); t.SetPixel(15, 12, sword);
            t.SetPixel(15, 11, sword);
            // Crossguard
            t.SetPixel(14, 14, sword); t.SetPixel(16, 14, sword);

            return MakeSprite(t, 16f);
        }

        // ─── PLAYER DASH AFTERIMAGE ───────────────────────────────────────────
        public static Sprite GeneratePlayerDashSprite()
        {
            var t = NewTex(18, 26);
            Color ghost = new Color(0.8f, 0.4f, 0.1f, 0.4f);
            for (int x = 3; x <= 14; x++)
                for (int y = 2; y <= 22; y++)
                    if (Random.value > 0.4f)
                        t.SetPixel(x, y, ghost);
            return MakeSprite(t, 16f);
        }

        // ─── SHARD COLLECTIBLE ────────────────────────────────────────────────
        public static Sprite GenerateShardSprite()
        {
            var t = NewTex(8, 10);
            Color core  = new Color(1.0f, 0.8f, 0.2f);
            Color shine = new Color(1.0f, 1.0f, 0.6f);
            Color edge  = new Color(0.7f, 0.5f, 0.1f);

            // Diamond shard shape
            t.SetPixel(3, 9, shine);  t.SetPixel(4, 9, shine);
            t.SetPixel(2, 8, core);   t.SetPixel(3, 8, core); t.SetPixel(4, 8, core); t.SetPixel(5, 8, core);
            t.SetPixel(1, 7, edge);   t.SetPixel(2, 7, core); t.SetPixel(3, 7, shine); t.SetPixel(4, 7, core); t.SetPixel(5, 7, core); t.SetPixel(6, 7, edge);
            t.SetPixel(1, 6, core);   t.SetPixel(2, 6, core); t.SetPixel(3, 6, core); t.SetPixel(4, 6, core); t.SetPixel(5, 6, core); t.SetPixel(6, 6, core);
            t.SetPixel(2, 5, edge);   t.SetPixel(3, 5, core); t.SetPixel(4, 5, core); t.SetPixel(5, 5, edge);
            t.SetPixel(3, 4, edge);   t.SetPixel(4, 4, edge);
            t.SetPixel(3, 3, edge);   t.SetPixel(4, 3, edge);
            t.SetPixel(3, 2, core);   t.SetPixel(4, 2, core);
            t.SetPixel(3, 1, edge);   t.SetPixel(4, 1, edge);
            t.SetPixel(3, 0, edge);
            return MakeSprite(t, 16f);
        }

        // ─── FOOD SPRITES ──────────────────────────────────────────────────────
        public static Sprite GenerateFoodSprite(FoodType type)
        {
            switch (type)
            {
                case FoodType.EmberBread:    return GenerateEmberBread();
                case FoodType.AshStew:       return GenerateAshStew();
                case FoodType.ManaDraught:   return GenerateManaDraught();
                case FoodType.PhoenixElixir: return GeneratePhoenixElixir();
                case FoodType.BoneJerky:     return GenerateBoneJerky();
                case FoodType.EmberFruit:    return GenerateEmberFruit();
                default: return GenerateEmberBread();
            }
        }

        private static Sprite GenerateEmberBread()
        {
            var t = NewTex(10, 8);
            Color crust = new Color(0.55f, 0.30f, 0.10f);
            Color inner = new Color(0.80f, 0.55f, 0.25f);
            Color glow  = new Color(1.00f, 0.60f, 0.10f);
            for (int x = 1; x <= 8; x++) { t.SetPixel(x, 0, crust); t.SetPixel(x, 1, crust); }
            for (int x = 0; x <= 9; x++) { t.SetPixel(x, 2, crust); t.SetPixel(x, 3, inner); t.SetPixel(x, 4, inner); t.SetPixel(x, 5, crust); }
            for (int x = 1; x <= 8; x++) t.SetPixel(x, 6, inner);
            for (int x = 2; x <= 7; x++) t.SetPixel(x, 7, crust);
            t.SetPixel(4, 4, glow); t.SetPixel(5, 4, glow);
            return MakeSprite(t, 16f);
        }

        private static Sprite GenerateAshStew()
        {
            var t = NewTex(10, 10);
            Color bowl   = new Color(0.30f, 0.20f, 0.15f);
            Color liquid  = new Color(0.15f, 0.10f, 0.08f);
            Color steam  = new Color(0.60f, 0.55f, 0.50f, 0.6f);
            for (int x = 0; x <= 9; x++) { t.SetPixel(x, 0, bowl); t.SetPixel(x, 1, bowl); }
            for (int x = 0; x <= 9; x++) for (int y = 2; y <= 5; y++) t.SetPixel(x, y, liquid);
            for (int x = 1; x <= 8; x++) t.SetPixel(x, 6, bowl);
            t.SetPixel(3, 7, steam); t.SetPixel(5, 8, steam); t.SetPixel(7, 7, steam);
            return MakeSprite(t, 16f);
        }

        private static Sprite GenerateManaDraught()
        {
            var t = NewTex(8, 12);
            Color glass = new Color(0.20f, 0.35f, 0.60f, 0.8f);
            Color liquid = new Color(0.10f, 0.20f, 0.80f);
            Color glow  = new Color(0.40f, 0.60f, 1.00f);
            Color cap   = new Color(0.50f, 0.45f, 0.40f);
            t.SetPixel(3, 11, cap); t.SetPixel(4, 11, cap);
            for (int x = 2; x <= 5; x++) t.SetPixel(x, 10, glass);
            for (int y = 3; y <= 9;  y++) for (int x = 1; x <= 6; x++) t.SetPixel(x, y, liquid);
            t.SetPixel(2, 6, glow); t.SetPixel(3, 7, glow);
            for (int x = 2; x <= 5; x++) { t.SetPixel(x, 2, glass); t.SetPixel(x, 1, glass); }
            for (int x = 1; x <= 6; x++) t.SetPixel(x, 0, glass);
            return MakeSprite(t, 16f);
        }

        private static Sprite GeneratePhoenixElixir()
        {
            var t = NewTex(8, 14);
            Color glass = new Color(0.60f, 0.20f, 0.40f, 0.75f);
            Color liquid = new Color(0.90f, 0.30f, 0.60f);
            Color glow  = new Color(1.00f, 0.70f, 0.90f);
            Color cap   = new Color(0.80f, 0.70f, 0.60f);
            t.SetPixel(3, 13, cap); t.SetPixel(4, 13, cap);
            for (int x = 2; x <= 5; x++) t.SetPixel(x, 12, glass);
            for (int y = 4; y <= 11; y++) for (int x = 1; x <= 6; x++) t.SetPixel(x, y, liquid);
            t.SetPixel(2, 7, glow); t.SetPixel(4, 8, glow); t.SetPixel(5, 6, glow);
            for (int x = 1; x <= 6; x++) { t.SetPixel(x, 3, glass); t.SetPixel(x, 2, glass); t.SetPixel(x, 1, glass); }
            for (int x = 2; x <= 5; x++) t.SetPixel(x, 0, glass);
            return MakeSprite(t, 16f);
        }

        private static Sprite GenerateBoneJerky()
        {
            var t = NewTex(12, 6);
            Color bone = new Color(0.75f, 0.70f, 0.55f);
            Color dark = new Color(0.50f, 0.45f, 0.35f);
            for (int x = 0; x <= 11; x++) t.SetPixel(x, 2, bone);
            for (int x = 0; x <= 11; x++) t.SetPixel(x, 3, dark);
            for (int x = 1; x <= 10; x++) { t.SetPixel(x, 1, bone); t.SetPixel(x, 4, bone); }
            t.SetPixel(0, 0, bone); t.SetPixel(1, 0, bone);
            t.SetPixel(10, 0, bone); t.SetPixel(11, 0, bone);
            t.SetPixel(0, 5, bone); t.SetPixel(1, 5, bone);
            t.SetPixel(10, 5, bone); t.SetPixel(11, 5, bone);
            return MakeSprite(t, 16f);
        }

        private static Sprite GenerateEmberFruit()
        {
            var t = NewTex(10, 10);
            Color skin  = new Color(0.90f, 0.35f, 0.05f);
            Color inner = new Color(1.00f, 0.60f, 0.10f);
            Color glow  = new Color(1.00f, 0.85f, 0.30f);
            Color stem  = new Color(0.20f, 0.45f, 0.10f);
            t.SetPixel(4, 9, stem); t.SetPixel(5, 9, stem);
            for (int x = 2; x <= 7; x++) t.SetPixel(x, 8, skin);
            for (int x = 1; x <= 8; x++) { t.SetPixel(x, 7, skin); t.SetPixel(x, 6, inner); t.SetPixel(x, 5, inner); t.SetPixel(x, 4, skin); }
            for (int x = 2; x <= 7; x++) { t.SetPixel(x, 3, skin); t.SetPixel(x, 2, skin); }
            t.SetPixel(4, 6, glow); t.SetPixel(5, 6, glow); t.SetPixel(5, 7, glow);
            return MakeSprite(t, 16f);
        }

        // ─── PLATFORM ─────────────────────────────────────────────────────────
        public static Sprite GeneratePlatformSprite(ZoneType zone, int width = 32, int height = 8)
        {
            var t = NewTex(width, height);
            Color top, mid, bot, edge;

            switch (zone)
            {
                case ZoneType.Ashfields:
                    top = new Color(0.25f, 0.18f, 0.12f);
                    mid = new Color(0.18f, 0.12f, 0.08f);
                    bot = new Color(0.10f, 0.08f, 0.06f);
                    edge = new Color(0.80f, 0.35f, 0.05f); // ember crack glow
                    break;
                case ZoneType.EmberCrypts:
                    top = new Color(0.15f, 0.25f, 0.15f);
                    mid = new Color(0.10f, 0.18f, 0.10f);
                    bot = new Color(0.08f, 0.12f, 0.08f);
                    edge = new Color(0.20f, 0.90f, 0.40f); // bioluminescent
                    break;
                case ZoneType.SlagPits:
                    top = new Color(0.30f, 0.12f, 0.05f);
                    mid = new Color(0.20f, 0.08f, 0.03f);
                    bot = new Color(0.15f, 0.05f, 0.02f);
                    edge = new Color(1.00f, 0.50f, 0.00f); // lava glow
                    break;
                case ZoneType.DrownedCitadel:
                    top = new Color(0.12f, 0.20f, 0.30f);
                    mid = new Color(0.08f, 0.15f, 0.22f);
                    bot = new Color(0.05f, 0.10f, 0.18f);
                    edge = new Color(0.30f, 0.70f, 1.00f); // water shimmer
                    break;
                case ZoneType.ObsidianSpire:
                    top = new Color(0.10f, 0.08f, 0.20f);
                    mid = new Color(0.08f, 0.06f, 0.15f);
                    bot = new Color(0.05f, 0.04f, 0.10f);
                    edge = new Color(0.60f, 0.30f, 1.00f); // crystal glow
                    break;
                default:
                    top = new Color(0.22f, 0.18f, 0.15f);
                    mid = new Color(0.15f, 0.12f, 0.10f);
                    bot = new Color(0.10f, 0.08f, 0.07f);
                    edge = new Color(0.60f, 0.40f, 0.20f);
                    break;
            }

            for (int x = 0; x < width; x++)
            {
                // Top surface
                t.SetPixel(x, height - 1, top);
                t.SetPixel(x, height - 2, top);
                // Middle
                for (int y = 2; y < height - 2; y++)
                    t.SetPixel(x, y, mid);
                // Bottom
                t.SetPixel(x, 1, bot);
                t.SetPixel(x, 0, bot);
                // Random crack glow on top row
                if (Random.value < 0.08f)
                    t.SetPixel(x, height - 1, edge);
            }
            // Edge highlights
            for (int y = 0; y < height; y++)
            {
                t.SetPixel(0, y, edge);
                t.SetPixel(width - 1, y, edge);
            }
            return MakeSprite(t, 16f);
        }

        // ─── SHRINE ───────────────────────────────────────────────────────────
        public static Sprite GenerateShrineSprite()
        {
            var t = NewTex(20, 28);
            Color stone = new Color(0.25f, 0.22f, 0.20f);
            Color dark  = new Color(0.15f, 0.12f, 0.10f);
            Color flame = new Color(1.00f, 0.55f, 0.10f);
            Color fglow = new Color(1.00f, 0.80f, 0.30f);
            Color rune  = new Color(0.80f, 0.40f, 0.60f);

            // Base
            for (int x = 0; x <= 19; x++) { t.SetPixel(x, 0, stone); t.SetPixel(x, 1, stone); t.SetPixel(x, 2, stone); }
            // Steps
            for (int x = 2; x <= 17; x++) for (int y = 3; y <= 4; y++) t.SetPixel(x, y, stone);
            // Pillar
            for (int x = 7; x <= 12; x++) for (int y = 5; y <= 20; y++) t.SetPixel(x, y, dark);
            // Runes on pillar
            t.SetPixel(9, 10, rune); t.SetPixel(10, 10, rune);
            t.SetPixel(9, 13, rune); t.SetPixel(10, 13, rune);
            t.SetPixel(8, 10, rune); t.SetPixel(11, 10, rune);
            // Bowl at top
            for (int x = 5; x <= 14; x++) for (int y = 21; y <= 23; y++) t.SetPixel(x, y, stone);
            // Flame
            t.SetPixel(9, 24, flame); t.SetPixel(10, 24, flame);
            t.SetPixel(8, 25, flame); t.SetPixel(9, 25, fglow); t.SetPixel(10, 25, fglow); t.SetPixel(11, 25, flame);
            t.SetPixel(9, 26, fglow); t.SetPixel(10, 26, fglow);
            t.SetPixel(9, 27, fglow);
            return MakeSprite(t, 16f);
        }

        // ─── EXIT DOOR ────────────────────────────────────────────────────────
        public static Sprite GenerateExitDoorSprite(bool active)
        {
            var t = NewTex(18, 28);
            Color frame  = new Color(0.30f, 0.25f, 0.20f);
            Color door   = active ? new Color(0.60f, 0.50f, 0.10f) : new Color(0.18f, 0.15f, 0.12f);
            Color glow   = active ? new Color(1.00f, 0.85f, 0.20f) : new Color(0.20f, 0.18f, 0.15f);
            Color arch   = new Color(0.35f, 0.30f, 0.25f);

            // Frame
            for (int y = 0; y <= 27; y++)
            {
                t.SetPixel(0,  y, frame); t.SetPixel(1,  y, frame);
                t.SetPixel(16, y, frame); t.SetPixel(17, y, frame);
            }
            for (int x = 0; x <= 17; x++) { t.SetPixel(x, 0, frame); t.SetPixel(x, 1, frame); }
            // Arch top
            for (int x = 2; x <= 15; x++) t.SetPixel(x, 26, arch);
            for (int x = 3; x <= 14; x++) t.SetPixel(x, 27, arch);
            // Door fill
            for (int x = 2; x <= 15; x++)
                for (int y = 2; y <= 25; y++)
                    t.SetPixel(x, y, door);
            // Glow edge
            if (active)
                for (int y = 2; y <= 25; y++)
                { t.SetPixel(2, y, glow); t.SetPixel(15, y, glow); }
            // Center symbol
            t.SetPixel(8, 13, glow); t.SetPixel(9, 13, glow);
            t.SetPixel(8, 14, glow); t.SetPixel(9, 14, glow);
            t.SetPixel(7, 13, glow); t.SetPixel(10, 13, glow);
            return MakeSprite(t, 16f);
        }

        // ─── PROJECTILE SPRITES ───────────────────────────────────────────────
        public static Sprite GenerateProjectileSprite(Color col, int size = 6)
        {
            var t = NewTex(size, size);
            int cx = size / 2, cy = size / 2;
            Color core = col;
            Color glow = new Color(col.r + 0.3f, col.g + 0.3f, col.b + 0.3f, 0.7f);
            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                    if (d < 1.2f) t.SetPixel(x, y, core);
                    else if (d < 2.2f) t.SetPixel(x, y, glow);
                }
            return MakeSprite(t, 16f);
        }

        // ─── BACKGROUND ELEMENTS ─────────────────────────────────────────────
        // Dead tree silhouette (Ashfields)
        public static Sprite GenerateDeadTreeSprite()
        {
            var t = NewTex(24, 48);
            Color bark  = new Color(0.12f, 0.09f, 0.07f);
            Color ember = new Color(0.80f, 0.30f, 0.05f, 0.6f);

            // Trunk
            for (int y = 0; y <= 30; y++)
                for (int x = 10; x <= 13; x++)
                    t.SetPixel(x, y, bark);
            // Main branches
            for (int i = 0; i <= 8; i++) { t.SetPixel(9-i, 30+i, bark); t.SetPixel(8-i, 30+i, bark); } // left
            for (int i = 0; i <= 8; i++) { t.SetPixel(14+i, 30+i, bark); t.SetPixel(15+i, 30+i, bark); } // right
            // Thin branches
            for (int i = 0; i <= 5; i++) t.SetPixel(6-i, 38+i/2, bark);
            for (int i = 0; i <= 5; i++) t.SetPixel(18+i, 38+i/2, bark);
            // Ember glow at base
            for (int x = 8; x <= 15; x++) t.SetPixel(x, 0, ember);
            for (int x = 9; x <= 14; x++) t.SetPixel(x, 1, ember);
            return MakeSprite(t, 16f);
        }

        // Blood moon circle
        public static Sprite GenerateMoonSprite()
        {
            int size = 32;
            var t = NewTex(size, size);
            int cx = size / 2, cy = size / 2;
            Color moon  = new Color(0.60f, 0.10f, 0.10f);
            Color inner = new Color(0.80f, 0.20f, 0.15f);
            Color glow  = new Color(0.40f, 0.05f, 0.05f, 0.5f);

            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                    if (d < 7f)       t.SetPixel(x, y, inner);
                    else if (d < 12f) t.SetPixel(x, y, moon);
                    else if (d < 16f) t.SetPixel(x, y, glow);
                }
            return MakeSprite(t, 16f);
        }

        // Star sprite
        public static Sprite GenerateStarSprite()
        {
            var t = NewTex(4, 4);
            Color star = new Color(1f, 0.95f, 0.80f, 0.9f);
            Color dim  = new Color(1f, 0.95f, 0.80f, 0.4f);
            t.SetPixel(1, 2, star); t.SetPixel(2, 2, star);
            t.SetPixel(1, 1, star); t.SetPixel(2, 1, star);
            t.SetPixel(0, 2, dim);  t.SetPixel(3, 2, dim);
            t.SetPixel(1, 0, dim);  t.SetPixel(1, 3, dim);
            t.SetPixel(2, 0, dim);  t.SetPixel(2, 3, dim);
            return MakeSprite(t, 16f);
        }

        // Lava texture (SlagPits background)
        public static Sprite GenerateLavaSprite(int w = 64, int h = 16)
        {
            var t = NewTex(w, h);
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                {
                    float n = Mathf.PerlinNoise(x * 0.15f, y * 0.3f);
                    Color c;
                    if (n > 0.65f)
                        c = new Color(1.0f, 0.80f, 0.10f); // bright lava
                    else if (n > 0.45f)
                        c = new Color(0.90f, 0.35f, 0.05f); // mid lava
                    else if (n > 0.25f)
                        c = new Color(0.50f, 0.10f, 0.02f); // dark lava
                    else
                        c = new Color(0.15f, 0.04f, 0.01f); // crust
                    t.SetPixel(x, y, c);
                }
            return MakeSprite(t, 16f);
        }

        // Generic colored quad sprite (for UI and misc use)
        public static Sprite GenerateColoredQuad(Color col, int w = 16, int h = 16)
        {
            var t = NewTex(w, h);
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                    t.SetPixel(x, y, col);
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        }

        // 1px outline pass over a sprite texture
        public static void ApplyOutline(Texture2D tex, Color outlineColor)
        {
            int w = tex.width, h = tex.height;
            bool[,] mask = new bool[w, h];
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                    mask[x, y] = tex.GetPixel(x, y).a > 0.1f;

            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                {
                    if (mask[x, y]) continue;
                    bool hasNeighbor = (x > 0   && mask[x-1, y]) ||
                                       (x < w-1 && mask[x+1, y]) ||
                                       (y > 0   && mask[x,   y-1]) ||
                                       (y < h-1 && mask[x,   y+1]);
                    if (hasNeighbor) tex.SetPixel(x, y, outlineColor);
                }
            tex.Apply();
        }
    }
}
