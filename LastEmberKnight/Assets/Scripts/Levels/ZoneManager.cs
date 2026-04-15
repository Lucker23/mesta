using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LastEmberKnight
{
    // =========================================================================
    //  Data structures
    // =========================================================================

    public enum ParticleAmbientType
    {
        Embers,
        Fog,
        Rain,
        Dust,
        Ash,
        Snow,
        Sparks,
        ChaosOrbs,
        LavaDrops,
        DarkMotes
    }

    [System.Serializable]
    public struct ParallaxLayerData
    {
        public Color color;
        public float scrollSpeed;   // 0 = stationary, 1 = matches camera
        public float heightOffset;  // Y offset from ground
        public float depth;         // Z depth (negative = further back)
    }

    [System.Serializable]
    public struct ZoneData
    {
        public string name;
        public Color[] colorPalette;
        public ParticleAmbientType ambientParticleType;
        public ParallaxLayerData[] parallaxLayers;
        public Color ambientLightColor;
        public Color fogColor;
        public float fogDensity;
    }

    // =========================================================================
    //  ZoneManager
    // =========================================================================

    public class ZoneManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────
        public static ZoneManager Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("Parallax Root")]
        [SerializeField] private Transform backgroundRoot;
        [SerializeField] private Camera mainCamera;

        [Header("Particle Prefabs")]
        [SerializeField] private GameObject embersParticlePrefab;
        [SerializeField] private GameObject fogParticlePrefab;
        [SerializeField] private GameObject rainParticlePrefab;
        [SerializeField] private GameObject dustParticlePrefab;
        [SerializeField] private GameObject ashParticlePrefab;
        [SerializeField] private GameObject snowParticlePrefab;
        [SerializeField] private GameObject sparksParticlePrefab;
        [SerializeField] private GameObject chaosOrbsPrefab;
        [SerializeField] private GameObject lavaDroppingsPrefab;
        [SerializeField] private GameObject darkMotesPrefab;

        [Header("Post Processing")]
        [SerializeField] private Volume postProcessVolume;

        // ── Runtime ───────────────────────────────────────────────────────────
        private readonly List<GameObject> _activeBackgroundObjects = new List<GameObject>();
        private GameObject _activeAmbientParticles;
        private ZoneType _currentZone = (ZoneType)(-1);
        private Vector3 _lastCameraPos;
        private readonly List<(Transform layer, float speed)> _parallaxLayers
            = new List<(Transform, float)>();

        // ── Zone data table ───────────────────────────────────────────────────
        private static readonly ZoneData[] ZoneTable = BuildZoneTable();

        // ── Unity ─────────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            if (mainCamera == null)
                mainCamera = Camera.main;

            if (backgroundRoot == null)
            {
                var bg = new GameObject("BackgroundRoot");
                backgroundRoot = bg.transform;
            }

            if (mainCamera != null)
                _lastCameraPos = mainCamera.transform.position;
        }

        private void LateUpdate()
        {
            if (mainCamera == null) return;

            Vector3 camPos = mainCamera.transform.position;
            Vector3 delta  = camPos - _lastCameraPos;

            foreach (var (layer, speed) in _parallaxLayers)
            {
                if (layer == null) continue;
                layer.position += new Vector3(delta.x * speed, delta.y * speed, 0f);
            }

            _lastCameraPos = camPos;
        }

        // =========================================================================
        //  Public API
        // =========================================================================

        /// <summary>Returns the ZoneType for a given level index (floor(level/5), capped 0-9).</summary>
        public static ZoneType GetZone(int level)
        {
            int zoneIndex = Mathf.Clamp(level / GameConstants.LevelsPerZone, 0, GameConstants.TotalZones - 1);
            return (ZoneType)zoneIndex;
        }

        /// <summary>Build all visual layers and particles for the given zone.</summary>
        public void BuildZoneBackground(ZoneType zone)
        {
            if (_currentZone == zone) return;
            _currentZone = zone;

            ClearBackground();

            int idx = (int)zone;
            if (idx < 0 || idx >= ZoneTable.Length) return;
            ZoneData data = ZoneTable[idx];

            ApplyAmbientLighting(data);
            CreateParallaxLayers(data);
            SpawnAmbientParticles(data.ambientParticleType);
        }

        /// <summary>Retrieve a copy of the zone data for external systems.</summary>
        public static ZoneData GetZoneData(ZoneType zone)
        {
            int idx = (int)zone;
            return (idx >= 0 && idx < ZoneTable.Length) ? ZoneTable[idx] : default;
        }

        /// <summary>
        /// One-line lore flavor text for each zone, shown during level transitions.
        /// Each line is a fragment of the knight's story — he's walking through his
        /// own ruined history toward the dragon that burned it all down.
        /// </summary>
        public static string GetZoneLoreText(ZoneType zone)
        {
            switch (zone)
            {
                case ZoneType.Ashfields:       return "These fields were green once. He remembers.";
                case ZoneType.EmberCrypts:     return "The dead still guard what they could not save.";
                case ZoneType.ShatteredRamparts: return "His kingdom's walls. Unrecognizable.";
                case ZoneType.SlagPits:        return "The Dragon's fire runs deep here.";
                case ZoneType.DrownedCitadel:  return "Even the sea could not put out this war.";
                case ZoneType.BoneWastes:      return "Every bone here was someone's soldier.";
                case ZoneType.ObsidianSpire:   return "Drakar's lieutenants sleep in crystal tombs.";
                case ZoneType.TheWound:        return "Reality remembers the Dragon's passage.";
                case ZoneType.DragonsApproach: return "He can smell the smoke now.";
                case ZoneType.ThroneOfAsh:     return "The end. Or the beginning.";
                default:                       return string.Empty;
            }
        }

        // =========================================================================
        //  Background construction
        // =========================================================================

        private void ClearBackground()
        {
            _parallaxLayers.Clear();

            foreach (GameObject go in _activeBackgroundObjects)
            {
                if (go != null) Destroy(go);
            }
            _activeBackgroundObjects.Clear();

            if (_activeAmbientParticles != null)
            {
                Destroy(_activeAmbientParticles);
                _activeAmbientParticles = null;
            }
        }

        private void CreateParallaxLayers(ZoneData data)
        {
            if (data.parallaxLayers == null) return;

            for (int i = 0; i < data.parallaxLayers.Length; i++)
            {
                ParallaxLayerData layerData = data.parallaxLayers[i];

                GameObject layerGO = new GameObject($"ParallaxLayer_{i}");
                layerGO.transform.SetParent(backgroundRoot);

                float camX = mainCamera != null ? mainCamera.transform.position.x : 0f;
                float camY = mainCamera != null ? mainCamera.transform.position.y : 0f;
                layerGO.transform.position = new Vector3(camX, camY + layerData.heightOffset, layerData.depth);

                // Create a wide quad for this layer
                SpriteRenderer sr = layerGO.AddComponent<SpriteRenderer>();
                sr.sprite = CreateSolidSprite(layerData.color, 200, 40);
                sr.sortingOrder = -10 + i;
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.size = new Vector2(300f, 40f);

                _parallaxLayers.Add((layerGO.transform, layerData.scrollSpeed));
                _activeBackgroundObjects.Add(layerGO);
            }
        }

        private void SpawnAmbientParticles(ParticleAmbientType particleType)
        {
            GameObject prefab = GetParticlePrefab(particleType);

            if (prefab == null)
            {
                // Procedural fallback particle system
                _activeAmbientParticles = CreateProceduralParticles(particleType);
            }
            else
            {
                _activeAmbientParticles = Instantiate(prefab,
                    new Vector3(0f, 5f, -1f), Quaternion.identity);
            }

            if (_activeAmbientParticles != null && backgroundRoot != null)
                _activeAmbientParticles.transform.SetParent(backgroundRoot);
        }

        private GameObject GetParticlePrefab(ParticleAmbientType type)
        {
            switch (type)
            {
                case ParticleAmbientType.Embers:    return embersParticlePrefab;
                case ParticleAmbientType.Fog:       return fogParticlePrefab;
                case ParticleAmbientType.Rain:      return rainParticlePrefab;
                case ParticleAmbientType.Dust:      return dustParticlePrefab;
                case ParticleAmbientType.Ash:       return ashParticlePrefab;
                case ParticleAmbientType.Snow:      return snowParticlePrefab;
                case ParticleAmbientType.Sparks:    return sparksParticlePrefab;
                case ParticleAmbientType.ChaosOrbs: return chaosOrbsPrefab;
                case ParticleAmbientType.LavaDrops: return lavaDroppingsPrefab;
                case ParticleAmbientType.DarkMotes: return darkMotesPrefab;
                default: return null;
            }
        }

        private GameObject CreateProceduralParticles(ParticleAmbientType type)
        {
            GameObject go = new GameObject($"AmbientParticles_{type}");
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = ps.main;

            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = ps.emission;
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(80f, 20f, 1f);

            switch (type)
            {
                case ParticleAmbientType.Embers:
                case ParticleAmbientType.Sparks:
                    ConfigureEmbersParticles(main, emission);
                    break;
                case ParticleAmbientType.Fog:
                    ConfigureFogParticles(main, emission);
                    break;
                case ParticleAmbientType.Rain:
                    ConfigureRainParticles(main, emission);
                    break;
                case ParticleAmbientType.Dust:
                case ParticleAmbientType.Ash:
                    ConfigureDustParticles(main, emission);
                    break;
                case ParticleAmbientType.Snow:
                    ConfigureSnowParticles(main, emission);
                    break;
                case ParticleAmbientType.LavaDrops:
                    ConfigureLavaParticles(main, emission);
                    break;
                default:
                    ConfigureEmbersParticles(main, emission);
                    break;
            }

            ps.Play();
            return go;
        }

        // ── Particle configuration helpers ────────────────────────────────────

        private static void ConfigureEmbersParticles(
            ParticleSystem.MainModule main, ParticleSystem.EmissionModule emission)
        {
            main.startLifetime = 3f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 1.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.5f, 0f, 0.8f), new Color(1f, 0.9f, 0.1f, 0.9f));
            main.gravityModifier = -0.1f;
            emission.rateOverTime = 60f;  // 40 × 1.5 — Ori density
        }

        private static void ConfigureFogParticles(
            ParticleSystem.MainModule main, ParticleSystem.EmissionModule emission)
        {
            main.startLifetime = 8f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.3f);
            main.startSize = new ParticleSystem.MinMaxCurve(2f, 6f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.6f, 0.7f, 0.8f, 0.08f), new Color(0.8f, 0.85f, 0.9f, 0.12f));
            main.gravityModifier = 0f;
            emission.rateOverTime = 8f;  // 5 × 1.5 — Ori density
        }

        private static void ConfigureRainParticles(
            ParticleSystem.MainModule main, ParticleSystem.EmissionModule emission)
        {
            main.startLifetime = 1f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(8f, 12f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.05f);
            main.startColor = new Color(0.5f, 0.6f, 0.9f, 0.6f);
            main.gravityModifier = 1.5f;
            main.startRotation = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
            emission.rateOverTime = 300f;  // 200 × 1.5 — Ori density
        }

        private static void ConfigureDustParticles(
            ParticleSystem.MainModule main, ParticleSystem.EmissionModule emission)
        {
            main.startLifetime = 6f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.2f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.7f, 0.6f, 0.4f, 0.3f), new Color(0.9f, 0.8f, 0.6f, 0.5f));
            main.gravityModifier = -0.05f;
            emission.rateOverTime = 22f;  // 15 × 1.5 — Ori density
        }

        private static void ConfigureSnowParticles(
            ParticleSystem.MainModule main, ParticleSystem.EmissionModule emission)
        {
            main.startLifetime = 5f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
            main.startColor = new Color(0.9f, 0.95f, 1f, 0.8f);
            main.gravityModifier = 0.3f;
            emission.rateOverTime = 90f;  // 60 × 1.5 — Ori density
        }

        private static void ConfigureLavaParticles(
            ParticleSystem.MainModule main, ParticleSystem.EmissionModule emission)
        {
            main.startLifetime = 1.5f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.2f, 0f, 1f), new Color(1f, 0.6f, 0f, 1f));
            main.gravityModifier = 2f;
            emission.rateOverTime = 30f;  // 20 × 1.5 — Ori density
        }

        // ── Lighting ──────────────────────────────────────────────────────────

        private void ApplyAmbientLighting(ZoneData data)
        {
            RenderSettings.ambientLight = data.ambientLightColor;
            RenderSettings.fogColor     = data.fogColor;
            RenderSettings.fogDensity   = data.fogDensity;
            RenderSettings.fog          = data.fogDensity > 0f;

            if (postProcessVolume != null &&
                postProcessVolume.profile.TryGet(out ColorAdjustments ca))
            {
                // Map zone index to a mild hue shift
                int idx = (int)_currentZone;
                ca.colorFilter.value = Color.Lerp(
                    Color.white, data.ambientLightColor, 0.3f);
            }
        }

        // =========================================================================
        //  Helpers
        // =========================================================================

        private static Sprite CreateSolidSprite(Color color, int width, int height)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 1f);
        }

        // =========================================================================
        //  Zone table (10 zones, 5+ layers each)
        // =========================================================================

        private static ZoneData[] BuildZoneTable()
        {
            return new ZoneData[]
            {
                // Zone 0 – Ashfields
                new ZoneData
                {
                    name = "Ashfields",
                    colorPalette = new[] {
                        new Color(0.22f,0.14f,0.10f),
                        new Color(0.45f,0.28f,0.18f),
                        new Color(0.68f,0.40f,0.20f),
                        new Color(0.90f,0.55f,0.22f),
                        new Color(1.00f,0.75f,0.35f)
                    },
                    ambientParticleType = ParticleAmbientType.Embers,
                    ambientLightColor   = new Color(1.0f, 0.65f, 0.35f),
                    fogColor            = new Color(0.5f, 0.3f, 0.15f),
                    fogDensity          = 0.02f,
                    parallaxLayers = new ParallaxLayerData[]
                    {
                        new ParallaxLayerData { color=new Color(0.10f,0.06f,0.04f,1f), scrollSpeed=0.05f, heightOffset=8f,  depth=-10f },
                        new ParallaxLayerData { color=new Color(0.18f,0.10f,0.06f,1f), scrollSpeed=0.10f, heightOffset=5f,  depth=-8f  },
                        new ParallaxLayerData { color=new Color(0.28f,0.16f,0.09f,1f), scrollSpeed=0.20f, heightOffset=3f,  depth=-6f  },
                        new ParallaxLayerData { color=new Color(0.40f,0.24f,0.12f,1f), scrollSpeed=0.35f, heightOffset=1f,  depth=-4f  },
                        new ParallaxLayerData { color=new Color(0.55f,0.33f,0.16f,1f), scrollSpeed=0.50f, heightOffset=0f,  depth=-2f  },
                        new ParallaxLayerData { color=new Color(0.65f,0.40f,0.20f,1f), scrollSpeed=0.70f, heightOffset=-1f, depth=-1f  },
                    }
                },

                // Zone 1 – EmberCrypts
                new ZoneData
                {
                    name = "EmberCrypts",
                    colorPalette = new[] {
                        new Color(0.08f,0.05f,0.08f),
                        new Color(0.20f,0.10f,0.18f),
                        new Color(0.38f,0.15f,0.28f),
                        new Color(0.65f,0.22f,0.35f),
                        new Color(0.90f,0.40f,0.45f)
                    },
                    ambientParticleType = ParticleAmbientType.Sparks,
                    ambientLightColor   = new Color(0.8f, 0.3f, 0.4f),
                    fogColor            = new Color(0.2f, 0.05f, 0.1f),
                    fogDensity          = 0.04f,
                    parallaxLayers = new ParallaxLayerData[]
                    {
                        new ParallaxLayerData { color=new Color(0.04f,0.02f,0.04f,1f), scrollSpeed=0.05f, heightOffset=8f,  depth=-10f },
                        new ParallaxLayerData { color=new Color(0.09f,0.04f,0.09f,1f), scrollSpeed=0.12f, heightOffset=5f,  depth=-8f  },
                        new ParallaxLayerData { color=new Color(0.14f,0.06f,0.14f,1f), scrollSpeed=0.22f, heightOffset=3f,  depth=-6f  },
                        new ParallaxLayerData { color=new Color(0.22f,0.09f,0.20f,1f), scrollSpeed=0.38f, heightOffset=1f,  depth=-4f  },
                        new ParallaxLayerData { color=new Color(0.30f,0.12f,0.25f,1f), scrollSpeed=0.55f, heightOffset=0f,  depth=-2f  },
                        new ParallaxLayerData { color=new Color(0.40f,0.16f,0.30f,1f), scrollSpeed=0.72f, heightOffset=-1f, depth=-1f  },
                    }
                },

                // Zone 2 – ShatteredRamparts
                new ZoneData
                {
                    name = "ShatteredRamparts",
                    colorPalette = new[] {
                        new Color(0.15f,0.14f,0.12f),
                        new Color(0.30f,0.28f,0.24f),
                        new Color(0.50f,0.46f,0.38f),
                        new Color(0.68f,0.62f,0.50f),
                        new Color(0.82f,0.76f,0.64f)
                    },
                    ambientParticleType = ParticleAmbientType.Dust,
                    ambientLightColor   = new Color(0.8f, 0.75f, 0.6f),
                    fogColor            = new Color(0.6f, 0.55f, 0.45f),
                    fogDensity          = 0.015f,
                    parallaxLayers = new ParallaxLayerData[]
                    {
                        new ParallaxLayerData { color=new Color(0.10f,0.09f,0.08f,1f), scrollSpeed=0.05f, heightOffset=8f,  depth=-10f },
                        new ParallaxLayerData { color=new Color(0.18f,0.17f,0.14f,1f), scrollSpeed=0.12f, heightOffset=5f,  depth=-8f  },
                        new ParallaxLayerData { color=new Color(0.28f,0.26f,0.22f,1f), scrollSpeed=0.22f, heightOffset=3f,  depth=-6f  },
                        new ParallaxLayerData { color=new Color(0.38f,0.35f,0.30f,1f), scrollSpeed=0.38f, heightOffset=1f,  depth=-4f  },
                        new ParallaxLayerData { color=new Color(0.50f,0.46f,0.38f,1f), scrollSpeed=0.55f, heightOffset=0f,  depth=-2f  },
                        new ParallaxLayerData { color=new Color(0.62f,0.56f,0.46f,1f), scrollSpeed=0.72f, heightOffset=-1f, depth=-1f  },
                    }
                },

                // Zone 3 – SlagPits
                new ZoneData
                {
                    name = "SlagPits",
                    colorPalette = new[] {
                        new Color(0.18f,0.06f,0.02f),
                        new Color(0.45f,0.14f,0.04f),
                        new Color(0.75f,0.28f,0.06f),
                        new Color(1.00f,0.50f,0.10f),
                        new Color(1.00f,0.80f,0.30f)
                    },
                    ambientParticleType = ParticleAmbientType.LavaDrops,
                    ambientLightColor   = new Color(1.0f, 0.4f, 0.1f),
                    fogColor            = new Color(0.6f, 0.15f, 0.0f),
                    fogDensity          = 0.03f,
                    parallaxLayers = new ParallaxLayerData[]
                    {
                        new ParallaxLayerData { color=new Color(0.10f,0.03f,0.01f,1f), scrollSpeed=0.05f, heightOffset=8f,  depth=-10f },
                        new ParallaxLayerData { color=new Color(0.20f,0.06f,0.02f,1f), scrollSpeed=0.12f, heightOffset=5f,  depth=-8f  },
                        new ParallaxLayerData { color=new Color(0.35f,0.11f,0.03f,1f), scrollSpeed=0.22f, heightOffset=3f,  depth=-6f  },
                        new ParallaxLayerData { color=new Color(0.55f,0.18f,0.04f,1f), scrollSpeed=0.38f, heightOffset=1f,  depth=-4f  },
                        new ParallaxLayerData { color=new Color(0.75f,0.28f,0.06f,1f), scrollSpeed=0.55f, heightOffset=0f,  depth=-2f  },
                        new ParallaxLayerData { color=new Color(0.90f,0.38f,0.08f,1f), scrollSpeed=0.72f, heightOffset=-1f, depth=-1f  },
                    }
                },

                // Zone 4 – DrownedCitadel
                new ZoneData
                {
                    name = "DrownedCitadel",
                    colorPalette = new[] {
                        new Color(0.04f,0.08f,0.16f),
                        new Color(0.08f,0.18f,0.34f),
                        new Color(0.12f,0.30f,0.52f),
                        new Color(0.18f,0.44f,0.70f),
                        new Color(0.30f,0.62f,0.88f)
                    },
                    ambientParticleType = ParticleAmbientType.Fog,
                    ambientLightColor   = new Color(0.3f, 0.5f, 0.8f),
                    fogColor            = new Color(0.1f, 0.2f, 0.4f),
                    fogDensity          = 0.05f,
                    parallaxLayers = new ParallaxLayerData[]
                    {
                        new ParallaxLayerData { color=new Color(0.02f,0.04f,0.10f,1f), scrollSpeed=0.05f, heightOffset=8f,  depth=-10f },
                        new ParallaxLayerData { color=new Color(0.04f,0.09f,0.20f,1f), scrollSpeed=0.12f, heightOffset=5f,  depth=-8f  },
                        new ParallaxLayerData { color=new Color(0.07f,0.15f,0.32f,1f), scrollSpeed=0.22f, heightOffset=3f,  depth=-6f  },
                        new ParallaxLayerData { color=new Color(0.10f,0.22f,0.45f,1f), scrollSpeed=0.38f, heightOffset=1f,  depth=-4f  },
                        new ParallaxLayerData { color=new Color(0.14f,0.30f,0.58f,1f), scrollSpeed=0.55f, heightOffset=0f,  depth=-2f  },
                        new ParallaxLayerData { color=new Color(0.18f,0.38f,0.68f,1f), scrollSpeed=0.72f, heightOffset=-1f, depth=-1f  },
                    }
                },

                // Zone 5 – BoneWastes
                new ZoneData
                {
                    name = "BoneWastes",
                    colorPalette = new[] {
                        new Color(0.16f,0.14f,0.10f),
                        new Color(0.38f,0.34f,0.26f),
                        new Color(0.62f,0.56f,0.42f),
                        new Color(0.82f,0.76f,0.60f),
                        new Color(0.96f,0.92f,0.80f)
                    },
                    ambientParticleType = ParticleAmbientType.Ash,
                    ambientLightColor   = new Color(0.9f, 0.85f, 0.7f),
                    fogColor            = new Color(0.75f, 0.70f, 0.55f),
                    fogDensity          = 0.025f,
                    parallaxLayers = new ParallaxLayerData[]
                    {
                        new ParallaxLayerData { color=new Color(0.08f,0.07f,0.05f,1f), scrollSpeed=0.05f, heightOffset=8f,  depth=-10f },
                        new ParallaxLayerData { color=new Color(0.18f,0.16f,0.12f,1f), scrollSpeed=0.12f, heightOffset=5f,  depth=-8f  },
                        new ParallaxLayerData { color=new Color(0.32f,0.28f,0.20f,1f), scrollSpeed=0.22f, heightOffset=3f,  depth=-6f  },
                        new ParallaxLayerData { color=new Color(0.48f,0.43f,0.32f,1f), scrollSpeed=0.38f, heightOffset=1f,  depth=-4f  },
                        new ParallaxLayerData { color=new Color(0.65f,0.58f,0.44f,1f), scrollSpeed=0.55f, heightOffset=0f,  depth=-2f  },
                        new ParallaxLayerData { color=new Color(0.78f,0.70f,0.54f,1f), scrollSpeed=0.72f, heightOffset=-1f, depth=-1f  },
                    }
                },

                // Zone 6 – ObsidianSpire
                new ZoneData
                {
                    name = "ObsidianSpire",
                    colorPalette = new[] {
                        new Color(0.05f,0.05f,0.05f),
                        new Color(0.12f,0.10f,0.14f),
                        new Color(0.20f,0.16f,0.24f),
                        new Color(0.35f,0.28f,0.42f),
                        new Color(0.55f,0.45f,0.65f)
                    },
                    ambientParticleType = ParticleAmbientType.DarkMotes,
                    ambientLightColor   = new Color(0.45f, 0.35f, 0.6f),
                    fogColor            = new Color(0.1f, 0.07f, 0.15f),
                    fogDensity          = 0.04f,
                    parallaxLayers = new ParallaxLayerData[]
                    {
                        new ParallaxLayerData { color=new Color(0.03f,0.02f,0.03f,1f), scrollSpeed=0.05f, heightOffset=8f,  depth=-10f },
                        new ParallaxLayerData { color=new Color(0.06f,0.05f,0.08f,1f), scrollSpeed=0.12f, heightOffset=5f,  depth=-8f  },
                        new ParallaxLayerData { color=new Color(0.10f,0.08f,0.13f,1f), scrollSpeed=0.22f, heightOffset=3f,  depth=-6f  },
                        new ParallaxLayerData { color=new Color(0.16f,0.12f,0.20f,1f), scrollSpeed=0.38f, heightOffset=1f,  depth=-4f  },
                        new ParallaxLayerData { color=new Color(0.22f,0.17f,0.28f,1f), scrollSpeed=0.55f, heightOffset=0f,  depth=-2f  },
                        new ParallaxLayerData { color=new Color(0.30f,0.23f,0.38f,1f), scrollSpeed=0.72f, heightOffset=-1f, depth=-1f  },
                    }
                },

                // Zone 7 – TheWound
                new ZoneData
                {
                    name = "TheWound",
                    colorPalette = new[] {
                        new Color(0.10f,0.02f,0.02f),
                        new Color(0.28f,0.05f,0.05f),
                        new Color(0.55f,0.08f,0.08f),
                        new Color(0.80f,0.12f,0.14f),
                        new Color(1.00f,0.30f,0.20f)
                    },
                    ambientParticleType = ParticleAmbientType.ChaosOrbs,
                    ambientLightColor   = new Color(0.9f, 0.15f, 0.15f),
                    fogColor            = new Color(0.3f, 0.02f, 0.02f),
                    fogDensity          = 0.06f,
                    parallaxLayers = new ParallaxLayerData[]
                    {
                        new ParallaxLayerData { color=new Color(0.05f,0.01f,0.01f,1f), scrollSpeed=0.05f, heightOffset=8f,  depth=-10f },
                        new ParallaxLayerData { color=new Color(0.12f,0.02f,0.02f,1f), scrollSpeed=0.12f, heightOffset=5f,  depth=-8f  },
                        new ParallaxLayerData { color=new Color(0.22f,0.04f,0.04f,1f), scrollSpeed=0.22f, heightOffset=3f,  depth=-6f  },
                        new ParallaxLayerData { color=new Color(0.36f,0.06f,0.06f,1f), scrollSpeed=0.38f, heightOffset=1f,  depth=-4f  },
                        new ParallaxLayerData { color=new Color(0.50f,0.08f,0.08f,1f), scrollSpeed=0.55f, heightOffset=0f,  depth=-2f  },
                        new ParallaxLayerData { color=new Color(0.65f,0.10f,0.10f,1f), scrollSpeed=0.72f, heightOffset=-1f, depth=-1f  },
                    }
                },

                // Zone 8 – DragonsApproach
                new ZoneData
                {
                    name = "DragonsApproach",
                    colorPalette = new[] {
                        new Color(0.12f,0.08f,0.04f),
                        new Color(0.35f,0.20f,0.08f),
                        new Color(0.65f,0.38f,0.14f),
                        new Color(0.90f,0.60f,0.22f),
                        new Color(1.00f,0.85f,0.50f)
                    },
                    ambientParticleType = ParticleAmbientType.Embers,
                    ambientLightColor   = new Color(1.0f, 0.55f, 0.15f),
                    fogColor            = new Color(0.5f, 0.22f, 0.05f),
                    fogDensity          = 0.025f,
                    parallaxLayers = new ParallaxLayerData[]
                    {
                        new ParallaxLayerData { color=new Color(0.06f,0.04f,0.02f,1f), scrollSpeed=0.05f, heightOffset=8f,  depth=-10f },
                        new ParallaxLayerData { color=new Color(0.14f,0.08f,0.03f,1f), scrollSpeed=0.12f, heightOffset=5f,  depth=-8f  },
                        new ParallaxLayerData { color=new Color(0.26f,0.15f,0.05f,1f), scrollSpeed=0.22f, heightOffset=3f,  depth=-6f  },
                        new ParallaxLayerData { color=new Color(0.42f,0.24f,0.09f,1f), scrollSpeed=0.38f, heightOffset=1f,  depth=-4f  },
                        new ParallaxLayerData { color=new Color(0.60f,0.35f,0.13f,1f), scrollSpeed=0.55f, heightOffset=0f,  depth=-2f  },
                        new ParallaxLayerData { color=new Color(0.78f,0.46f,0.18f,1f), scrollSpeed=0.72f, heightOffset=-1f, depth=-1f  },
                        new ParallaxLayerData { color=new Color(0.92f,0.58f,0.22f,1f), scrollSpeed=0.88f, heightOffset=-2f, depth=-0.5f},
                    }
                },

                // Zone 9 – ThroneOfAsh
                new ZoneData
                {
                    name = "ThroneOfAsh",
                    colorPalette = new[] {
                        new Color(0.02f,0.02f,0.02f),
                        new Color(0.08f,0.06f,0.06f),
                        new Color(0.18f,0.12f,0.10f),
                        new Color(0.35f,0.22f,0.16f),
                        new Color(0.60f,0.38f,0.24f)
                    },
                    ambientParticleType = ParticleAmbientType.Ash,
                    ambientLightColor   = new Color(0.55f, 0.30f, 0.20f),
                    fogColor            = new Color(0.15f, 0.08f, 0.05f),
                    fogDensity          = 0.05f,
                    parallaxLayers = new ParallaxLayerData[]
                    {
                        new ParallaxLayerData { color=new Color(0.01f,0.01f,0.01f,1f), scrollSpeed=0.05f, heightOffset=8f,  depth=-10f },
                        new ParallaxLayerData { color=new Color(0.04f,0.03f,0.03f,1f), scrollSpeed=0.12f, heightOffset=5f,  depth=-8f  },
                        new ParallaxLayerData { color=new Color(0.08f,0.06f,0.05f,1f), scrollSpeed=0.22f, heightOffset=3f,  depth=-6f  },
                        new ParallaxLayerData { color=new Color(0.14f,0.10f,0.08f,1f), scrollSpeed=0.38f, heightOffset=1f,  depth=-4f  },
                        new ParallaxLayerData { color=new Color(0.22f,0.15f,0.12f,1f), scrollSpeed=0.55f, heightOffset=0f,  depth=-2f  },
                        new ParallaxLayerData { color=new Color(0.32f,0.20f,0.15f,1f), scrollSpeed=0.72f, heightOffset=-1f, depth=-1f  },
                        new ParallaxLayerData { color=new Color(0.45f,0.28f,0.20f,1f), scrollSpeed=0.88f, heightOffset=-2f, depth=-0.5f},
                    }
                },
            };
        }
    }
}
