// =============================================================================
//  ParticleManager.cs  –  Centralised VFX / particle effect manager
// =============================================================================
// Singleton. Spawns and pools all runtime particle effects via Unity's
// ParticleSystem API. Every effect type has its own object pool.
//
// Public methods:
//   SpawnHitEffect(pos, color)                  – 10-particle colour burst
//   SpawnDustPuff(pos)                          – foot-step dust
//   SpawnEmberBurst(pos, count)                 – fire/ember explosion
//   SpawnSoulAbsorb(pos)                        – soul pickup shimmer
//   SpawnDeathEffect(pos)                       – enemy death smoke + embers
//   SpawnBossPhaseEffect(pos)                   – dramatic boss-phase explosion
//   SpawnLightBurst(pos)                        – Ori-style pure light ring
//   SpawnSlamImpact(pos)                        – ground slam shockwave ring
//   SpawnSoulEssenceDrain(enemyPos, playerPos)  – teal soul arc (enemy→player)
//   SpawnAshBodyIdle(pos)                       – ambient ash smoulder halo
//   SpawnOriMote(pos, zoneColor)                – long-life floating world mote
//
// All effects use color-over-lifetime, size-over-lifetime, and velocity curves.
// =============================================================================
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LastEmberKnight
{
    [DefaultExecutionOrder(-40)]
    public class ParticleManager : MonoBehaviour
    {
        // ─── Singleton ────────────────────────────────────────────────────────────
        public static ParticleManager Instance { get; private set; }

        // ─────────────────────────────────────────────────────────────────────────
        #region Pool Infrastructure

        private class EffectPool
        {
            private readonly Queue<ParticleSystem> _free   = new Queue<ParticleSystem>();
            private readonly List<ParticleSystem>  _active = new List<ParticleSystem>();
            private readonly ParticleSystem        _template;
            private readonly Transform             _parent;

            public EffectPool(ParticleSystem template, Transform parent, int prewarm)
            {
                _template = template;
                _parent   = parent;
                for (int i = 0; i < prewarm; i++) Grow();
            }

            private ParticleSystem Grow()
            {
                ParticleSystem ps = Object.Instantiate(_template, _parent);
                ps.gameObject.SetActive(false);
                _free.Enqueue(ps);
                return ps;
            }

            public ParticleSystem Get(Vector3 position)
            {
                ParticleSystem ps = null;
                while (_free.Count > 0 && ps == null)
                {
                    ps = _free.Dequeue();
                    if (ps == null || ps.gameObject.activeInHierarchy) ps = null;
                }
                if (ps == null) ps = Grow();

                ps.transform.position = position;
                ps.gameObject.SetActive(true);
                ps.Clear();
                ps.Play();
                _active.Add(ps);
                return ps;
            }

            public void Tick()
            {
                for (int i = _active.Count - 1; i >= 0; i--)
                {
                    ParticleSystem ps = _active[i];
                    if (ps == null) { _active.RemoveAt(i); continue; }
                    if (!ps.IsAlive())
                    {
                        ps.Stop();
                        ps.gameObject.SetActive(false);
                        _active.RemoveAt(i);
                        _free.Enqueue(ps);
                    }
                }
            }
        }

        #endregion

        // ─── Inspector: Optional Prefab Overrides ─────────────────────────────────
        [Header("Effect Prefabs (leave null for procedural build)")]
        [SerializeField] private ParticleSystem hitEffectPrefab;
        [SerializeField] private ParticleSystem dustPuffPrefab;
        [SerializeField] private ParticleSystem emberBurstPrefab;
        [SerializeField] private ParticleSystem soulAbsorbPrefab;
        [SerializeField] private ParticleSystem deathEffectPrefab;
        [SerializeField] private ParticleSystem bossPhaseEffectPrefab;
        [SerializeField] private ParticleSystem lightBurstPrefab;
        [SerializeField] private ParticleSystem slamImpactPrefab;

        // ─── Pools ────────────────────────────────────────────────────────────────
        private EffectPool _hitPool;
        private EffectPool _dustPool;
        private EffectPool _emberBurstPool;
        private EffectPool _soulPool;
        private EffectPool _deathPool;
        private EffectPool _bossPhasePool;
        private EffectPool _lightBurstPool;
        private EffectPool _slamPool;

        private Transform  _poolRoot;

        // ─────────────────────────────────────────────────────────────────────────
        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _poolRoot = new GameObject("[ParticlePools]").transform;
            _poolRoot.SetParent(transform);
            BuildAllPools();
        }

        private void Update()
        {
            _hitPool?.Tick();
            _dustPool?.Tick();
            _emberBurstPool?.Tick();
            _soulPool?.Tick();
            _deathPool?.Tick();
            _bossPhasePool?.Tick();
            _lightBurstPool?.Tick();
            _slamPool?.Tick();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        #region Pool Construction

        private void BuildAllPools()
        {
            _hitPool        = MakePool(ref hitEffectPrefab,       BuildHitEffect,       8);
            _dustPool       = MakePool(ref dustPuffPrefab,        BuildDustPuff,        6);
            _emberBurstPool = MakePool(ref emberBurstPrefab,      BuildEmberBurst,      4);
            _soulPool       = MakePool(ref soulAbsorbPrefab,      BuildSoulAbsorb,      4);
            _deathPool      = MakePool(ref deathEffectPrefab,     BuildDeathEffect,     4);
            _bossPhasePool  = MakePool(ref bossPhaseEffectPrefab, BuildBossPhaseEffect, 2);
            _lightBurstPool = MakePool(ref lightBurstPrefab,      BuildLightBurst,      4);
            _slamPool       = MakePool(ref slamImpactPrefab,      BuildSlamImpact,      4);
        }

        private EffectPool MakePool(ref ParticleSystem prefabSlot,
                                    System.Func<ParticleSystem> factory, int prewarm)
        {
            if (prefabSlot == null)
                prefabSlot = factory();

            prefabSlot.gameObject.SetActive(false);
            return new EffectPool(prefabSlot, _poolRoot, prewarm);
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        #region Public API

        /// <summary>10-particle hit spark burst. Pass the colour of the effect.</summary>
        public void SpawnHitEffect(Vector3 pos, Color color)
        {
            ParticleSystem ps = _hitPool?.Get(pos);
            if (ps == null) return;
            var main = ps.main;
            main.startColor = color;
        }

        /// <summary>Foot-step dust puff at player feet.</summary>
        public void SpawnDustPuff(Vector3 pos)
            => _dustPool?.Get(pos);

        /// <summary>Fire/ember explosion with <paramref name="count"/> particles.</summary>
        public void SpawnEmberBurst(Vector3 pos, int count)
        {
            ParticleSystem ps = _emberBurstPool?.Get(pos);
            if (ps == null) return;
            var emission = ps.emission;
            emission.SetBurst(0, new ParticleSystem.Burst(0f, count));
        }

        /// <summary>Soul pickup shimmer (orange/gold drift).</summary>
        public void SpawnSoulAbsorb(Vector3 pos)
            => _soulPool?.Get(pos);

        /// <summary>Enemy death smoke + ember shower.</summary>
        public void SpawnDeathEffect(Vector3 pos)
            => _deathPool?.Get(pos);

        /// <summary>Dramatic boss phase-transition explosion.</summary>
        public void SpawnBossPhaseEffect(Vector3 pos)
            => _bossPhasePool?.Get(pos);

        /// <summary>Ori-style pure white light ring burst.</summary>
        public void SpawnLightBurst(Vector3 pos)
            => _lightBurstPool?.Get(pos);

        /// <summary>Ground slam radial shockwave ring.</summary>
        public void SpawnSlamImpact(Vector3 pos)
            => _slamPool?.Get(pos);

        /// <summary>
        /// 8 teal soul particles arc bezier-style from dead enemy to player.
        /// Called by EnemyBase.OnDeath — soul essence flowing back to the knight.
        /// </summary>
        public void SpawnSoulEssenceDrain(Vector3 enemyPos, Vector3 playerPos)
        {
            StartCoroutine(SoulDrainCoroutine(enemyPos, playerPos));
        }

        /// <summary>
        /// Ambient ash smoulder halo — 3 near-stationary particles orbiting pos.
        /// Creates the "he's still burning even when still" effect.
        /// </summary>
        public void SpawnAshBodyIdle(Vector3 pos)
        {
            GameObject go = new GameObject("AshBodyIdle");
            go.transform.position = pos;
            ParticleSystem ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.maxParticles    = 8;
            main.loop            = false;
            main.playOnAwake     = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(1.5f, 2.5f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(0.01f, 0.05f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.02f, 0.05f);
            main.startColor      = new ParticleSystem.MinMaxGradient(
                new Color(0.55f, 0.50f, 0.44f, 0.80f),
                new Color(1.00f, 0.55f, 0.10f, 0.60f));
            main.gravityModifier = new ParticleSystem.MinMaxCurve(-0.03f);

            var em = ps.emission;
            em.enabled = true;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 3) });

            var sh = ps.shape;
            sh.enabled   = true;
            sh.shapeType = ParticleSystemShapeType.Circle;
            sh.radius    = 0.30f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(
                new[] {
                    new GradientColorKey(new Color(0.55f, 0.50f, 0.44f), 0.0f),
                    new GradientColorKey(new Color(1.00f, 0.55f, 0.10f), 0.5f),
                    new GradientColorKey(new Color(0.20f, 0.18f, 0.15f), 1.0f)
                },
                new[] {
                    new GradientAlphaKey(0.8f, 0.0f),
                    new GradientAlphaKey(0.5f, 0.5f),
                    new GradientAlphaKey(0.0f, 1.0f)
                });
            col.color = new ParticleSystem.MinMaxGradient(g);

            var rend = go.GetComponent<ParticleSystemRenderer>();
            rend.renderMode = ParticleSystemRenderMode.Billboard;
            rend.material   = new Material(Shader.Find("Particles/Standard Unlit")
                                        ?? Shader.Find("Sprites/Default"));

            ps.Play();
            Object.Destroy(go, 3.0f);
        }

        /// <summary>
        /// Long-lifetime Ori-style ambient mote: a surviving ember of the old empire.
        /// Called by ZoneManager on zone build — color matches zone accent.
        /// Lifetime 8-15 s, tiny (0.02-0.04 size), slow upward drift.
        /// </summary>
        public void SpawnOriMote(Vector3 pos, Color zoneColor)
        {
            GameObject go = new GameObject("OriMote");
            go.transform.position = pos;
            ParticleSystem ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.maxParticles    = 4;
            main.loop            = false;
            main.playOnAwake     = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(8.0f, 15.0f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.02f, 0.04f);
            main.startColor      = new Color(zoneColor.r, zoneColor.g, zoneColor.b, 0.75f);
            main.gravityModifier = new ParticleSystem.MinMaxCurve(-0.02f);

            var em = ps.emission;
            em.enabled = true;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 2) });

            var sh = ps.shape;
            sh.enabled   = true;
            sh.shapeType = ParticleSystemShapeType.Circle;
            sh.radius    = 0.5f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(
                new[] {
                    new GradientColorKey(zoneColor, 0.0f),
                    new GradientColorKey(zoneColor, 0.8f),
                    new GradientColorKey(zoneColor, 1.0f)
                },
                new[] {
                    new GradientAlphaKey(0.00f, 0.00f),
                    new GradientAlphaKey(0.75f, 0.10f),
                    new GradientAlphaKey(0.60f, 0.70f),
                    new GradientAlphaKey(0.00f, 1.00f)
                });
            col.color = new ParticleSystem.MinMaxGradient(g);

            var rend = go.GetComponent<ParticleSystemRenderer>();
            rend.renderMode = ParticleSystemRenderMode.Billboard;
            rend.material   = new Material(Shader.Find("Particles/Standard Unlit")
                                        ?? Shader.Find("Sprites/Default"));

            ps.Play();
            Object.Destroy(go, 16.0f);
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        #region Procedural ParticleSystem Builders

        // ─── Shared helpers ───────────────────────────────────────────────────────

        private ParticleSystem NewPS(string label, int maxParticles)
        {
            GameObject go = new GameObject(label);
            go.transform.SetParent(_poolRoot);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.maxParticles    = maxParticles;
            main.loop            = false;
            main.playOnAwake     = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var em = ps.emission;
            em.enabled = false;

            var rend = go.GetComponent<ParticleSystemRenderer>();
            rend.renderMode = ParticleSystemRenderMode.Billboard;
            rend.material   = new Material(Shader.Find("Particles/Standard Unlit")
                                        ?? Shader.Find("Sprites/Default"));
            return ps;
        }

        private static void SetColorOverLifetime(ParticleSystem ps, Color startCol, Color endCol)
        {
            var col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(startCol, 0f), new GradientColorKey(endCol,  1f) },
                new[] { new GradientAlphaKey(1f,        0f), new GradientAlphaKey(0f,       1f) });
            col.color = new ParticleSystem.MinMaxGradient(grad);
        }

        private static void SetSizeOverLifetime(ParticleSystem ps, float startScale, float endScale)
        {
            var sot = ps.sizeOverLifetime;
            sot.enabled = true;
            sot.size    = new ParticleSystem.MinMaxCurve(1f,
                AnimationCurve.EaseInOut(0f, startScale, 1f, endScale));
        }

        private static void SetRadialVelocity(ParticleSystem ps, float speed)
        {
            var vol = ps.velocityOverLifetime;
            vol.enabled = true;
            vol.radial  = new ParticleSystem.MinMaxCurve(speed);
            vol.space   = ParticleSystemSimulationSpace.Local;
        }

        private static void SetBurst(ParticleSystem ps, int count)
        {
            var em = ps.emission;
            em.enabled = true;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, count) });
        }

        private static void SetCircleShape(ParticleSystem ps, float radius, bool ring = false)
        {
            var sh = ps.shape;
            sh.enabled          = true;
            sh.shapeType        = ParticleSystemShapeType.Circle;
            sh.radius           = radius;
            sh.radiusThickness  = ring ? 0f : 1f;
        }

        private static void SetConeShape(ParticleSystem ps, float angle, float radius)
        {
            var sh = ps.shape;
            sh.enabled   = true;
            sh.shapeType = ParticleSystemShapeType.Cone;
            sh.angle     = angle;
            sh.radius    = radius;
        }

        // ─── Hit Effect ───────────────────────────────────────────────────────────
        private ParticleSystem BuildHitEffect()
        {
            ParticleSystem ps = NewPS("HitEffect_tmpl", 20);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.20f, 0.45f);
            main.startSpeed    = new ParticleSystem.MinMaxCurve(2f, 5f);
            main.startSize     = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
            main.startColor    = new Color(1f, 0.6f, 0.1f);
            SetBurst(ps, 10);
            SetCircleShape(ps, 0.1f);
            SetColorOverLifetime(ps, new Color(1f, 0.7f, 0.1f), new Color(0.5f, 0.1f, 0f, 0f));
            SetSizeOverLifetime(ps, 1f, 0f);
            return ps;
        }

        // ─── Dust Puff ────────────────────────────────────────────────────────────
        private ParticleSystem BuildDustPuff()
        {
            ParticleSystem ps = NewPS("DustPuff_tmpl", 12);
            var main = ps.main;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.25f, 0.50f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
            main.startColor      = new Color(0.55f, 0.45f, 0.35f, 0.8f);
            main.gravityModifier = new ParticleSystem.MinMaxCurve(-0.2f);
            SetBurst(ps, 6);
            SetConeShape(ps, 35f, 0.15f);
            SetColorOverLifetime(ps, new Color(0.6f, 0.5f, 0.4f, 0.7f), new Color(0.6f, 0.5f, 0.4f, 0f));
            SetSizeOverLifetime(ps, 0.5f, 1.5f);
            return ps;
        }

        // ─── Ember Burst ──────────────────────────────────────────────────────────
        private ParticleSystem BuildEmberBurst()
        {
            ParticleSystem ps = NewPS("EmberBurst_tmpl", 60);
            var main = ps.main;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.40f, 0.90f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(3f, 8f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
            main.startColor      = new ParticleSystem.MinMaxGradient(new Color(1f, 0.7f, 0.1f), new Color(1f, 0.3f, 0f));
            main.gravityModifier = new ParticleSystem.MinMaxCurve(0.3f);
            SetBurst(ps, 20);
            SetCircleShape(ps, 0.2f);
            SetColorOverLifetime(ps, new Color(1f, 0.6f, 0.1f), new Color(0.4f, 0.05f, 0f, 0f));
            SetSizeOverLifetime(ps, 1f, 0.2f);
            return ps;
        }

        // ─── Soul Absorb ──────────────────────────────────────────────────────────
        private ParticleSystem BuildSoulAbsorb()
        {
            ParticleSystem ps = NewPS("SoulAbsorb_tmpl", 20);
            var main = ps.main;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.50f, 0.90f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(1f, 2.5f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.06f, 0.12f);
            main.startColor      = new Color(0.95f, 0.55f, 0.10f);
            main.gravityModifier = new ParticleSystem.MinMaxCurve(-0.5f);
            SetBurst(ps, 12);
            SetCircleShape(ps, 0.3f);
            SetColorOverLifetime(ps, new Color(1f, 0.8f, 0.2f), new Color(1f, 1f, 1f, 0f));
            SetSizeOverLifetime(ps, 1f, 0f);
            return ps;
        }

        // ─── Death Effect ─────────────────────────────────────────────────────────
        private ParticleSystem BuildDeathEffect()
        {
            ParticleSystem ps = NewPS("DeathEffect_tmpl", 40);
            var main = ps.main;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.50f, 1.20f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(1f, 4f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.08f, 0.25f);
            main.startColor      = new ParticleSystem.MinMaxGradient(
                new Color(0.2f, 0.2f, 0.2f, 0.9f), new Color(0.1f, 0.08f, 0.05f, 0.8f));
            main.gravityModifier = new ParticleSystem.MinMaxCurve(-0.1f);
            SetBurst(ps, 25);
            SetCircleShape(ps, 0.4f);
            SetColorOverLifetime(ps, new Color(0.25f, 0.2f, 0.15f, 0.9f), new Color(0.1f, 0.08f, 0.05f, 0f));
            SetSizeOverLifetime(ps, 1f, 2f);
            return ps;
        }

        // ─── Boss Phase Effect ────────────────────────────────────────────────────
        private ParticleSystem BuildBossPhaseEffect()
        {
            ParticleSystem ps = NewPS("BossPhaseEffect_tmpl", 80);
            var main = ps.main;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.60f, 1.50f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(4f, 12f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.10f, 0.30f);
            main.startColor      = new ParticleSystem.MinMaxGradient(new Color(1f, 0.5f, 0.05f), new Color(1f, 0.8f, 0.2f));
            main.gravityModifier = new ParticleSystem.MinMaxCurve(0.2f);
            SetBurst(ps, 60);
            SetCircleShape(ps, 0.5f);
            SetColorOverLifetime(ps, new Color(1f, 0.6f, 0.1f), new Color(0.4f, 0.05f, 0f, 0f));
            SetSizeOverLifetime(ps, 1f, 0.1f);
            return ps;
        }

        // ─── Light Burst ──────────────────────────────────────────────────────────
        private ParticleSystem BuildLightBurst()
        {
            ParticleSystem ps = NewPS("LightBurst_tmpl", 30);
            var main = ps.main;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.30f, 0.60f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(3f, 6f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
            main.startColor      = Color.white;
            main.gravityModifier = new ParticleSystem.MinMaxCurve(0f);
            SetBurst(ps, 24);
            SetCircleShape(ps, 0.05f, ring: true);
            SetColorOverLifetime(ps, Color.white, new Color(0.9f, 0.9f, 1f, 0f));
            SetSizeOverLifetime(ps, 0.8f, 0f);
            SetRadialVelocity(ps, 2f);
            return ps;
        }

        // ─── Slam Impact ──────────────────────────────────────────────────────────
        private ParticleSystem BuildSlamImpact()
        {
            ParticleSystem ps = NewPS("SlamImpact_tmpl", 40);
            var main = ps.main;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.30f, 0.60f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(2f, 5f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.06f, 0.18f);
            main.startColor      = new ParticleSystem.MinMaxGradient(
                new Color(0.6f, 0.45f, 0.3f), new Color(1f, 0.5f, 0.1f, 0.9f));
            main.gravityModifier = new ParticleSystem.MinMaxCurve(-0.15f);
            SetBurst(ps, 30);
            SetCircleShape(ps, 0.5f, ring: true);
            SetColorOverLifetime(ps, new Color(0.7f, 0.55f, 0.3f, 0.9f), new Color(0.7f, 0.55f, 0.3f, 0f));
            SetSizeOverLifetime(ps, 0.5f, 1.5f);
            SetRadialVelocity(ps, 3f);
            return ps;
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        #region Soul Drain Coroutines

        // 8 individual soul sparks arc one-by-one from enemy toward player
        private IEnumerator SoulDrainCoroutine(Vector3 enemyPos, Vector3 playerPos)
        {
            const int   SOUL_COUNT   = 8;
            const float SOUL_DURATION = 0.6f;
            Color soulColor = new Color(0.40f, 0.90f, 1.00f);

            for (int i = 0; i < SOUL_COUNT; i++)
            {
                yield return new WaitForSeconds(i * 0.04f);

                GameObject go = new GameObject("SoulSpark");
                go.transform.position = enemyPos + new Vector3(
                    Random.Range(-0.30f, 0.30f),
                    Random.Range(-0.20f, 0.20f), 0f);

                ParticleSystem ps = go.AddComponent<ParticleSystem>();
                var main = ps.main;
                main.maxParticles    = 1;
                main.loop            = false;
                main.playOnAwake     = false;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.startLifetime   = SOUL_DURATION;
                main.startSpeed      = 0f;
                main.startSize       = new ParticleSystem.MinMaxCurve(0.06f, 0.10f);
                main.startColor      = soulColor;

                var em = ps.emission;
                em.enabled = true;
                em.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });

                var sh = ps.shape;
                sh.enabled = false;

                var col = ps.colorOverLifetime;
                col.enabled = true;
                Gradient g = new Gradient();
                g.SetKeys(
                    new[] {
                        new GradientColorKey(soulColor, 0f),
                        new GradientColorKey(Color.white, 1f)
                    },
                    new[] {
                        new GradientAlphaKey(1.0f, 0.0f),
                        new GradientAlphaKey(0.0f, 1.0f)
                    });
                col.color = new ParticleSystem.MinMaxGradient(g);

                var rend = go.GetComponent<ParticleSystemRenderer>();
                rend.renderMode = ParticleSystemRenderMode.Billboard;
                rend.material   = new Material(Shader.Find("Particles/Standard Unlit")
                                            ?? Shader.Find("Sprites/Default"));

                ps.Play();
                StartCoroutine(ArcToPlayer(go.transform, go.transform.position,
                    playerPos, SOUL_DURATION));
                Object.Destroy(go, SOUL_DURATION + 0.15f);
            }
        }

        // Moves a transform along a quadratic bezier arc from 'from' to 'to' over duration.
        // The arc peaks above the midpoint, giving the soul sparks a looping flight path.
        private static IEnumerator ArcToPlayer(Transform t, Vector3 from, Vector3 to, float duration)
        {
            if (t == null) yield break;
            Vector3 mid = (from + to) * 0.5f + Vector3.up * 0.8f;
            float elapsed = 0f;
            while (elapsed < duration && t != null)
            {
                elapsed += Time.deltaTime;
                float pct = Mathf.Clamp01(elapsed / duration);
                Vector3 p1 = Vector3.Lerp(from, mid, pct);
                Vector3 p2 = Vector3.Lerp(mid,  to,  pct);
                t.position = Vector3.Lerp(p1, p2, pct);
                yield return null;
            }
        }

        #endregion
    }
}
