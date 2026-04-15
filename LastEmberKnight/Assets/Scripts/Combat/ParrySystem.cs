using System.Collections;
using UnityEngine;

namespace LastEmberKnight
{
    /// <summary>
    /// Parry system for the player.
    /// Attach to the player GameObject.
    ///
    /// Usage:
    ///   - Call TryActivateParryWindow() at the start of the player's parry input animation.
    ///   - Enemy attack scripts call AttemptParry(attackerGameObject) during their startup frames.
    ///   - On success: enemy is stunned, slow-motion burst fires, feedback plays.
    /// </summary>
    public class ParrySystem : MonoBehaviour
    {
        // ── Inspector Fields ──────────────────────────────────────────────────
        [Header("Parry Timing")]
        [SerializeField] private float parryWindowDuration = 0.133f; // ~4 frames at 30fps

        [Header("On Successful Parry")]
        [SerializeField] private float enemyStunDuration  = 0.5f;
        [SerializeField] private float slowMotionTimeScale = 0.15f;
        [SerializeField] private float slowMotionDuration  = 0.2f;

        [Header("VFX")]
        [SerializeField] private GameObject parryBurstPrefab;    // white particle burst
        [SerializeField] private GameObject screenFlashPrefab;   // full-screen white flash overlay

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip   parrySuccessClip;
        [SerializeField] private AudioClip   parryFailClip;

        // ── Runtime State ─────────────────────────────────────────────────────
        private bool  parryWindowActive;
        private float parryWindowTimer;
        private bool  parryConsumed;    // only one parry per activation

        // ── Properties ────────────────────────────────────────────────────────
        public bool IsParryWindowActive => parryWindowActive;

        // ── Unity Lifecycle ────────────────────────────────────────────────────
        private void Update()
        {
            if (!parryWindowActive) return;

            parryWindowTimer -= Time.deltaTime;
            if (parryWindowTimer <= 0f)
                CloseParryWindow();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Call this when the player inputs a parry (first 4 startup frames of their guard animation).
        /// </summary>
        public void TryActivateParryWindow()
        {
            if (parryWindowActive) return;
            parryWindowActive = true;
            parryWindowTimer  = parryWindowDuration;
            parryConsumed     = false;
        }

        /// <summary>
        /// Called by an enemy attack script during its startup frames.
        /// Returns true if the parry was successful.
        /// </summary>
        public bool AttemptParry(GameObject attacker)
        {
            if (!parryWindowActive || parryConsumed) return false;

            parryConsumed = true;
            CloseParryWindow();
            OnParrySuccess(attacker);
            return true;
        }

        // ── Internal Logic ────────────────────────────────────────────────────
        private void CloseParryWindow()
        {
            parryWindowActive = false;
            parryWindowTimer  = 0f;
        }

        private void OnParrySuccess(GameObject attacker)
        {
            // 1. Stun the attacker
            if (attacker != null)
            {
                // Try AshenGuard stun
                AshenGuard guard = attacker.GetComponent<AshenGuard>();
                if (guard != null) guard.Stun(enemyStunDuration);

                // Generic stun via EnemyBase if a dedicated Stun() isn't exposed
                // (extend this block for other enemy types as needed)
            }

            // 2. White burst particles at impact point
            if (parryBurstPrefab != null)
                Instantiate(parryBurstPrefab, transform.position, Quaternion.identity);

            // 3. Screen flash
            if (screenFlashPrefab != null)
            {
                GameObject flash = Instantiate(screenFlashPrefab);
                Destroy(flash, 0.15f);
            }

            // 4. Slow motion
            StartCoroutine(SlowMotionBurst());

            // 5. Sound
            if (audioSource != null && parrySuccessClip != null)
                audioSource.PlayOneShot(parrySuccessClip);

            // 6. Camera impulse via HitStop
            if (HitStop.Instance != null)
                HitStop.Instance.RequestStop(2);
        }

        private IEnumerator SlowMotionBurst()
        {
            float original = Time.timeScale;
            Time.timeScale = slowMotionTimeScale;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;

            yield return new WaitForSecondsRealtime(slowMotionDuration);

            Time.timeScale = original;
            Time.fixedDeltaTime = 0.02f * original;
        }
    }
}
