// =============================================================================
//  HitEffects.cs  –  Sprite hit flash, i-frame flicker, Ori-style death dissolve
// =============================================================================
// Ori × Hollow Knight blend:
//   Player death:  beautiful 3-phase dissolution (soul flash → ash scatter → lingering glow)
//   Enemy death:   dismissive crumble (quick alpha drop + gray smoke puff)
//   The difference in quality is intentional — his death is an event;
//   enemies are obstacles.
//
// Public API:
//   FlashWhite(sr, duration)              – 0.05 s white hit flash
//   ShowInvincibilityFlicker(sr)          – alpha flicker during i-frames
//   StopInvincibilityFlicker(sr)          – end flicker, restore alpha
//   DissolveOut(sr, duration)             – ENEMY death: fast crumble dissolve
//   PlayPlayerDeath(sr, onComplete)       – PLAYER death: 3-phase Ori dissolution
//   PlayEnemyDeathEffect(sr)              – enemy crumble + ash puff
// =============================================================================
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LastEmberKnight
{
    public class HitEffects : MonoBehaviour
    {
        // ─── Singleton ────────────────────────────────────────────────────────────
        public static HitEffects Instance { get; private set; }

        // ─── Timings ──────────────────────────────────────────────────────────────
        private const float WHITE_FLASH_DURATION   = 0.05f;
        private const float IFRAMES_FLICKER_RATE   = 0.06f;

        // Player death phase durations
        private const float PLAYER_DEATH_PHASE1    = 0.15f;   // soul flash white
        private const float PLAYER_DEATH_PHASE2    = 0.22f;   // scatter + alpha drops
        private const float PLAYER_DEATH_PHASE3    = 0.28f;   // lingering ember glow fades

        // Enemy death
        private const float ENEMY_DISSOLVE         = 0.20f;   // fast crumble

        // ─── Colors ───────────────────────────────────────────────────────────────
        private static readonly Color EMBER_GLOW = new Color(1.0f, 0.55f, 0.08f);
        private static readonly Color ASH_PUFF   = new Color(0.55f, 0.50f, 0.44f, 0.7f);

        // ─── Coroutine caches ─────────────────────────────────────────────────────
        private readonly Dictionary<SpriteRenderer, Coroutine> _activeFlashes   = new();
        private readonly Dictionary<SpriteRenderer, Coroutine> _activeFlickers  = new();
        private readonly Dictionary<SpriteRenderer, Coroutine> _activeDissolves = new();
        private readonly Dictionary<SpriteRenderer, Material>  _originalMats    = new();
        private Material _whiteMaterial;

        // ─────────────────────────────────────────────────────────────────────────
        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _whiteMaterial = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Sprites/Diffuse"));
            _whiteMaterial.color = Color.white;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        #region Public API

        /// <summary>Briefly flash a sprite white (hit feedback).</summary>
        public void FlashWhite(SpriteRenderer sr, float duration = WHITE_FLASH_DURATION)
        {
            if (sr == null) return;
            if (_activeFlashes.TryGetValue(sr, out Coroutine prev) && prev != null)
                StopCoroutine(prev);
            _activeFlashes[sr] = StartCoroutine(FlashWhiteCoroutine(sr, duration));
        }

        /// <summary>Begin alpha flicker during invincibility frames.</summary>
        public void ShowInvincibilityFlicker(SpriteRenderer sr)
        {
            if (sr == null) return;
            StopInvincibilityFlicker(sr);
            _activeFlickers[sr] = StartCoroutine(IFrameFlickerCoroutine(sr));
        }

        /// <summary>End i-frame flicker, restore alpha.</summary>
        public void StopInvincibilityFlicker(SpriteRenderer sr)
        {
            if (sr == null) return;
            if (_activeFlickers.TryGetValue(sr, out Coroutine co) && co != null)
                StopCoroutine(co);
            _activeFlickers.Remove(sr);
            if (sr != null) SetAlpha(sr, 1f);
        }

        /// <summary>
        /// Enemy dissolve: quick alpha fade + gray crumble puff.
        /// Fast and dismissive — enemies don't get a dramatic exit.
        /// </summary>
        public void DissolveOut(SpriteRenderer sr, float duration = ENEMY_DISSOLVE)
        {
            if (sr == null) return;
            if (_activeDissolves.TryGetValue(sr, out Coroutine prev) && prev != null)
                StopCoroutine(prev);
            _activeDissolves[sr] = StartCoroutine(EnemyDissolveCoroutine(sr, duration));
        }

        /// <summary>Alias kept for compatibility.</summary>
        public void PlayEnemyDeathEffect(SpriteRenderer sr) => DissolveOut(sr, ENEMY_DISSOLVE);

        /// <summary>
        /// Player death: 3-phase Ori-style dissolution.
        ///   Phase 1 — Soul flash: sprite turns white (the soul trying to persist)
        ///   Phase 2 — Scatter:   alpha drops while EmberTrail fires TriggerDeathScatter()
        ///   Phase 3 — Linger:    a warm ember PointLight fades at death position
        /// <paramref name="onComplete"/> fires when all three phases finish.
        /// </summary>
        public void PlayPlayerDeath(SpriteRenderer sr, Action onComplete = null)
        {
            if (sr == null) { onComplete?.Invoke(); return; }
            if (_activeDissolves.TryGetValue(sr, out Coroutine prev) && prev != null)
                StopCoroutine(prev);
            _activeDissolves[sr] = StartCoroutine(PlayerDeathCoroutine(sr, onComplete));
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        #region Coroutines

        private IEnumerator FlashWhiteCoroutine(SpriteRenderer sr, float duration)
        {
            if (sr == null) yield break;
            if (!_originalMats.ContainsKey(sr)) _originalMats[sr] = sr.material;
            sr.material = _whiteMaterial;
            yield return new WaitForSeconds(duration);
            if (sr != null && _originalMats.TryGetValue(sr, out Material orig))
                sr.material = orig;
            _activeFlashes.Remove(sr);
        }

        private IEnumerator IFrameFlickerCoroutine(SpriteRenderer sr)
        {
            if (sr == null) yield break;
            bool visible = true;
            while (true)
            {
                visible = !visible;
                SetAlpha(sr, visible ? 1f : 0f);
                yield return new WaitForSeconds(IFRAMES_FLICKER_RATE);
            }
        }

        // ── Enemy dissolve: quick crumble ──────────────────────────────────────────
        private IEnumerator EnemyDissolveCoroutine(SpriteRenderer sr, float duration)
        {
            if (sr == null) yield break;

            // Brief gray tint before dissolving
            sr.color = new Color(0.55f, 0.52f, 0.50f, 1f);
            yield return new WaitForSeconds(0.04f);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = 1f - Mathf.Clamp01(elapsed / duration);
                Color c = sr.color; c.a = alpha; sr.color = c;
                yield return null;
            }
            SetAlpha(sr, 0f);
            _activeDissolves.Remove(sr);

            // Spawn a small gray ash puff at the enemy position
            SpawnAshPuff(sr.transform.position);
        }

        // ── Player death: Ori-style 3-phase dissolution ────────────────────────────
        private IEnumerator PlayerDeathCoroutine(SpriteRenderer sr, Action onComplete)
        {
            if (sr == null) { onComplete?.Invoke(); yield break; }

            // ── Phase 1: Soul flash (the soul refusing to leave) ──────────────────
            // Sprite flashes white
            if (!_originalMats.ContainsKey(sr)) _originalMats[sr] = sr.material;
            sr.material = _whiteMaterial;
            yield return new WaitForSeconds(PLAYER_DEATH_PHASE1);
            if (sr != null && _originalMats.TryGetValue(sr, out Material orig))
                sr.material = orig;

            // ── Phase 2: Scatter (he becomes ash and embers) ──────────────────────
            // Trigger EmberTrail's death scatter burst
            EmberTrail trail = sr.GetComponent<EmberTrail>();
            trail?.TriggerDeathScatter();

            // Sprite alpha drops rapidly
            float elapsed = 0f;
            while (elapsed < PLAYER_DEATH_PHASE2 && sr != null)
            {
                elapsed += Time.deltaTime;
                float alpha = 1f - Mathf.Clamp01(elapsed / PLAYER_DEATH_PHASE2);
                SetAlpha(sr, alpha);
                yield return null;
            }
            if (sr != null) SetAlpha(sr, 0f);

            // ── Phase 3: Lingering ember glow (he leaves a light even in death) ────
            Vector3 deathPos = sr != null ? sr.transform.position : Vector3.zero;
            yield return SpawnLingeringEmberGlow(deathPos, PLAYER_DEATH_PHASE3);

            _activeDissolves.Remove(sr);
            onComplete?.Invoke();
        }

        // Spawns a warm PointLight at the death position that fades to nothing.
        // The knight leaves behind a small ember glow — a final memory.
        private IEnumerator SpawnLingeringEmberGlow(Vector3 pos, float duration)
        {
            GameObject go = new GameObject("DeathEmberGlow");
            go.transform.position = pos;
            Light lt = go.AddComponent<Light>();
            lt.type      = LightType.Point;
            lt.color     = EMBER_GLOW;
            lt.intensity = 0.9f;
            lt.range     = 2.5f;
            lt.shadows   = LightShadows.None;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                lt.intensity = Mathf.Lerp(0.9f, 0f, t);
                lt.range     = Mathf.Lerp(2.5f, 4.5f, t);  // expands as it fades
                yield return null;
            }
            Destroy(go);
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        #region Helpers

        private static void SetAlpha(SpriteRenderer sr, float alpha)
        {
            if (sr == null) return;
            Color c = sr.color; c.a = alpha; sr.color = c;
        }

        private static void SpawnAshPuff(Vector3 pos)
        {
            // Small one-shot gray smoke puff (enemy crumble)
            GameObject go = new GameObject("EnemyAshPuff");
            go.transform.position = pos;
            ParticleSystem ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.maxParticles    = 12;
            main.loop            = false;
            main.playOnAwake     = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(0.5f, 1.8f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.06f, 0.16f);
            main.startColor      = ASH_PUFF;
            main.gravityModifier = new ParticleSystem.MinMaxCurve(-0.1f);

            var em = ps.emission; em.enabled = true;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 8) });

            var sh = ps.shape; sh.enabled = true;
            sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 0.2f;

            var col = ps.colorOverLifetime; col.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(new Color(0.55f, 0.50f, 0.44f), 0f), new GradientColorKey(new Color(0.30f, 0.28f, 0.25f), 1f) },
                new[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g);

            var rend = go.GetComponent<ParticleSystemRenderer>();
            rend.renderMode = ParticleSystemRenderMode.Billboard;
            rend.material   = new Material(Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Sprites/Default"));

            ps.Play();
            // Auto-destroy
            UnityEngine.Object.Destroy(go, 1.0f);
        }

        #endregion
    }
}
