// =============================================================================
//  Lucker23Events.cs  –  Manages both Lucker23 narrative Easter egg events
// =============================================================================
//
// EVENT 1 – Level 5 transition (IEnumerator Level5HackSequence):
//   Screen flickers green → CRT scanline overlay appears → terminal types:
//     "> CONNECTION INTERCEPTED"
//     "> Lucker23 has entered the game"
//     "> Easter egg incoming at Lv.10"
//     "> You'll know when you see it"
//   Green tint fades over 3 s.
//
// EVENT 2 – Level 10 boss fight (IEnumerator Level10HackSequence):
//   - hackTimer: waits 3 s after boss spawns
//   - Phase 1: overlay + "SYSTEM BREACH DETECTED" + Lucker23 intro + "LOADING: ELDER TITAN"
//   - Phase 2: destroy current boss, spawn Boss1ElderTitan preserving HP% (min 80%),
//              fade message "Lucker23: Good luck with THIS one"
//
// Wiring: GameManager.OnLevelLoaded subscribes to trigger these sequences.
// =============================================================================
using System.Collections;
using UnityEngine;

namespace LastEmberKnight
{
    public class Lucker23Events : MonoBehaviour
    {
        // ─── Singleton ────────────────────────────────────────────────────────────
        public static Lucker23Events Instance { get; private set; }

        // ─── Inspector ────────────────────────────────────────────────────────────
        [Header("Boss Prefab for Level 10")]
        [SerializeField] private GameObject elderTitanPrefab;   // Boss1ElderTitan prefab

        [Header("Timing")]
        [SerializeField] private float hackTimerDelay = 3.0f;   // seconds after boss spawns before hack
        [SerializeField] private float greenFadeDuration = 3.0f;

        // ─── Colors ───────────────────────────────────────────────────────────────
        private static readonly Color GREEN_OVERLAY = new Color(0.0f, 0.9f, 0.15f);

        // ─── State ────────────────────────────────────────────────────────────────
        private bool _level5Done = false;
        private bool _level10Done = false;

        // ─────────────────────────────────────────────────────────────────────────
        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            GameManager.OnLevelLoaded += OnLevelLoaded;
            BossBase.OnBossDefeated   += OnBossDefeated;
        }

        private void OnDisable()
        {
            GameManager.OnLevelLoaded -= OnLevelLoaded;
            BossBase.OnBossDefeated   -= OnBossDefeated;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        #region Event Callbacks

        private void OnLevelLoaded(int level)
        {
            // Level 5 transition easter egg (first time only)
            if (level == 5 && !_level5Done)
            {
                _level5Done = true;
                StartCoroutine(Level5HackSequence());
            }
        }

        private void OnBossDefeated(BossBase boss)
        {
            // No further action needed here – Level 10 is handled via trigger
        }

        /// <summary>
        /// Called by the Level 10 boss arena trigger to start the hack sequence.
        /// </summary>
        public void TriggerLevel10Hack(BossBase currentBoss)
        {
            if (_level10Done) return;
            _level10Done = true;
            StartCoroutine(Level10HackSequence(currentBoss));
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        #region Event 1 – Level 5 Hack Sequence

        /// <summary>Green CRT terminal tease sequence at the start of Level 5.</summary>
        public IEnumerator Level5HackSequence()
        {
            HackOverlay overlay = HackOverlay.Instance;
            if (overlay == null) yield break;

            // Brief flicker before overlay appears
            yield return new WaitForSeconds(0.5f);

            overlay.EnableOverlay(GREEN_OVERLAY, 0.45f);
            overlay.ClearTerminal();

            // Type out terminal lines with staggered delays
            yield return overlay.ShowTerminalLine("> CONNECTION INTERCEPTED", 0.3f);
            yield return overlay.ShowTerminalLine("> Lucker23 has entered the game", 0.8f);
            yield return overlay.ShowTerminalLine("> Easter egg incoming at Lv.10", 0.7f);
            yield return overlay.ShowTerminalLine("> You'll know when you see it", 0.6f);

            // Hold for a moment then fade out
            yield return new WaitForSeconds(2.0f);
            overlay.DisableOverlay(greenFadeDuration);
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        #region Event 2 – Level 10 Hack Sequence

        /// <summary>
        /// Full hack sequence during the Level 10 boss fight.
        /// Waits 3 s, then intercepts and swaps the boss with Elder Titan.
        /// </summary>
        public IEnumerator Level10HackSequence(BossBase currentBoss)
        {
            if (currentBoss == null) yield break;

            HackOverlay overlay = HackOverlay.Instance;

            // ── Phase 1: Wait for hack timer ───────────────────────────────────
            yield return new WaitForSeconds(hackTimerDelay);

            // Record current HP percentage before we do anything
            float hpPercent = (currentBoss.MaxHP > 0f)
                ? Mathf.Clamp01(currentBoss.HP / currentBoss.MaxHP)
                : 1f;

            // ── Phase 1: Green overlay + scanlines ────────────────────────────
            if (overlay != null)
            {
                overlay.EnableOverlay(GREEN_OVERLAY, 0.55f);
                overlay.ClearTerminal();
            }

            yield return new WaitForSeconds(0.4f);

            if (overlay != null)
            {
                yield return overlay.ShowTerminalLine("> SYSTEM BREACH DETECTED", 0.1f);
                yield return overlay.ShowTerminalLine("> Lucker23: Surprise!", 0.7f);
                yield return overlay.ShowTerminalLine("> Let me fix this boss...", 0.5f);
                yield return overlay.ShowTerminalLine("> LOADING: ELDER TITAN", 0.9f);
            }

            yield return new WaitForSeconds(1.2f);

            // ── Phase 2: Destroy current boss, spawn Elder Titan ──────────────
            Vector3 spawnPos = currentBoss.transform.position;

            // Spawn VFX at current boss position
            if (ParticleManager.Instance != null)
                ParticleManager.Instance.SpawnBossPhaseEffect(spawnPos);

            // Screen shake
            if (ScreenShake.Instance != null)
                ScreenShake.Instance.Shake(ScreenShake.Boss);

            // Destroy current boss without triggering normal death sequence
            // We disable it first so OnDeath events don't fire mid-swap
            currentBoss.gameObject.SetActive(false);
            Destroy(currentBoss.gameObject);

            // Ensure Elder Titan HP is at least 80%
            float titanHpPercent = Mathf.Max(hpPercent, 0.80f);

            yield return null; // one frame for destroy to process

            // Spawn Elder Titan
            if (elderTitanPrefab != null)
            {
                GameObject titanGO = Instantiate(elderTitanPrefab, spawnPos, Quaternion.identity);
                BossBase titanBoss = titanGO.GetComponent<BossBase>();

                if (titanBoss != null)
                {
                    // Wait for the boss's Start() to run so maxHP is scaled
                    yield return null;

                    // Force HP to the preserved percentage (minimum 80%)
                    float titanMaxHP = titanBoss.MaxHP;
                    float titanHP    = titanMaxHP * titanHpPercent;
                    // Damage it down to the right value via TakeDamage if needed
                    float excess = titanMaxHP - titanHP;
                    if (excess > 0f)
                        titanBoss.TakeDamage(excess, Vector2.zero);
                }
            }
            else
            {
                Debug.LogWarning("[Lucker23Events] elderTitanPrefab is not assigned. Cannot spawn Elder Titan.");
            }

            // ── Phase 2 message ───────────────────────────────────────────────
            if (overlay != null)
            {
                yield return overlay.ShowTerminalLine("> Lucker23: Good luck with THIS one", 0.3f);
                yield return new WaitForSeconds(2.5f);
                overlay.DisableOverlay(1.5f);
            }
        }

        #endregion
    }
}
