using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

namespace LastEmberKnight
{
    /// <summary>
    /// GameBootstrap is the first MonoBehaviour that runs in the game.
    /// It instantiates all singleton managers, sets up post-processing,
    /// configures the render pipeline, and starts the game loop.
    ///
    /// Attach to a "Bootstrap" GameObject in the Main scene.
    /// Mark it to execute before other scripts in Project Settings > Script Execution Order.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class GameBootstrap : MonoBehaviour
    {
        [Header("Manager Prefabs (optional — created at runtime if null)")]
        [SerializeField] private GameObject gameManagerPrefab;
        [SerializeField] private GameObject audioManagerPrefab;
        [SerializeField] private GameObject particleManagerPrefab;
        [SerializeField] private GameObject uiManagerPrefab;

        [Header("Post Processing")]
        [SerializeField] private VolumeProfile postProcessProfile;

        [Header("Render Settings")]
        [SerializeField] private float targetRenderScale = 1.0f;
        [SerializeField] private bool enableBloom       = true;
        [SerializeField] private bool enableVignette    = true;
        [SerializeField] private bool enableColorGrading = true;
        [SerializeField] private bool enableFilmGrain   = true;

        private static bool _bootstrapped = false;

        private void Awake()
        {
            if (_bootstrapped) { Destroy(gameObject); return; }
            _bootstrapped = true;
            DontDestroyOnLoad(gameObject);

            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount  = 0;

            InitializeManagers();
            ConfigurePostProcessing();
            ConfigureCamera();
            SetApplicationSettings();

            StartCoroutine(DelayedStart());
        }

        private void InitializeManagers()
        {
            // GameManager
            if (GameManager.Instance == null)
            {
                var go = gameManagerPrefab != null
                    ? Instantiate(gameManagerPrefab)
                    : new GameObject("GameManager");
                if (go.GetComponent<GameManager>() == null) go.AddComponent<GameManager>();
                DontDestroyOnLoad(go);
            }

            // AudioManager + ProceduralSFX
            if (AudioManager.Instance == null)
            {
                var go = audioManagerPrefab != null
                    ? Instantiate(audioManagerPrefab)
                    : new GameObject("AudioManager");
                if (go.GetComponent<AudioManager>() == null)  go.AddComponent<AudioManager>();
                if (go.GetComponent<ProceduralSFX>() == null) go.AddComponent<ProceduralSFX>();
                DontDestroyOnLoad(go);
            }

            // DamageSystem
            if (DamageSystem.Instance == null)
            {
                var go = new GameObject("DamageSystem");
                go.AddComponent<DamageSystem>();
                DontDestroyOnLoad(go);
            }

            // SoulMeter
            if (SoulMeter.Instance == null)
            {
                var go = new GameObject("SoulMeter");
                go.AddComponent<SoulMeter>();
                DontDestroyOnLoad(go);
            }

            // SaveSystem
            if (SaveSystem.Instance == null)
            {
                var go = new GameObject("SaveSystem");
                go.AddComponent<SaveSystem>();
                DontDestroyOnLoad(go);
            }

            // UpgradeSystem
            if (UpgradeSystem.Instance == null)
            {
                var go = new GameObject("UpgradeSystem");
                go.AddComponent<UpgradeSystem>();
                DontDestroyOnLoad(go);
            }

            // AchievementTracker
            if (AchievementTracker.Instance == null)
            {
                var go = new GameObject("AchievementTracker");
                go.AddComponent<AchievementTracker>();
                DontDestroyOnLoad(go);
            }

            // FoodSystem
            if (FoodSystem.Instance == null)
            {
                var go = new GameObject("FoodSystem");
                go.AddComponent<FoodSystem>();
                DontDestroyOnLoad(go);
            }

            // ParticleManager
            if (ParticleManager.Instance == null)
            {
                var go = particleManagerPrefab != null
                    ? Instantiate(particleManagerPrefab)
                    : new GameObject("ParticleManager");
                if (go.GetComponent<ParticleManager>() == null) go.AddComponent<ParticleManager>();
                DontDestroyOnLoad(go);
            }

            // ScreenShake
            if (ScreenShake.Instance == null)
            {
                var go = new GameObject("ScreenShake");
                go.AddComponent<ScreenShake>();
                DontDestroyOnLoad(go);
            }

            // LightingManager
            {
                var go = new GameObject("LightingManager");
                go.AddComponent<LightingManager>();
                DontDestroyOnLoad(go);
            }

            // ZoneManager
            if (ZoneManager.Instance == null)
            {
                var go = new GameObject("ZoneManager");
                go.AddComponent<ZoneManager>();
                DontDestroyOnLoad(go);
            }

            // GameLoop
            if (GameLoop.Instance == null)
            {
                var go = new GameObject("GameLoop");
                go.AddComponent<GameLoop>();
                DontDestroyOnLoad(go);
            }

            // HUDManager
            if (uiManagerPrefab != null)
            {
                var go = Instantiate(uiManagerPrefab);
                DontDestroyOnLoad(go);
            }

            Debug.Log("[Bootstrap] All managers initialized.");
        }

        private void ConfigurePostProcessing()
        {
            // Create a Global Volume at runtime if no profile was assigned
            var volumeGO = new GameObject("GlobalPostProcessVolume");
            DontDestroyOnLoad(volumeGO);
            var volume = volumeGO.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 0;

            if (postProcessProfile != null)
            {
                volume.profile = postProcessProfile;
                return;
            }

            // Build the profile in code
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();

            // Bloom — threshold 0.8, intensity 0.5, scatter 0.7
            if (enableBloom)
            {
                var bloom = profile.Add<Bloom>(true);
                bloom.threshold.value = 0.8f;
                bloom.intensity.value = 0.5f;
                bloom.scatter.value   = 0.7f;
                bloom.tint.value      = new Color(1.0f, 0.85f, 0.6f);
                bloom.highQualityFiltering.value = true;
            }

            // Vignette — intensity 0.35
            if (enableVignette)
            {
                var vignette = profile.Add<Vignette>(true);
                vignette.intensity.value  = 0.35f;
                vignette.smoothness.value = 0.5f;
                vignette.rounded.value    = true;
                vignette.color.value      = Color.black;
            }

            // Color Grading — warm shadows, cool highlights, slight desaturation
            if (enableColorGrading)
            {
                var cg = profile.Add<ColorAdjustments>(true);
                cg.colorFilter.value     = new Color(1.0f, 0.95f, 0.88f); // slight warm
                cg.saturation.value      = -12f; // slightly desaturated
                cg.contrast.value        = 8f;
                cg.postExposure.value    = 0.05f;

                var curves = profile.Add<LiftGammaGain>(true);
                curves.lift.value  = new Vector4(0.00f, -0.02f, -0.04f, 0f); // cool blacks
                curves.gain.value  = new Vector4(0.02f,  0.01f,  0.00f, 0f); // warm highlights
            }

            // Film Grain — very subtle
            if (enableFilmGrain)
            {
                var grain = profile.Add<FilmGrain>(true);
                grain.intensity.value   = 0.1f;
                grain.response.value    = 0.8f;
                grain.type.value        = FilmGrainLookup.Thin1;
            }

            // Chromatic Aberration (subtle, spikes during hack events)
            var ca = profile.Add<ChromaticAberration>(true);
            ca.intensity.value = 0.02f;

            // Depth of Field (very mild — mostly in cutscenes)
            // Disabled by default, narrative events enable it

            volume.profile = profile;
            Debug.Log("[Bootstrap] Post-processing configured.");
        }

        private void ConfigureCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var camGO = new GameObject("Main Camera");
                camGO.tag = "MainCamera";
                cam = camGO.AddComponent<Camera>();
                camGO.AddComponent<AudioListener>();
            }

            cam.orthographic = true;
            cam.orthographicSize = 7.5f; // 15 units tall at 1080p
            cam.backgroundColor  = new Color(0.03f, 0.02f, 0.02f);
            cam.nearClipPlane    = -100f;
            cam.farClipPlane     = 100f;

            // URP camera data
            var urpData = cam.GetUniversalAdditionalCameraData();
            if (urpData != null)
            {
                urpData.renderPostProcessing = true;
                urpData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            }

            // Render scale for pixel-art look: render at 480x270, scale to 1920x1080
            // This gives 4x nearest-neighbor pixel-art crispness
            var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urpAsset != null)
                urpAsset.renderScale = targetRenderScale;

            Debug.Log("[Bootstrap] Camera configured.");
        }

        private void SetApplicationSettings()
        {
            // Input settings
            Input.simulateMouseWithTouches = true;

            // Screen
            Screen.SetResolution(1920, 1080, FullScreenMode.FullScreenWindow);

            // Physics 2D — optimize for 2D platformer
            Physics2D.gravity        = new Vector2(0, -20f);
            Physics2D.queriesHitTriggers    = true;
            Physics2D.callbacksOnDisable    = false;

            // Layer setup
            // Layer 8  = "Player"
            // Layer 9  = "Enemy"
            // Layer 10 = "PlayerProjectile"
            // Layer 11 = "EnemyProjectile"
            // Layer 12 = "Ground"
            // Layer 13 = "OneWayPlatform"
            // Enemies don't collide with each other
            Physics2D.IgnoreLayerCollision(9, 9, true);
            // Player projectiles don't hit player
            Physics2D.IgnoreLayerCollision(10, 8, true);
            // Enemy projectiles don't hit enemies
            Physics2D.IgnoreLayerCollision(11, 9, true);
            // Flying enemies don't collide with ground
            // (EmberWraith — set per-instance in EmberWraith.cs)

            Debug.Log("[Bootstrap] Application settings configured.");
        }

        private IEnumerator DelayedStart()
        {
            // Give all Awake() calls one frame to finish
            yield return null;

            // Load save if available, then start the game
            if (SaveSystem.Instance != null && SaveSystem.Instance.HasSave())
            {
                // Title screen will offer "Continue" button
                Debug.Log("[Bootstrap] Save found — title screen will show Continue.");
            }

            GameManager.Instance?.SetInitialState();
            Debug.Log("[Bootstrap] Game started.");
        }

        private void OnDestroy()
        {
            _bootstrapped = false;
        }
    }
}
