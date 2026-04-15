// =============================================================================
//  LightingManager.cs  –  Dynamic 2D lighting controller (URP Light2D)
// =============================================================================
// Manages all runtime 2D lights using Unity URP Light2D components.
// Conditional compilation guards keep the script compilable without the
// Rendering.Universal assembly (builds without errors in non-URP projects).
//
// Responsibilities:
//   SetupZoneLighting(ZoneType)  – configure ambient + directional tint per zone
//   PlayerLight                  – warm orange point light, follows player
//   ShrineLight                  – flame flicker (sine wave on intensity)
//   BossLight                    – dramatic coloured light when boss spawns
//   ZoneAmbient                  – global ambient (intensity 0.3)
//   LightShafts                  – alpha-animated sprites simulating volume light
// =============================================================================
using System.Collections.Generic;
using UnityEngine;

// Guard URP-specific types behind a define that the URP package adds automatically.
#if UNITY_2022_1_OR_NEWER && RENDER_PIPELINE_UNIVERSAL
using UnityEngine.Rendering.Universal;
#endif

namespace LastEmberKnight
{
    [DefaultExecutionOrder(-35)]
    public class LightingManager : MonoBehaviour
    {
        // ─── Singleton ────────────────────────────────────────────────────────────
        public static LightingManager Instance { get; private set; }

        // ─────────────────────────────────────────────────────────────────────────
        #region Zone Color Config

        private static readonly ZoneLightConfig[] ZoneConfigs = new ZoneLightConfig[]
        {
            // Ashfields:        orange/dark, ember glow
            new ZoneLightConfig(ZoneType.Ashfields,
                ambient: new Color(0.22f, 0.12f, 0.05f),
                directional: new Color(0.90f, 0.55f, 0.15f),
                intensity: 0.30f),

            // EmberCrypts:      teal/green bioluminescent
            new ZoneLightConfig(ZoneType.EmberCrypts,
                ambient: new Color(0.03f, 0.14f, 0.12f),
                directional: new Color(0.20f, 0.85f, 0.65f),
                intensity: 0.28f),

            // ShatteredRamparts: warm brown/twilight
            new ZoneLightConfig(ZoneType.ShatteredRamparts,
                ambient: new Color(0.18f, 0.13f, 0.09f),
                directional: new Color(0.80f, 0.60f, 0.35f),
                intensity: 0.32f),

            // SlagPits:         intense red/orange lava glow
            new ZoneLightConfig(ZoneType.SlagPits,
                ambient: new Color(0.28f, 0.06f, 0.02f),
                directional: new Color(1.00f, 0.35f, 0.05f),
                intensity: 0.35f),

            // DrownedCitadel:   blue/teal rain atmosphere
            new ZoneLightConfig(ZoneType.DrownedCitadel,
                ambient: new Color(0.05f, 0.10f, 0.22f),
                directional: new Color(0.30f, 0.55f, 0.90f),
                intensity: 0.25f),

            // BoneWastes:       harsh white desert sun
            new ZoneLightConfig(ZoneType.BoneWastes,
                ambient: new Color(0.30f, 0.28f, 0.22f),
                directional: new Color(0.95f, 0.92f, 0.78f),
                intensity: 0.45f),

            // ObsidianSpire:    purple/blue crystal glow
            new ZoneLightConfig(ZoneType.ObsidianSpire,
                ambient: new Color(0.08f, 0.05f, 0.18f),
                directional: new Color(0.55f, 0.40f, 0.95f),
                intensity: 0.30f),

            // TheWound:         purple/magenta corruption
            new ZoneLightConfig(ZoneType.TheWound,
                ambient: new Color(0.15f, 0.03f, 0.18f),
                directional: new Color(0.85f, 0.20f, 0.90f),
                intensity: 0.28f),

            // DragonsApproach:  dark storm gray/blue
            new ZoneLightConfig(ZoneType.DragonsApproach,
                ambient: new Color(0.08f, 0.09f, 0.14f),
                directional: new Color(0.45f, 0.50f, 0.68f),
                intensity: 0.22f),

            // ThroneOfAsh:      red/black/orange inferno
            new ZoneLightConfig(ZoneType.ThroneOfAsh,
                ambient: new Color(0.25f, 0.04f, 0.01f),
                directional: new Color(1.00f, 0.22f, 0.03f),
                intensity: 0.38f),
        };

        private readonly struct ZoneLightConfig
        {
            public readonly ZoneType Zone;
            public readonly Color    Ambient;
            public readonly Color    Directional;
            public readonly float    Intensity;
            public ZoneLightConfig(ZoneType zone, Color ambient, Color directional, float intensity)
            { Zone = zone; Ambient = ambient; Directional = directional; Intensity = intensity; }
        }

        #endregion

        // ─── Inspector ────────────────────────────────────────────────────────────
        [Header("Player Light")]
        [SerializeField] private Light playerLightTarget;       // assign a child Light/Light2D GO

        [Header("Shrine Lights")]
        [SerializeField] private Light[] shrineLights;

        [Header("Boss Light")]
        [SerializeField] private Light bossLight;

        [Header("Global Ambient")]
        [SerializeField] private float globalAmbientIntensity = 0.30f;

        [Header("Light Shaft Sprites")]
        [SerializeField] private SpriteRenderer[] lightShaftSprites;
        [SerializeField] private float shaftAlphaMin = 0.08f;
        [SerializeField] private float shaftAlphaMax = 0.25f;
        [SerializeField] private float shaftPulseSpeed = 0.4f;

        // ─── Player Light Constants ───────────────────────────────────────────────
        private const float PLAYER_LIGHT_RADIUS    = 4.0f;
        private const float PLAYER_LIGHT_INTENSITY = 0.8f;
        private static readonly Color PLAYER_LIGHT_COLOR = new Color(1.0f, 0.62f, 0.18f);

        // ─── Shrine Flicker ───────────────────────────────────────────────────────
        private const float SHRINE_FLICKER_SPEED  = 3.5f;
        private const float SHRINE_BASE_INTENSITY = 0.7f;
        private const float SHRINE_FLICKER_RANGE  = 0.25f;
        private static readonly Color SHRINE_COLOR = new Color(1.0f, 0.70f, 0.22f);

        // ─── Boss Light ───────────────────────────────────────────────────────────
        private const float BOSS_LIGHT_INTENSITY  = 1.1f;
        private const float BOSS_LIGHT_RADIUS     = 8.0f;

        // ─── Runtime ──────────────────────────────────────────────────────────────
        private Transform    _playerTransform;
        private float        _shaftTimer;
        private ZoneType     _currentZone;

        // ─────────────────────────────────────────────────────────────────────────
        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            ConfigurePlayerLight();
        }

        private void Start()
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag(GameConstants.TagPlayer);
            if (playerObj != null) _playerTransform = playerObj.transform;

            // Default to Ashfields
            SetupZoneLighting(ZoneType.Ashfields);
        }

        private void Update()
        {
            UpdatePlayerLightPosition();
            UpdateShrineFlicker();
            UpdateLightShafts();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        #region Public API

        /// <summary>Configure ambient, directional, and special lights for the given zone.</summary>
        public void SetupZoneLighting(ZoneType zone)
        {
            _currentZone = zone;
            ZoneLightConfig cfg = GetConfig(zone);

            // Unity global ambient
            RenderSettings.ambientLight     = cfg.Ambient;
            RenderSettings.ambientIntensity = cfg.Intensity;

            // Tint the directional light if one is set
            if (RenderSettings.sun != null)
            {
                RenderSettings.sun.color     = cfg.Directional;
                RenderSettings.sun.intensity = cfg.Intensity * 0.8f;
            }

            // Update light shaft tint to match zone directional colour
            if (lightShaftSprites != null)
                foreach (SpriteRenderer sr in lightShaftSprites)
                    if (sr != null) { Color c = cfg.Directional; c.a = shaftAlphaMin; sr.color = c; }
        }

        /// <summary>Register the player transform for the follow-light.</summary>
        public void RegisterPlayer(Transform playerTransform)
        {
            _playerTransform = playerTransform;
        }

        /// <summary>Enable and colour the boss dramatic light.</summary>
        public void ShowBossLight(Color bossColor)
        {
            if (bossLight == null) return;
            bossLight.color     = bossColor;
            bossLight.intensity = BOSS_LIGHT_INTENSITY;
            bossLight.range     = BOSS_LIGHT_RADIUS;
            bossLight.enabled   = true;
        }

        /// <summary>Disable the boss light (boss defeated / not present).</summary>
        public void HideBossLight()
        {
            if (bossLight != null) bossLight.enabled = false;
        }

        /// <summary>Returns the ambient Color for a given zone (used by other systems).</summary>
        public Color GetZoneAmbient(ZoneType zone) => GetConfig(zone).Ambient;

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        #region Private Update

        private void ConfigurePlayerLight()
        {
            if (playerLightTarget == null) return;
            playerLightTarget.color     = PLAYER_LIGHT_COLOR;
            playerLightTarget.intensity = PLAYER_LIGHT_INTENSITY;
            playerLightTarget.range     = PLAYER_LIGHT_RADIUS;
        }

        private void UpdatePlayerLightPosition()
        {
            if (playerLightTarget == null || _playerTransform == null) return;
            playerLightTarget.transform.position = _playerTransform.position;
        }

        private void UpdateShrineFlicker()
        {
            if (shrineLights == null) return;
            float t = Mathf.Sin(Time.time * SHRINE_FLICKER_SPEED);
            // Add a secondary harmonic for more organic flame feel
            float t2 = Mathf.Sin(Time.time * SHRINE_FLICKER_SPEED * 2.3f) * 0.3f;
            float intensity = SHRINE_BASE_INTENSITY + (t + t2) * SHRINE_FLICKER_RANGE;

            foreach (Light sl in shrineLights)
            {
                if (sl == null) continue;
                sl.color     = SHRINE_COLOR;
                sl.intensity = Mathf.Max(0f, intensity);
            }
        }

        private void UpdateLightShafts()
        {
            if (lightShaftSprites == null) return;
            _shaftTimer += Time.deltaTime * shaftPulseSpeed;
            float alpha = Mathf.Lerp(shaftAlphaMin, shaftAlphaMax,
                              (Mathf.Sin(_shaftTimer) + 1f) * 0.5f);

            foreach (SpriteRenderer sr in lightShaftSprites)
            {
                if (sr == null) continue;
                Color c = sr.color; c.a = alpha; sr.color = c;
            }
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        #region Helpers

        private static ZoneLightConfig GetConfig(ZoneType zone)
        {
            foreach (ZoneLightConfig cfg in ZoneConfigs)
                if (cfg.Zone == zone) return cfg;
            return ZoneConfigs[0]; // fallback: Ashfields
        }

        #endregion
    }
}
