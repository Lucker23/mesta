// =============================================================================
//  EmberTrail.cs  –  Player movement VFX: ash + ember trail, body halo, death scatter
// =============================================================================
// The Ember Knight IS ash and fire — his body smoulders constantly.
// Every movement leaves traces of both ember and ash.
//
// Visual layers (Ori 70% / Hollow Knight 30% blend):
//   RunDust    – foot impact dust (grounded movement)
//   RunEmber   – warm ember trail behind the knight
//   RunAsh     – cooler ash that drifts from him as he moves (he IS ash)
//   JumpBurst  – radial ember ring at launch point
//   LandDust   – ground ring on impact
//   DashStreak – golden streak lines + afterimage sprites during dash
//   IdleEmbers – constantly floating embers around the knight at rest
//   AshBodyHalo– near-stationary ash orbit (he smoulders even when still)
//   DeathScatter (one-shot) – called by PlayerHealth.OnDeath
// =============================================================================
using System.Collections;
using UnityEngine;

namespace LastEmberKnight
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class EmberTrail : MonoBehaviour
    {
        // ─── Inspector ────────────────────────────────────────────────────────────
        [Header("Particle System References (auto-created if null)")]
        [SerializeField] private ParticleSystem runDustPS;
        [SerializeField] private ParticleSystem runEmberPS;
        [SerializeField] private ParticleSystem runAshPS;          // NEW: ash layer
        [SerializeField] private ParticleSystem jumpBurstPS;
        [SerializeField] private ParticleSystem landDustPS;
        [SerializeField] private ParticleSystem dashStreakPS;
        [SerializeField] private ParticleSystem idleEmberPS;
        [SerializeField] private ParticleSystem ashBodyHaloPS;     // NEW: body halo

        [Header("Afterimage")]
        [SerializeField] private int   afterimagePoolSize  = 8;
        [SerializeField] private float afterimageAlpha     = 0.45f;
        [SerializeField] private float afterimageDuration  = 0.15f;
        [SerializeField] private float afterimageInterval  = 0.04f;

        // ─── Timing ───────────────────────────────────────────────────────────────
        private const float RUN_DUST_INTERVAL  = 0.15f;
        private const float RUN_EMBER_INTERVAL = 0.08f;
        private const float RUN_ASH_INTERVAL   = 0.22f;   // ash drifts slower

        // ─── Color Palette ────────────────────────────────────────────────────────
        // Embers: hot bright orange
        private static readonly Color EMBER_A      = new Color(1.00f, 0.58f, 0.08f, 1f);
        private static readonly Color EMBER_B      = new Color(1.00f, 0.35f, 0.05f, 1f);
        // Ash: warm gray cooling toward transparent
        private static readonly Color ASH_WARM     = new Color(0.55f, 0.50f, 0.44f, 0.85f);
        private static readonly Color ASH_COOL     = new Color(0.35f, 0.32f, 0.28f, 0.00f);
        // Mid-life ember→ash transition (for idle — embers cool to ash as they rise)
        private static readonly Color EMBER_MID    = new Color(0.75f, 0.40f, 0.15f, 0.7f);
        // Dust / foot impact
        private static readonly Color DUST_COLOR   = new Color(0.60f, 0.50f, 0.38f, 0.80f);
        // Afterimage ghost tint
        private static readonly Color AFTERIMAGE   = new Color(1.00f, 0.45f, 0.05f, 1f);

        // ─── Runtime State ────────────────────────────────────────────────────────
        private SpriteRenderer   _sr;
        private SpriteRenderer[] _afterimagePool;
        private int              _afterimageIndex;
        private Coroutine        _dashAfterimageCoroutine;

        private float _runDustTimer;
        private float _runEmberTimer;
        private float _runAshTimer;
        private bool  _wasGrounded;

        // External state feed – set by PlayerController each frame
        [HideInInspector] public bool  IsGrounded;
        [HideInInspector] public bool  IsRunning;
        [HideInInspector] public bool  IsDashing;
        [HideInInspector] public bool  IsIdle;
        [HideInInspector] public float HorizontalVelocity;

        // ─────────────────────────────────────────────────────────────────────────
        #region Unity Lifecycle

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            BuildParticleSystems();
            BuildAfterimagePook();
        }

        private void Update()
        {
            UpdateRunParticles();
            UpdateIdleAndHalo();
            HandleLandDetection();
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        #region Public Triggers

        public void OnJump()  => TriggerJumpBurst();
        public void OnLand()  => TriggerLandDust();

        public void OnDashStart()
        {
            if (dashStreakPS != null) dashStreakPS.Play();
            if (_dashAfterimageCoroutine != null) StopCoroutine(_dashAfterimageCoroutine);
            _dashAfterimageCoroutine = StartCoroutine(DashAfterimageCoroutine());
        }

        public void OnDashEnd()
        {
            if (dashStreakPS != null) dashStreakPS.Stop();
            if (_dashAfterimageCoroutine != null)
            {
                StopCoroutine(_dashAfterimageCoroutine);
                _dashAfterimageCoroutine = null;
            }
        }

        /// <summary>
        /// Called by PlayerHealth.OnDeath — one-shot 60-particle ash+ember burst.
        /// He scatters into ash and embers as he falls.
        /// </summary>
        public void TriggerDeathScatter()
        {
            // Stop all continuous effects
            if (idleEmberPS    != null) idleEmberPS.Stop();
            if (ashBodyHaloPS  != null) ashBodyHaloPS.Stop();
            if (runEmberPS     != null) runEmberPS.Stop();
            if (runAshPS       != null) runAshPS.Stop();

            StartCoroutine(DeathScatterCoroutine());
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        #region Update Logic

        private void UpdateRunParticles()
        {
            bool running = IsGrounded && IsRunning;
            if (!running)
            {
                _runDustTimer  = 0f;
                _runEmberTimer = 0f;
                _runAshTimer   = 0f;
                return;
            }

            _runDustTimer  += Time.deltaTime;
            _runEmberTimer += Time.deltaTime;
            _runAshTimer   += Time.deltaTime;

            Vector3 feet = transform.position - Vector3.up * 0.3f;

            if (_runDustTimer >= RUN_DUST_INTERVAL)
            {
                _runDustTimer = 0f;
                EmitBurst(runDustPS, feet, 3);
            }
            if (_runEmberTimer >= RUN_EMBER_INTERVAL)
            {
                _runEmberTimer = 0f;
                EmitBurst(runEmberPS, transform.position, 1);
            }
            if (_runAshTimer >= RUN_ASH_INTERVAL)
            {
                _runAshTimer = 0f;
                // Ash trails slightly behind the knight
                Vector3 behind = transform.position - new Vector3(Mathf.Sign(HorizontalVelocity) * 0.2f, 0, 0);
                EmitBurst(runAshPS, behind, 2);
            }
        }

        private void UpdateIdleAndHalo()
        {
            // Idle embers: only when still on ground
            if (idleEmberPS != null)
            {
                bool shouldPlay = IsIdle && IsGrounded;
                if (shouldPlay  && !idleEmberPS.isPlaying) idleEmberPS.Play();
                if (!shouldPlay && idleEmberPS.isPlaying)  idleEmberPS.Stop();
            }

            // Ash body halo: always active (he smoulders constantly)
            if (ashBodyHaloPS != null && !ashBodyHaloPS.isPlaying)
                ashBodyHaloPS.Play();
        }

        private void HandleLandDetection()
        {
            if (!_wasGrounded && IsGrounded) TriggerLandDust();
            _wasGrounded = IsGrounded;
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        #region Effect Triggers

        private void TriggerJumpBurst()
        {
            EmitBurst(jumpBurstPS, transform.position - Vector3.up * 0.4f, 15);
        }

        private void TriggerLandDust()
        {
            EmitBurst(landDustPS, transform.position - Vector3.up * 0.4f, 20);
        }

        private static void EmitBurst(ParticleSystem ps, Vector3 pos, int count)
        {
            if (ps == null) return;
            ps.transform.position = pos;
            ps.Emit(count);
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        #region Death Scatter

        private IEnumerator DeathScatterCoroutine()
        {
            // Build a one-shot death scatter ParticleSystem at the knight's position
            GameObject go = new GameObject("DeathScatterBurst");
            go.transform.position = transform.position;
            ParticleSystem ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.maxParticles    = 80;
            main.loop            = false;
            main.playOnAwake     = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.5f, 1.4f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(2.0f, 5.5f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.04f, 0.18f);
            main.gravityModifier = new ParticleSystem.MinMaxCurve(0.1f);
            // Dual color: orange embers AND warm ash
            main.startColor = new ParticleSystem.MinMaxGradient(EMBER_A, ASH_WARM);

            var sh = ps.shape;
            sh.enabled   = true;
            sh.shapeType = ParticleSystemShapeType.Circle;
            sh.radius    = 0.8f;
            sh.radiusThickness = 1f;

            // Color: embers cool to ash
            var col = ps.colorOverLifetime; col.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(
                new[] {
                    new GradientColorKey(EMBER_A,  0.0f),
                    new GradientColorKey(EMBER_MID, 0.4f),
                    new GradientColorKey(ASH_WARM, 0.75f)
                },
                new[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.8f, 0.5f),
                    new GradientAlphaKey(0f, 1f)
                });
            col.color = new ParticleSystem.MinMaxGradient(g);

            var sot = ps.sizeOverLifetime; sot.enabled = true;
            sot.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

            var em = ps.emission; em.enabled = true;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 60) });

            var rend = go.GetComponent<ParticleSystemRenderer>();
            rend.renderMode = ParticleSystemRenderMode.Billboard;
            rend.material   = new Material(Shader.Find("Particles/Standard Unlit")
                                        ?? Shader.Find("Sprites/Default"));
            ps.Play();

            // Destroy after particles expire
            yield return new WaitForSeconds(1.8f);
            if (go != null) Destroy(go);
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        #region Afterimage Pool

        private void BuildAfterimagePook()
        {
            _afterimagePool = new SpriteRenderer[afterimagePoolSize];
            for (int i = 0; i < afterimagePoolSize; i++)
            {
                GameObject go = new GameObject("Afterimage_" + i);
                go.transform.SetParent(transform.parent);
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sortingOrder = _sr.sortingOrder - 1;
                sr.color        = new Color(AFTERIMAGE.r, AFTERIMAGE.g, AFTERIMAGE.b, 0f);
                go.SetActive(false);
                _afterimagePool[i] = sr;
            }
        }

        private IEnumerator DashAfterimageCoroutine()
        {
            while (IsDashing)
            {
                SpawnAfterimage();
                yield return new WaitForSeconds(afterimageInterval);
            }
        }

        private void SpawnAfterimage()
        {
            SpriteRenderer ghost = _afterimagePool[_afterimageIndex % afterimagePoolSize];
            _afterimageIndex++;
            if (ghost == null) return;

            ghost.transform.position   = transform.position;
            ghost.transform.localScale = transform.localScale;
            ghost.transform.rotation   = transform.rotation;
            ghost.sprite  = _sr.sprite;
            ghost.flipX   = _sr.flipX;
            ghost.color   = new Color(AFTERIMAGE.r, AFTERIMAGE.g, AFTERIMAGE.b, afterimageAlpha);
            ghost.gameObject.SetActive(true);
            StartCoroutine(FadeAfterimage(ghost));
        }

        private IEnumerator FadeAfterimage(SpriteRenderer ghost)
        {
            float elapsed = 0f;
            Color startCol = ghost.color;
            while (elapsed < afterimageDuration)
            {
                elapsed += Time.deltaTime;
                Color c = startCol;
                c.a = startCol.a * (1f - elapsed / afterimageDuration);
                ghost.color = c;
                yield return null;
            }
            ghost.gameObject.SetActive(false);
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        #region ParticleSystem Construction

        private void BuildParticleSystems()
        {
            runDustPS    = runDustPS    != null ? runDustPS    : MakeRunDust();
            runEmberPS   = runEmberPS   != null ? runEmberPS   : MakeRunEmber();
            runAshPS     = runAshPS     != null ? runAshPS     : MakeRunAsh();
            jumpBurstPS  = jumpBurstPS  != null ? jumpBurstPS  : MakeJumpBurst();
            landDustPS   = landDustPS   != null ? landDustPS   : MakeLandDust();
            dashStreakPS = dashStreakPS  != null ? dashStreakPS : MakeDashStreak();
            idleEmberPS  = idleEmberPS  != null ? idleEmberPS  : MakeIdleEmbers();
            ashBodyHaloPS = ashBodyHaloPS != null ? ashBodyHaloPS : MakeAshBodyHalo();
        }

        private ParticleSystem MakePS(string label)
        {
            GameObject go = new GameObject(label);
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop        = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.enabled = false;
            var rend = go.GetComponent<ParticleSystemRenderer>();
            rend.renderMode = ParticleSystemRenderMode.Billboard;
            rend.material   = new Material(Shader.Find("Particles/Standard Unlit")
                                        ?? Shader.Find("Sprites/Default"));
            return ps;
        }

        private static void ApplyGradient(ParticleSystem ps, Color c0, Color c1, Color c2 = default, bool threeStop = false)
        {
            var col = ps.colorOverLifetime; col.enabled = true;
            Gradient g = new Gradient();
            if (threeStop)
            {
                g.SetKeys(
                    new[] { new GradientColorKey(c0, 0f), new GradientColorKey(c1, 0.5f), new GradientColorKey(c2, 1f) },
                    new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) });
            }
            else
            {
                g.SetKeys(
                    new[] { new GradientColorKey(c0, 0f), new GradientColorKey(c1, 1f) },
                    new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            }
            col.color = new ParticleSystem.MinMaxGradient(g);
        }

        private static void ApplySizeOverLifetime(ParticleSystem ps, float s0, float s1)
        {
            var sot = ps.sizeOverLifetime; sot.enabled = true;
            sot.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, s0, 1f, s1));
        }

        // ── Run Dust ──────────────────────────────────────────────────────────────
        private ParticleSystem MakeRunDust()
        {
            var ps   = MakePS("RunDust");
            var main = ps.main;
            main.maxParticles  = 30;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.30f, 0.60f);
            main.startSpeed    = new ParticleSystem.MinMaxCurve(0.5f, 1.2f);
            main.startSize     = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
            main.startColor    = DUST_COLOR;
            main.gravityModifier = new ParticleSystem.MinMaxCurve(-0.1f);
            ApplyGradient(ps, DUST_COLOR, new Color(DUST_COLOR.r, DUST_COLOR.g, DUST_COLOR.b, 0f));
            ApplySizeOverLifetime(ps, 0.6f, 1.4f);
            return ps;
        }

        // ── Run Ember ─────────────────────────────────────────────────────────────
        private ParticleSystem MakeRunEmber()
        {
            var ps   = MakePS("RunEmber");
            var main = ps.main;
            main.maxParticles  = 20;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.30f, 0.70f);
            main.startSpeed    = new ParticleSystem.MinMaxCurve(0.3f, 1.0f);
            main.startSize     = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
            main.startColor    = EMBER_A;
            main.gravityModifier = new ParticleSystem.MinMaxCurve(-0.2f);
            ApplyGradient(ps, EMBER_A, new Color(EMBER_B.r, EMBER_B.g, EMBER_B.b, 0f));
            ApplySizeOverLifetime(ps, 1f, 0f);
            return ps;
        }

        // ── Run Ash (NEW) ─────────────────────────────────────────────────────────
        // Warm gray ash drifts behind the knight as he moves — he IS made of ash
        private ParticleSystem MakeRunAsh()
        {
            var ps   = MakePS("RunAsh");
            var main = ps.main;
            main.maxParticles  = 25;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.40f, 1.00f);
            main.startSpeed    = new ParticleSystem.MinMaxCurve(0.15f, 0.60f);
            main.startSize     = new ParticleSystem.MinMaxCurve(0.04f, 0.10f);
            main.startColor    = ASH_WARM;
            // Gentle upward drift — ash rises
            main.gravityModifier = new ParticleSystem.MinMaxCurve(-0.05f);
            ApplyGradient(ps, ASH_WARM, ASH_COOL);
            ApplySizeOverLifetime(ps, 0.8f, 1.2f);
            return ps;
        }

        // ── Jump Burst ────────────────────────────────────────────────────────────
        private ParticleSystem MakeJumpBurst()
        {
            var ps   = MakePS("JumpBurst");
            var main = ps.main;
            main.maxParticles  = 22;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.70f);
            main.startSpeed    = new ParticleSystem.MinMaxCurve(2f, 4.5f);
            main.startSize     = new ParticleSystem.MinMaxCurve(0.07f, 0.16f);
            main.startColor    = EMBER_A;
            var sh = ps.shape;
            sh.enabled   = true;
            sh.shapeType = ParticleSystemShapeType.Circle;
            sh.radius    = 0.15f;
            sh.radiusThickness = 0f;
            // Three-stop: ember → mid → ash (he launches, embers scatter, ash settles)
            ApplyGradient(ps, EMBER_A, EMBER_MID, ASH_COOL, threeStop: true);
            ApplySizeOverLifetime(ps, 1f, 0f);
            return ps;
        }

        // ── Land Dust ─────────────────────────────────────────────────────────────
        private ParticleSystem MakeLandDust()
        {
            var ps   = MakePS("LandDust");
            var main = ps.main;
            main.maxParticles  = 30;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.65f);
            main.startSpeed    = new ParticleSystem.MinMaxCurve(1f, 3f);
            main.startSize     = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
            main.startColor    = DUST_COLOR;
            var sh = ps.shape;
            sh.enabled   = true;
            sh.shapeType = ParticleSystemShapeType.Circle;
            sh.radius    = 0.35f;
            sh.radiusThickness = 0f;
            ApplyGradient(ps, DUST_COLOR, new Color(DUST_COLOR.r, DUST_COLOR.g, DUST_COLOR.b, 0f));
            ApplySizeOverLifetime(ps, 0.5f, 1.8f);
            return ps;
        }

        // ── Dash Streak ───────────────────────────────────────────────────────────
        private ParticleSystem MakeDashStreak()
        {
            var ps   = MakePS("DashStreak");
            var main = ps.main;
            main.maxParticles  = 50;
            main.loop          = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.10f, 0.25f);
            main.startSpeed    = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
            main.startSize     = new ParticleSystem.MinMaxCurve(0.04f, 0.10f);
            main.startColor    = new Color(1f, 0.82f, 0.32f, 0.95f);
            var em = ps.emission; em.enabled = true;
            em.rateOverTime = new ParticleSystem.MinMaxCurve(75f);
            ApplyGradient(ps, new Color(1f, 0.82f, 0.32f, 0.95f), new Color(1f, 0.45f, 0.05f, 0f));
            ApplySizeOverLifetime(ps, 1f, 0f);
            return ps;
        }

        // ── Idle Embers (AMPLIFIED) ───────────────────────────────────────────────
        // Rate 5→12, radius 0.25→0.45, embers cool to ash mid-life
        private ParticleSystem MakeIdleEmbers()
        {
            var ps   = MakePS("IdleEmbers");
            var main = ps.main;
            main.maxParticles  = 35;
            main.loop          = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.3f);
            main.startSpeed    = new ParticleSystem.MinMaxCurve(0.1f, 0.5f);
            main.startSize     = new ParticleSystem.MinMaxCurve(0.04f, 0.11f);
            main.startColor    = EMBER_A;
            var em = ps.emission; em.enabled = true;
            em.rateOverTime = new ParticleSystem.MinMaxCurve(12f);   // was 5
            var sh = ps.shape;
            sh.enabled   = true;
            sh.shapeType = ParticleSystemShapeType.Circle;
            sh.radius    = 0.45f;  // was 0.25
            // Drift upward
            var vol = ps.velocityOverLifetime; vol.enabled = true;
            vol.y = new ParticleSystem.MinMaxCurve(0.25f, 0.75f);
            // Three-stop: bright ember → cooler mid → ash vanishes
            ApplyGradient(ps, EMBER_A, EMBER_MID, ASH_COOL, threeStop: true);
            ApplySizeOverLifetime(ps, 0.9f, 0f);
            return ps;
        }

        // ── Ash Body Halo (NEW) ───────────────────────────────────────────────────
        // Near-stationary ash particles orbiting the knight's body
        // Creates the "he smoulders even when still" effect from the reference art
        private ParticleSystem MakeAshBodyHalo()
        {
            var ps   = MakePS("AshBodyHalo");
            var main = ps.main;
            main.maxParticles  = 20;
            main.loop          = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 2.5f);
            main.startSpeed    = new ParticleSystem.MinMaxCurve(0.01f, 0.05f);  // almost still
            main.startSize     = new ParticleSystem.MinMaxCurve(0.02f, 0.05f);  // tiny
            main.startColor    = ASH_WARM;
            main.gravityModifier = new ParticleSystem.MinMaxCurve(-0.02f);      // barely rises
            var em = ps.emission; em.enabled = true;
            em.rateOverTime = new ParticleSystem.MinMaxCurve(3f);
            // Orbit tightly around the knight's torso
            var sh = ps.shape;
            sh.enabled   = true;
            sh.shapeType = ParticleSystemShapeType.Circle;
            sh.radius    = 0.30f;
            sh.radiusThickness = 1f;  // whole disc, not just ring
            ApplyGradient(ps, ASH_WARM, ASH_COOL);
            ApplySizeOverLifetime(ps, 0.7f, 0f);
            return ps;
        }

        #endregion
    }
}
