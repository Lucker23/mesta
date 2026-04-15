using UnityEngine;

namespace LastEmberKnight
{
    /// <summary>
    /// Manages the player's mana pool for casting abilities.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerMana : MonoBehaviour
    {
        [Header("Stats")]
        [SerializeField] private float maxMana    = GameConstants.DefaultMaxMana;
        [SerializeField] private float regenRate  = GameConstants.ManaRegenRate;

        // ── Runtime ───────────────────────────────────────────────────────────
        private float _currentMana;
        private float _maxManaBonus;

        public float CurrentMana => _currentMana;
        public float MaxMana     => maxMana + _maxManaBonus;
        public float ManaFraction => MaxMana > 0f ? _currentMana / MaxMana : 0f;

        private void Awake() => _currentMana = maxMana;

        private void Update()
        {
            if (_currentMana < MaxMana)
                _currentMana = Mathf.Min(MaxMana, _currentMana + regenRate * Time.deltaTime);
        }

        // ── Public API ────────────────────────────────────────────────────────

        public bool TrySpend(float amount)
        {
            if (_currentMana < amount) return false;
            _currentMana -= amount;
            return true;
        }

        /// <summary>Restore mana by <paramref name="amount"/>, clamped to max.</summary>
        public void Restore(float amount)
        {
            _currentMana = Mathf.Min(MaxMana, _currentMana + amount);
        }

        /// <summary>Restore mana to current maximum.</summary>
        public void RestoreToFull()
        {
            _currentMana = MaxMana;
        }

        /// <summary>Permanently increase max mana by <paramref name="amount"/>.</summary>
        public void AddMaxMana(float amount)
        {
            _maxManaBonus += amount;
            _currentMana  += amount;
            _currentMana   = Mathf.Min(_currentMana, MaxMana);
        }
    }
}
