using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace LastEmberKnight
{
    /// <summary>
    /// Singleton soul-meter manager.
    /// Soul fills via AddSoul(); fires OnSoulFull when maxSoul is reached.
    /// Soul Blast is triggered automatically when full, or via TriggerSoulBlast().
    /// </summary>
    public class SoulMeter : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────
        public static SoulMeter Instance { get; private set; }

        // ── Inspector Fields ──────────────────────────────────────────────────
        [Header("Soul Settings")]
        [SerializeField] private float maxSoul        = 100f;
        [SerializeField] private int   soulPerHit     = 8;
        [SerializeField] private int   soulPerBossHit = 5;

        [Header("Soul Blast")]
        [SerializeField] private GameObject soulBlastProjectilePrefab;
        [SerializeField] private float      soulBlastDamageMultiplier = 3f;
        [SerializeField] private float      soulBlastSpeed            = 14f;
        [SerializeField] private Transform  soulBlastSpawnPoint;   // assign to player muzzle

        [Header("UI")]
        [SerializeField] private Image   soulBarFill;      // fill image set to orange
        [SerializeField] private CanvasGroup soulBarGroup;
        [SerializeField] private Animator soulBarAnimator;

        // ── Events ─────────────────────────────────────────────────────────────
        public static event Action        OnSoulFull;
        public static event Action<float> OnSoulChanged;  // passes current value (0–maxSoul)

        // ── Runtime State ─────────────────────────────────────────────────────
        private float currentSoul;
        private bool  soulFullTriggered;

        // ── Properties ────────────────────────────────────────────────────────
        public float CurrentSoul => currentSoul;
        public float MaxSoul     => maxSoul;
        public float SoulFraction => maxSoul > 0f ? currentSoul / maxSoul : 0f;
        public bool  IsFull       => currentSoul >= maxSoul;

        // ── Unity Lifecycle ────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            currentSoul = 0f;
            RefreshUI();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Add soul points. Pass isBossHit=true to use the boss rate.</summary>
        public void AddSoul(int amount)
        {
            if (soulFullTriggered && IsFull) return;  // already full; wait for blast

            currentSoul = Mathf.Clamp(currentSoul + amount, 0f, maxSoul);
            OnSoulChanged?.Invoke(currentSoul);
            RefreshUI();

            if (IsFull && !soulFullTriggered)
            {
                soulFullTriggered = true;
                OnSoulFull?.Invoke();
                StartCoroutine(PulseFullIndicator());
            }
        }

        /// <summary>Convenience wrapper using per-hit rates.</summary>
        public void RegisterHit(bool isBossHit = false)
            => AddSoul(isBossHit ? soulPerBossHit : soulPerHit);

        /// <summary>
        /// Fires the Soul Blast projectile at 3× the player's base damage.
        /// Automatically called when soul is full; can also be called manually.
        /// </summary>
        public void TriggerSoulBlast(float playerBaseDamage, Vector2 fireDirection)
        {
            if (!IsFull) return;

            if (soulBlastProjectilePrefab != null)
            {
                Vector3 spawnPos = soulBlastSpawnPoint != null
                    ? soulBlastSpawnPoint.position
                    : transform.position;

                GameObject blastObj = Instantiate(soulBlastProjectilePrefab, spawnPos, Quaternion.identity);
                Projectile proj = blastObj.GetComponent<Projectile>();
                if (proj != null)
                {
                    proj.Initialize(
                        direction:      fireDirection.normalized,
                        speed:          soulBlastSpeed,
                        damage:         playerBaseDamage * soulBlastDamageMultiplier,
                        owner:          gameObject,
                        isHoming:       false,
                        homingStrength: 0f,
                        homingTarget:   null,
                        damageType:     DamageType.Magic);
                }
            }

            // Drain soul
            currentSoul       = 0f;
            soulFullTriggered = false;
            OnSoulChanged?.Invoke(currentSoul);
            RefreshUI();
        }

        // ── UI ────────────────────────────────────────────────────────────────
        private void RefreshUI()
        {
            if (soulBarFill == null) return;

            // Orange fill
            soulBarFill.color = new Color(1f, 0.5f, 0.05f);
            soulBarFill.fillAmount = SoulFraction;
        }

        private IEnumerator PulseFullIndicator()
        {
            if (soulBarAnimator != null)
            {
                soulBarAnimator.SetBool("Full", true);
            }
            else if (soulBarFill != null)
            {
                // Simple pulse: flash bright
                float elapsed = 0f;
                while (IsFull)
                {
                    elapsed += Time.deltaTime;
                    float pulse = 0.5f + 0.5f * Mathf.Sin(elapsed * 8f);
                    soulBarFill.color = Color.Lerp(
                        new Color(1f, 0.5f, 0.05f),
                        Color.white,
                        pulse * 0.4f);
                    yield return null;
                }

                soulBarFill.color = new Color(1f, 0.5f, 0.05f);
            }
        }
    }
}
