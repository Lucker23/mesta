using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LastEmberKnight
{
    /// <summary>
    /// UI controller for the Shrine upgrade menu.
    /// Called by <see cref="ShrineSystem"/> when the player interacts with a shrine.
    /// </summary>
    public class ShrineMenuUI : MonoBehaviour
    {
        [Header("Upgrade Buttons (0=MaxHP, 1=Attack, 2=DashCD, 3=SoulRate, 4=MaxMana)")]
        [SerializeField] private Button[]           upgradeButtons;
        [SerializeField] private TextMeshProUGUI[]  upgradeLabels;
        [SerializeField] private TextMeshProUGUI[]  costLabels;
        [SerializeField] private TextMeshProUGUI    shardCountLabel;
        [SerializeField] private Button             closeButton;

        private ShrineSystem _shrine;

        private void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(OnCloseClicked);

            for (int i = 0; i < (upgradeButtons?.Length ?? 0); i++)
            {
                int capturedIndex = i;
                if (upgradeButtons[i] != null)
                    upgradeButtons[i].onClick.AddListener(() => OnUpgradeClicked(capturedIndex));
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void Open(ShrineSystem shrine)
        {
            _shrine = shrine;
            gameObject.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            _shrine = null;
            gameObject.SetActive(false);
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void Refresh()
        {
            if (_shrine == null) return;

            // Shard count
            if (shardCountLabel != null && GameManager.Instance != null)
                shardCountLabel.text = $"Shards: {GameManager.Instance.TotalShards}";

            for (int i = 0; i < _shrine.UpgradeCount; i++)
            {
                if (upgradeLabels != null && i < upgradeLabels.Length && upgradeLabels[i] != null)
                    upgradeLabels[i].text = _shrine.GetUpgradeName(i);

                if (costLabels != null && i < costLabels.Length && costLabels[i] != null)
                    costLabels[i].text = $"{_shrine.GetUpgradeCost(i)} shards";

                if (upgradeButtons != null && i < upgradeButtons.Length && upgradeButtons[i] != null)
                {
                    UpgradeType type = (UpgradeType)i;
                    upgradeButtons[i].interactable =
                        UpgradeSystem.Instance != null && UpgradeSystem.Instance.CanAfford(type);
                }
            }
        }

        private void OnUpgradeClicked(int index)
        {
            if (_shrine == null) return;
            _shrine.TryPurchaseUpgrade(index);
            AudioManager.Instance?.PlaySound(SoundType.MenuClick);
            Refresh();
        }

        private void OnCloseClicked()
        {
            if (_shrine != null)
                _shrine.CloseShrine();
            AudioManager.Instance?.PlaySound(SoundType.MenuClick);
        }
    }
}
