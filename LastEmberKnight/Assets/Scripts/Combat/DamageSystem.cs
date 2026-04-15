using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace LastEmberKnight
{
    /// <summary>
    /// Central damage-processing singleton.
    /// Call DamageSystem.Instance.DealDamage(...) from anywhere in the game.
    /// </summary>
    public class DamageSystem : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────
        public static DamageSystem Instance { get; private set; }

        // ── Inspector Fields ──────────────────────────────────────────────────
        [Header("Floating Numbers")]
        [SerializeField] private GameObject floatingDamageNumberPrefab;
        [SerializeField] private Color normalDamageColor  = Color.white;
        [SerializeField] private Color critDamageColor    = new Color(1f, 0.8f, 0f);   // gold
        [SerializeField] private Color fireDamageColor    = new Color(1f, 0.4f, 0.1f);
        [SerializeField] private Color iceDamageColor     = new Color(0.4f, 0.85f, 1f);
        [SerializeField] private Color magicDamageColor   = new Color(0.7f, 0.3f, 1f);

        [Header("Critical Hits")]
        [SerializeField] private float critChance  = 0.05f;  // 5%
        [SerializeField] private float critMultiplier = 2f;

        [Header("HitStop")]
        [SerializeField] private int normalHitStopFrames = 3;
        [SerializeField] private int bossHitStopFrames   = 5;

        [Header("Canvas")]
        [SerializeField] private Canvas worldSpaceCanvas;

        // ── Events ─────────────────────────────────────────────────────────────
        public static event Action<IDamageable, float, DamageType>  OnDamageDealt;
        public static event Action<float, Vector2>                  OnPlayerDamaged;

        // ── Unity Lifecycle ────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── Core API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Deal damage to any IDamageable. Handles crits, floating numbers, hit-stop, and events.
        /// </summary>
        public void DealDamage(IDamageable target, float amount, Vector2 knockback,
                               DamageType type, GameObject source)
        {
            if (target == null || amount <= 0f) return;

            // ── Critical hit roll ──────────────────────────────────────────────
            bool isCrit = UnityEngine.Random.value < critChance;
            float finalAmount = isCrit ? amount * critMultiplier : amount;

            // ── Apply damage ───────────────────────────────────────────────────
            target.TakeDamage(finalAmount, knockback);

            // ── Floating number ────────────────────────────────────────────────
            if (target is MonoBehaviour mb)
            {
                SpawnFloatingNumber(mb.transform.position, finalAmount, type, isCrit);
            }

            // ── Hit stop ───────────────────────────────────────────────────────
            bool isBossSource = source != null && source.CompareTag("Boss");
            int stopFrames = isBossSource ? bossHitStopFrames : normalHitStopFrames;
            if (HitStop.Instance != null)
                HitStop.Instance.RequestStop(stopFrames);

            // ── Events ────────────────────────────────────────────────────────
            OnDamageDealt?.Invoke(target, finalAmount, type);

            // Detect if target is the player
            if (target is MonoBehaviour playerMb && playerMb.CompareTag("Player"))
                OnPlayerDamaged?.Invoke(finalAmount, knockback);
        }

        // ── Overload: source-less version ─────────────────────────────────────
        public void DealDamage(IDamageable target, float amount, Vector2 knockback, DamageType type)
            => DealDamage(target, amount, knockback, type, null);

        // ── Floating damage numbers ────────────────────────────────────────────
        private void SpawnFloatingNumber(Vector3 worldPos, float amount, DamageType type, bool isCrit)
        {
            if (floatingDamageNumberPrefab == null) return;

            GameObject numObj = Instantiate(floatingDamageNumberPrefab, worldPos, Quaternion.identity);

            // Parent to canvas if available for proper world-space rendering
            if (worldSpaceCanvas != null)
                numObj.transform.SetParent(worldSpaceCanvas.transform, true);

            TMP_Text label = numObj.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.text  = isCrit ? $"<b>{Mathf.CeilToInt(amount)}</b>" : Mathf.CeilToInt(amount).ToString();
                label.color = isCrit ? critDamageColor : GetTypeColor(type);
            }

            // Animate float-up and fade
            StartCoroutine(AnimateFloatingNumber(numObj));
        }

        private IEnumerator AnimateFloatingNumber(GameObject numObj)
        {
            if (numObj == null) yield break;

            TMP_Text label = numObj.GetComponentInChildren<TMP_Text>();
            float duration = 1.0f;
            float elapsed  = 0f;
            Vector3 startPos = numObj.transform.position;

            while (elapsed < duration && numObj != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                numObj.transform.position = startPos + Vector3.up * (t * 1.2f);

                if (label != null)
                {
                    Color c = label.color;
                    c.a = 1f - t;
                    label.color = c;
                }

                yield return null;
            }

            if (numObj != null) Destroy(numObj);
        }

        private Color GetTypeColor(DamageType type)
        {
            return type switch
            {
                DamageType.Fire      => fireDamageColor,
                DamageType.Ice       => iceDamageColor,
                DamageType.Magic     => magicDamageColor,
                DamageType.Slash     => new Color(1f, 0.95f, 0.8f),
                DamageType.Explosion => new Color(1f, 0.55f, 0.1f),
                _                    => normalDamageColor,
            };
        }
    }
}
