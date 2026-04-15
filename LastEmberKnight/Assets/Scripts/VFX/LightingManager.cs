// =============================================================================
//  LightingManager.cs  –  Dynamic 2D lighting controller (URP Light2D)
// =============================================================================
// Ori × Hollow Knight blend:
//   • Everything is lit from WITHIN — embers, eyes, soul, magic.
//   • Player casts wide warm orange light (he IS the last ember).
//   • Soul motes (surviving embers of the old empire) float in every zone.
//   • Shrines are sacred — intense sacred flicker.
//   • Zone narrative arc: burned homeland (dark) → Dragon's domain (furnace glow).
// =============================================================================
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

        // Zone narrative arc:
        //   Zones 0-2  (burned homeland)     — very dark, heavy ember glow
        //   Zones 3-5  (enemy depths)        — slightly less dark, unique tints
        //   Zones 6-8  (Dragon's approach)   — warming orange/red, heat building
        //   Zone 9     (Throne of Ash)       — dim but intense point lights everywhere
        private static readonly ZoneLightConfig[] ZoneConfigs = new ZoneLightConfig[]
        {
            // Zone 0  Ashfields — burned homeland, orange cinders in darkness
            new ZoneLightConfig(ZoneType.Ashfields,
                ambient:     new Color(0.18f, 0.09f, 0.04f),
                directional: new Color(0.92f, 0.58f, 0.16f),
                intensity:   0.28f),

            // Zone 1  Ember Crypts — teal bioluminescent dead keep watch
            new ZoneLightConfig(ZoneType.EmberCrypts,
                ambient:     new Color(0.02f, 0.12f, 0.10f),
                directional: new Color(0.18f, 0.88f, 0.68f),
                intensity:   0.25f),

            // Zone 2  Shattered Ramparts — twilight over his ruined kingdom
            new ZoneLightConfig(ZoneType.ShatteredRamparts,
                ambient:     new Color(0.15f, 0.10f, 0.07f),
                directional: new Color(0.82f, 0.62f, 0.36f),
                intensity:   0.30f),

            // Zone 3  Slag Pits — Dragon's fire runs deep (intense lava glow)
            new ZoneLightConfig(ZoneType.SlagPits,
                ambient:     new Color(0.32f, 0.07f, 0.02f),
                directional: new Color(1.00f, 0.38f, 0.05f),
                intensity:   0.38f),

            // Zone 4  Drowned Citadel — cold water, cold war, cold blue
            new ZoneLightConfig(ZoneType.DrownedCitadel,
                ambient:     new Color(0.04f, 0.09f, 0.22f),
                directional: new Color(0.28f, 0.55f, 0.95f),
                intensity:   0.24f),

            // Zone 5  Bone Wastes — harsh bleached desert; every bone was a soldier
            new ZoneLightConfig(ZoneType.BoneWastes,
                ambient:     new Color(0.28f, 0.26f, 0.20f),
                directional: new Color(0.98f, 0.94f, 0.80f),
                intensity:   0.44f),

            // Zone 6  Obsidian Spire — crystal purple, Drakar's lieutenants entombed
            new ZoneLightConfig(ZoneType.ObsidianSpire,
                ambient:     new Color(0.07f, 0.04f, 0.18f),
                directional: new Color(0.58f, 0.42f, 0.98f),
                intensity:   0.30f),

            // Zone 7  The Wound — reality torn; corruption pulses
            new ZoneLightConfig(ZoneType.TheWound,
                ambient:     new Color(0.14f, 0.02f, 0.17f),
                directional: new Color(0.88f, 0.20f, 0.92f),
                intensity:   0.27f),

            // Zone 8  Dragon's Approach — storm dark, heat building, almost there
            new ZoneLightConfig(ZoneType.DragonsApproach,
                ambient:     new Color(0.10f, 0.08f, 0.05f),
                directional: new Color(0.88f, 0.52f, 0.18f),
                intensity:   0.32f),

            // Zone 9  Throne of Ash — furnace. Dim ambient but intense point lights.
            new ZoneLightConfig(ZoneType.ThroneOfAsh,
                ambient:     new Color(0.22f, 0.03f, 0.01f),
                directional: new Color(1.00f, 0.24f, 0.03f),
                intensity:   0.28f),   // dim ambient — the glow comes from POINT LIGHTS
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
        [SerializeField] private Light playerLightTarget;

        [Header("Shrine Lights")]
        [SerializeField] private Light[] shrineLights;

        [Header("Boss Light")]
        [SerializeField] private Light bossLight;

        [Header("Light Shaft Sprites")]
        [SerializeField] private SpriteRenderer[] lightShaftSprites;
        [SerializeField] private float shaftAlphaMin   = 0.08f;
        [SerializeField] private float shaftAlphaMax   = 0.30f;
        [SerializeField] private float shaftPulseSpeed = 0.4f;

        [Header("Soul Motes")]
        [SerializeField] private int   soulMoteCount      = 8;
        [SerializeField] private float soulMoteAreaRadius  = 12f;

        // ─── Player Light — amplified (he lights the darkness) ────────────────────
        private const float PLAYER_LIGHT_RADIUS    = 5.5f;   // was 4.0
        private const float PLAYER_LIGHT_INTENSITY = 1.2f;   // was 0.8
        private static readonly Color PLAYER_LIGHT_COLOR = new Color(1.0f, 0.62f, 0.18f);

        // ─── Shrine Flicker — sacred, not decorative ──────────────────────────────
        private const float SHRINE_FLICKER_SPEED  = 3.5f;
        private const float SHRINE_BASE_INTENSITY = 1.1f;   // was 0.7
        private const float SHRINE_FLICKER_RANGE  = 0.40f;  // was 0.25
        private static readonly Color SHRINE_COLOR = new Color(1.0f, 0.72f, 0.24f);

        // ─── Boss Light ───────────────────────────────────────────────────────────
        private const float BOSS_LIGHT_INTENSITY = 1.2f;
        private const float BOSS_LIGHT_RADIUS    = 9.0f;

        // ─── Soul Mote colors (surviving embers of the old empire) ────────────────
        private static readonly Color SOUL_MOTE_ORANGE = new Color(1.0f, 0.55f, 0.12f);
        private static readonly Color SOUL_MOTE_TEAL   = new Color(0.20f, 0.85f, 0.70f);

        // ─── Runtime ──────────────────────────────────────────────────────────────
        private Transform _playerTransform;
        private float     _shaftTimer;
        private ZoneType  _currentZone;
        private readonly List<SoulMoteLight> _soulMotes = new List<SoulMoteLight>();

        // Soul mote data
        private class SoulMoteLight
        {
            public Light light;
            public Vector3 origin;
            public float   phaseX;     // sine phase for drift X
            public float   phaseY;     // sine phase for drift Y
            public float   phaseI;     // sine phase for intensity flicker
            public float   driftAmp;   // how far it drifts
            public float   baseIntensity;
        }

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
            GameObject playerObj = GameObject.FindGameObjectWithTag(GameConstants.PlayerTag);
            if (playerObj != null) _playerTransform = playerObj.transform;
            SetupZoneLighting(ZoneType.Ashfields);
        }

        private void Update()
        {
            UpdatePlayerLightPosition();
            UpdateShrineFlicker();
            UpdateLightShafts();
            UpdateSoulMotes();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        #region Public API

        public void SetupZoneLighting(ZoneType zone)
        {
            _currentZone = zone;
            ZoneLightConfig cfg = GetConfig(zone);

            RenderSettings.ambientLight     = cfg.Ambient;
            RenderSettings.ambientIntensity = cfg.Intensity;

            if (RenderSettings.sun != null)
            {
                RenderSettings.sun.color     = cfg.Directional;
                RenderSettings.sun.intensity = cfg.Intensity * 0.8f;
            }

            if (lightShaftSprites != null)
                foreach (SpriteRenderer sr in lightShaftSprites)
                    if (sr != null) { Color c = cfg.Directional; c.a = shaftAlphaMin; sr.color = c; }

            // Spawn soul motes for this zone
            SpawnSoulMotes(zone);
        }

        public void RegisterPlayer(Transform playerTransform)
        {
            _playerTransform = playerTransform;
        }

        public void ShowBossLight(Color bossColor)
        {
            if (bossLight == null) return;
            bossLight.color     = bossColor;
            bossLight.intensity = BOSS_LIGHT_INTENSITY;
            bossLight.range     = BOSS_LIGHT_RADIUS;
            bossLight.enabled   = true;
        }

        public void HideBossLight()
        {
            if (bossLight != null) bossLight.enabled = false;
        }

        public Color GetZoneAmbient(ZoneType zone) => GetConfig(zone).Ambient;

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        #region Soul Motes (Ori-style ambient light points)

        // Soul motes are tiny floating lights — surviving embers of the old empire.
        // They drift in gentle sine-wave paths and flicker softly.
        private void SpawnSoulMotes(ZoneType zone)
        {
            // Destroy old motes
            foreach (SoulMoteLight m in _soulMotes)
                if (m.light != null) Destroy(m.light.gameObject);
            _soulMotes.Clear();

            // Zone-specific mote color
            bool useTeal = zone == ZoneType.EmberCrypts || zone == ZoneType.DrownedCitadel
                                                        || zone == ZoneType.ObsidianSpire;
            Color moteColor = useTeal ? SOUL_MOTE_TEAL : SOUL_MOTE_ORANGE;

            // In TheWound, motes are corruption purple
            if (zone == ZoneType.TheWound)
                moteColor = new Color(0.75f, 0.20f, 0.90f);

            Vector3 camPos = _playerTransform != null ? _playerTransform.position : Vector3.zero;

            for (int i = 0; i < soulMoteCount; i++)
            {
                GameObject go = new GameObject("SoulMote_" + i);
                go.transform.SetParent(transform);
                Light lt = go.AddComponent<Light>();
                lt.type      = LightType.Point;
                lt.color     = moteColor;
                lt.intensity = Random.Range(0.15f, 0.30f);
                lt.range     = Random.Range(1.0f, 2.0f);
                lt.shadows   = LightShadows.None;

                // Scatter around camera position at zone entry
                float angle  = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float dist   = Random.Range(3f, soulMoteAreaRadius);
                Vector3 origin = camPos + new Vector3(
                    Mathf.Cos(angle) * dist,
                    Random.Range(-4f, 4f),
                    0f);
                go.transform.position = origin;

                _soulMotes.Add(new SoulMoteLight
                {
                    light         = lt,
                    origin        = origin,
                    phaseX        = Random.Range(0f, Mathf.PI * 2f),
                    phaseY        = Random.Range(0f, Mathf.PI * 2f),
                    phaseI        = Random.Range(0f, Mathf.PI * 2f),
                    driftAmp      = Random.Range(0.3f, 1.2f),
                    baseIntensity = lt.intensity,
                });
            }
        }

        private void UpdateSoulMotes()
        {
            if (_soulMotes.Count == 0) return;
            float t = Time.time;
            foreach (SoulMoteLight m in _soulMotes)
            {
                if (m.light == null) continue;
                // Gentle sine-wave drift
                float driftX = Mathf.Sin(t * 0.4f + m.phaseX) * m.driftAmp;
                float driftY = Mathf.Sin(t * 0.3f + m.phaseY) * m.driftAmp * 0.6f;
                m.light.transform.position = m.origin + new Vector3(driftX, driftY, 0f);
                // Soft intensity flicker
                float flicker = (Mathf.Sin(t * 1.2f + m.phaseI) + 1f) * 0.5f;
                m.light.intensity = Mathf.Lerp(m.baseIntensity * 0.6f, m.baseIntensity * 1.3f, flicker);
            }
        }

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
            float t  = Mathf.Sin(Time.time * SHRINE_FLICKER_SPEED);
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
            return ZoneConfigs[0];
        }

        #endregion
    }
}
