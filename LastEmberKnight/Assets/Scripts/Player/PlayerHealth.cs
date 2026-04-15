using System;
using UnityEngine;

namespace LastEmberKnight
{
    /// <summary>
    /// Manages the player's HP pool, damage intake, healing, and death notification.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [Header("Stats")]
        [SerializeField] private float maxHP = 100f;

        [Header("Invincibility")]
        [SerializeField] private float invincibilityDuration = 0.5f;

        // ── Runtime ───────────────────────────────────────────────────────────
        private float _currentHP;
        private float _maxHPBonus;
        private bool  _isInvincible;

        // ── IDamageable ───────────────────────────────────────────────────────
        public float HP    => _currentHP;
        public float MaxHP => maxHP + _maxHPBonus;

        // ── Events ────────────────────────────────────────────────────────────
        /// <summary>Fired whenever damage greater than zero is applied.</summary>
        public event Action<float> OnDamageTaken;
        public event Action        OnDeath;

        private void Awake()
        {
            _currentHP = maxHP;
        }

        // ── IDamageable ───────────────────────────────────────────────────────

        public void TakeDamage(float amount, Vector2 knockback)
        {
            if (_isInvincible || amount <= 0f) return;

            _currentHP = Mathf.Max(0f, _currentHP - amount);
            OnDamageTaken?.Invoke(amount);

            if (_currentHP <= 0f)
            {
                OnDeath?.Invoke();
                GameManager.Instance?.TriggerPlayerDeath();
                return;
            }

            StartCoroutine(InvincibilityFlash());
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Restore HP by <paramref name="amount"/>, clamped to max.</summary>
        public void Heal(float amount)
        {
            _currentHP = Mathf.Min(MaxHP, _currentHP + amount);
        }

        /// <summary>Restore HP to the current maximum.</summary>
        public void HealToFull()
        {
            _currentHP = MaxHP;
        }

        /// <summary>Permanently increase max HP by <paramref name="amount"/>.</summary>
        public void AddMaxHP(float amount)
        {
            _maxHPBonus += amount;
            _currentHP  += amount;          // grant the extra HP immediately
            _currentHP   = Mathf.Min(_currentHP, MaxHP);
        }

        public float CurrentHP => _currentHP;
        public float HPFraction => MaxHP > 0f ? _currentHP / MaxHP : 0f;

        // ── Invincibility flash ───────────────────────────────────────────────
        private System.Collections.IEnumerator InvincibilityFlash()
        {
            _isInvincible = true;
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            float elapsed = 0f;
            while (elapsed < invincibilityDuration)
            {
                if (sr != null) sr.enabled = !sr.enabled;
                yield return new WaitForSeconds(0.05f);
                elapsed += 0.05f;
            }
            if (sr != null) sr.enabled = true;
            _isInvincible = false;
        }
    }
}
