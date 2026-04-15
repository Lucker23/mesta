// =============================================================================
//  EmberTrail.cs  –  Player movement VFX: ember trail, dust, afterimages
// =============================================================================
// Attach to the player GameObject. Manages separate ParticleSystems for:
//   Run:   dust puffs at feet every 0.15 s + small ember trail behind
//   Jump:  15-particle outward ring burst at launch point
//   Land:  20-particle ground dust ring
//   Dash:  speed streak lines + afterimage ghost sprites
//   Idle:  gentle floating embers drifting upward (slow loop)
//
// All particles: warm orange, size 0.05-0.15, lifetime 0.3-0.8 s.
// The script auto-creates its own ParticleSystems in Awake if none are assigned.
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
        [SerializeField] private ParticleSystem jumpBurstPS;
        [SerializeField] private ParticleSystem landDustPS;
        [SerializeField] private ParticleSystem dashStreakPS;
        [SerializeField] private ParticleSystem idleEmberPS;

        [Header("Afterimage")]
        [SerializeField] private int   afterimagePoolSize  = 8;
        [SerializeField] private float afterimageAlpha     = 0.45f;
        [SerializeField] private float afterimageDuration  = 0.15f;
        [SerializeField] private float afterimageInterval  = 0.04f;

        // ─── Timing ───────────────────────────────────────────────────────────────
        private const float RUN_DUST_INTERVAL  = 0.15f;
        private const float RUN_EMBER_INTERVAL = 0.08f;

        // ─── Warm Ember Color ─────────────────────────────────────────────────────
        private static readonly Color EMBER_COLOR_A   = new Color(1.00f, 0.58f, 0.08f, 1f);
        private static readonly Color EMBER_COLOR_B   = new Color(1.00f, 0.35f, 0.05f, 1f);
        private static readonly Color DUST_COLOR      = new Color(0.60f, 0.50f, 0.38f, 0.80f);
        private static readonly Color AFTERIMAGE_COLOR = new Color(1.00f, 0.45f, 0.05f, 1f);

        // ─── Runtime State ────────────────────────────────────────────────────────
        private SpriteRenderer   _sr;
        private SpriteRenderer[] _afterimagePool;
        private int              _afterimageIndex;
        private Coroutine        _dashAfterimageCoroutine;

        private float _runDustTimer;
        private float _runEmberTimer;
        private bool  _wasGrounded;
        private bool  _wasRunning;
        private bool  _wasDashing;

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
            UpdateDust();
            UpdateIdleEmbers();
            HandleStateTransitions();
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        #region Public Triggers

        /// <summary>Call when the player leaves the ground (jump start).</summary>
        public void OnJump()
        {
            TriggerJumpBurst();
        }

        /// <summary>Call when the player lands on the ground.</summary>
        public void OnLand()
        {
            TriggerLandDust();
        }

        /// <summary>Call when a dash begins.</summary>
        public void OnDashStart()
        {
            if (dashStreakPS != null) dashStreakPS.Play();
            if (_dashAfterimageCoroutine != null) StopCoroutine(_dashAfterimageCoroutine);
            _dashAfterimageCoroutine = StartCoroutine(DashAfterimageCoroutine());
        }

        /// <summary>Call when a dash ends.</summary>
        public void OnDashEnd()
        {
            if (dashStreakPS != null) dashStreakPS.Stop();
            if (_dashAfterimageCoroutine != null)
            {
                StopCoroutine(_dashAfterimageCoroutine);
                _dashAfterimageCoroutine = null;
            }
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        #region Update Logic

        private void UpdateDust()
        {
            if (!IsGrounded || !IsRunning) { _runDustTimer = 0f; _runEmberTimer = 0f; return; }

            _runDustTimer  += Time.deltaTime;
            _runEmberTimer += Time.deltaTime;

            if (_runDustTimer >= RUN_DUST_INTERVAL)
            {
                _runDustTimer = 0f;
                EmitBurst(runDustPS, transform.position - Vector3.up * 0.3f, 3);
            }

            if (_runEmberTimer >= RUN_EMBER_INTERVAL)
            {
                _runEmberTimer = 0f;
                EmitBurst(runEmberPS, transform.position, 1);
            }
        }

        private void UpdateIdleEmbers()
        {
            if (idleEmberPS == null) return;
            bool shouldPlay = IsIdle && IsGrounded;
            if (shouldPlay && !idleEmberPS.isPlaying) idleEmberPS.Play();
            if (!shouldPlay && idleEmberPS.isPlaying) idleEmberPS.Stop();
        }

        private void HandleStateTransitions()
        {
            // Detect grounded → air transition (jump/fall)
            if (_wasGrounded && !IsGrounded) TriggerJumpBurst();
            // Detect landing
            if (!_wasGrounded && IsGrounded) TriggerLandDust();

            _wasGrounded = IsGrounded;
            _wasRunning  = IsRunning;
            _wasDashing  = IsDashing;
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        #region Effect Triggers

        private void TriggerJumpBurst()
        {
            Vector3 pos = transform.position - Vector3.up * 0.4f;
            EmitBurst(jumpBurstPS, pos, 15);
        }

        private void TriggerLandDust()
        {
            Vector3 pos = transform.position - Vector3.up * 0.4f;
            EmitBurst(landDustPS, pos, 20);
        }

        private static void EmitBurst(ParticleSystem ps, Vector3 pos, int count)
        {
            if (ps == null) return;
            ps.transform.position = pos;
            ps.Emit(count);
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        #region Afterimage Pool

        private void BuildAfterimagePook()
        {
            _afterimagePool  = new SpriteRenderer[afterimagePoolSize];
            for (int i = 0; i < afterimagePoolSize; i++)
            {
                GameObject go = new GameObject("Afterimage_" + i);
                go.transform.SetParent(transform.parent);
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sortingOrder    = _sr.sortingOrder - 1;
                sr.color           = new Color(AFTERIMAGE_COLOR.r, AFTERIMAGE_COLOR.g, AFTERIMAGE_COLOR.b, 0f);
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

            ghost.transform.position = transform.position;
            ghost.transform.localScale = transform.localScale;
            ghost.transform.rotation   = transform.rotation;
            ghost.sprite   = _sr.sprite;
            ghost.flipX    = _sr.flipX;
            ghost.color    = new Color(AFTERIMAGE_COLOR.r, AFTERIMAGE_COLOR.g, AFTERIMAGE_COLOR.b, afterimageAlpha);
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
                float t = elapsed / afterimageDuration;
                Color c = startCol; c.a = startCol.a * (1f - t);
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
            runDustPS   = runDustPS   != null ? runDustPS   : MakeRunDust();
            runEmberPS  = runEmberPS  != null ? runEmberPS  : MakeRunEmber();
            jumpBurstPS = jumpBurstPS != null ? jumpBurstPS : MakeJumpBurst();
            landDustPS  = landDustPS  != null ? landDustPS  : MakeLandDust();
            dashStreakPS = dashStreakPS != null ? dashStreakPS : MakeDashStreak();
            idleEmberPS = idleEmberPS != null ? idleEmberPS : MakeIdleEmbers();
        }

        private ParticleSystem MakePS(string label)
        {
            GameObject go = new GameObject(label);
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop       = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.enabled = false;
            var rend = go.GetComponent<ParticleSystemRenderer>();
            rend.renderMode = ParticleSystemRenderMode.Billboard;
            rend.material = new Material(Shader.Find("Particles/Standard Unlit")
                                      ?? Shader.Find("Sprites/Default"));
            return ps;
        }

        private static void ApplyEmberColors(ParticleSystem ps, Color start, Color end)
        {
            var col = ps.colorOverLifetime; col.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(start, 0f), new GradientColorKey(end, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g);
        }

        private static void ApplySizeOverLifetime(ParticleSystem ps, float start, float end)
        {
            var sot = ps.sizeOverLifetime; sot.enabled = true;
            sot.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, start, 1f, end));
        }

        private ParticleSystem MakeRunDust()
        {
            ParticleSystem ps = MakePS("RunDust");
            var main = ps.main;
            main.maxParticles  = 30;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.30f, 0.60f);
            main.startSpeed    = new ParticleSystem.MinMaxCurve(0.5f, 1.2f);
            main.startSize     = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
            main.startColor    = DUST_COLOR;
            main.gravityModifier = new ParticleSystem.MinMaxCurve(-0.1f);
            ApplyEmberColors(ps, DUST_COLOR, new Color(DUST_COLOR.r, DUST_COLOR.g, DUST_COLOR.b, 0f));
            ApplySizeOverLifetime(ps, 0.6f, 1.4f);
            return ps;
        }

        private ParticleSystem MakeRunEmber()
        {
            ParticleSystem ps = MakePS("RunEmber");
            var main = ps.main;
            main.maxParticles  = 20;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.30f, 0.70f);
            main.startSpeed    = new ParticleSystem.MinMaxCurve(0.3f, 1.0f);
            main.startSize     = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
            main.startColor    = EMBER_COLOR_A;
            main.gravityModifier = new ParticleSystem.MinMaxCurve(-0.2f);
            ApplyEmberColors(ps, EMBER_COLOR_A, new Color(EMBER_COLOR_B.r, EMBER_COLOR_B.g, EMBER_COLOR_B.b, 0f));
            ApplySizeOverLifetime(ps, 1f, 0f);
            return ps;
        }

        private ParticleSystem MakeJumpBurst()
        {
            ParticleSystem ps = MakePS("JumpBurst");
            var main = ps.main;
            main.maxParticles  = 20;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.70f);
            main.startSpeed    = new ParticleSystem.MinMaxCurve(2f, 4f);
            main.startSize     = new ParticleSystem.MinMaxCurve(0.07f, 0.15f);
            main.startColor    = EMBER_COLOR_A;
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 0.15f; sh.radiusThickness = 0f;
            ApplyEmberColors(ps, EMBER_COLOR_A, new Color(EMBER_COLOR_B.r, EMBER_COLOR_B.g, EMBER_COLOR_B.b, 0f));
            ApplySizeOverLifetime(ps, 1f, 0f);
            return ps;
        }

        private ParticleSystem MakeLandDust()
        {
            ParticleSystem ps = MakePS("LandDust");
            var main = ps.main;
            main.maxParticles  = 30;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.65f);
            main.startSpeed    = new ParticleSystem.MinMaxCurve(1f, 3f);
            main.startSize     = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
            main.startColor    = DUST_COLOR;
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 0.35f; sh.radiusThickness = 0f;
            ApplyEmberColors(ps, DUST_COLOR, new Color(DUST_COLOR.r, DUST_COLOR.g, DUST_COLOR.b, 0f));
            ApplySizeOverLifetime(ps, 0.5f, 1.8f);
            return ps;
        }

        private ParticleSystem MakeDashStreak()
        {
            ParticleSystem ps = MakePS("DashStreak");
            var main = ps.main;
            main.maxParticles  = 40;
            main.loop          = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.10f, 0.25f);
            main.startSpeed    = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
            main.startSize     = new ParticleSystem.MinMaxCurve(0.04f, 0.10f);
            main.startColor    = new Color(1f, 0.8f, 0.3f, 0.9f);
            var em = ps.emission; em.enabled = true; em.rateOverTime = new ParticleSystem.MinMaxCurve(60f);
            ApplyEmberColors(ps, new Color(1f, 0.8f, 0.3f, 0.9f), new Color(1f, 0.4f, 0.05f, 0f));
            ApplySizeOverLifetime(ps, 1f, 0f);
            return ps;
        }

        private ParticleSystem MakeIdleEmbers()
        {
            ParticleSystem ps = MakePS("IdleEmbers");
            var main = ps.main;
            main.maxParticles  = 15;
            main.loop          = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.0f);
            main.startSpeed    = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
            main.startSize     = new ParticleSystem.MinMaxCurve(0.04f, 0.10f);
            main.startColor    = EMBER_COLOR_A;
            var em = ps.emission; em.enabled = true; em.rateOverTime = new ParticleSystem.MinMaxCurve(5f);
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 0.25f;

            // Drift upward over lifetime
            var vol = ps.velocityOverLifetime; vol.enabled = true;
            vol.y = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);

            ApplyEmberColors(ps, EMBER_COLOR_A, new Color(EMBER_COLOR_B.r, EMBER_COLOR_B.g, EMBER_COLOR_B.b, 0f));
            ApplySizeOverLifetime(ps, 0.8f, 0f);
            return ps;
        }

        #endregion
    }
}
