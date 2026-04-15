using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace LastEmberKnight
{
    /// <summary>
    /// Placed on each Shrine GameObject in the level.
    /// Player presses E within 1.5 units to open the upgrade menu.
    /// Shrine heals the player to full HP/MP on activation.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class ShrineSystem : MonoBehaviour
    {
        // ── Upgrade cost table ────────────────────────────────────────────────
        private static readonly float[] UpgradeCosts = new float[]
        {
            5f,   // MaxHP       +20 HP
            5f,   // Attack      +5 ATK
            8f,   // DashCD      -0.13s
            6f,   // SoulRate    +3
            7f,   // MaxMana     +10 MP
        };

        private static readonly string[] UpgradeNames = new string[]
        {
            "Max HP +20",
            "Attack +5",
            "Dash CD -0.13s",
            "Soul Rate +3",
            "Max Mana +10"
        };

        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("Interaction")]
        [SerializeField] private float interactRange = 1.5f;

        [Header("Visual – Light")]
        [SerializeField] private Light shrineLight;
        [SerializeField] private float lightBaseIntensity = 1.2f;
        [SerializeField] private float lightPulseSpeed    = 2.0f;
        [SerializeField] private float lightPulseAmount   = 0.4f;
        [SerializeField] private Color lightColor         = new Color(1.0f, 0.55f, 0.20f);

        [Header("Visual – Particles")]
        [SerializeField] private ParticleSystem sparkParticles;

        [Header("UI")]
        [SerializeField] private GameObject upgradeMenuPanel;
        [SerializeField] private ShrineMenuUI shrineMenuUI;       // optional UI bridge

        [Header("Events")]
        public UnityEvent onShrineOpened;
        public UnityEvent onShrineClosed;
        public UnityEvent onUpgradePurchased;

        // ── Runtime ───────────────────────────────────────────────────────────
        private bool     _menuOpen      = false;
        private bool     _used          = false;        // true after at least one interaction
        private Transform _playerTransform;
        private float    _lightTime     = 0f;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            // Create a procedural point light if none assigned
            if (shrineLight == null)
            {
                GameObject lightGO = new GameObject("ShrineLight");
                lightGO.transform.SetParent(transform);
                lightGO.transform.localPosition = new Vector3(0f, 1.2f, -1f);
                shrineLight = lightGO.AddComponent<Light>();
                shrineLight.type  = LightType.Point;
                shrineLight.range = 5f;
                shrineLight.color = lightColor;
                shrineLight.intensity = lightBaseIntensity;
            }

            // Create spark particles if none assigned
            if (sparkParticles == null)
                sparkParticles = CreateSparkParticles();

            if (upgradeMenuPanel != null)
                upgradeMenuPanel.SetActive(false);
        }

        private void Start()
        {
            // Cache player reference
            GameObject playerObj = GameObject.FindGameObjectWithTag(GameConstants.TagPlayer);
            if (playerObj != null)
                _playerTransform = playerObj.transform;
        }

        private void Update()
        {
            AnimateLight();

            if (_playerTransform == null) return;
            float dist = Vector2.Distance(transform.position, _playerTransform.position);

            if (!_menuOpen && dist <= interactRange && Input.GetKeyDown(KeyCode.E))
            {
                OpenShrine();
            }
            else if (_menuOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                CloseShrine();
            }
        }

        // =========================================================================
        //  Public API
        // =========================================================================

        /// <summary>Open the shrine UI and heal the player.</summary>
        public void OpenShrine()
        {
            if (_menuOpen) return;
            _menuOpen = true;

            HealPlayer();

            if (upgradeMenuPanel != null)
                upgradeMenuPanel.SetActive(true);

            if (shrineMenuUI != null)
                shrineMenuUI.Open(this);

            GameManager.Instance?.EnterShrine();
            onShrineOpened.Invoke();

            AudioManager.Instance?.PlaySound(SoundType.Shrine);
        }

        /// <summary>Close the shrine menu.</summary>
        public void CloseShrine()
        {
            if (!_menuOpen) return;
            _menuOpen = false;

            if (upgradeMenuPanel != null)
                upgradeMenuPanel.SetActive(false);

            if (shrineMenuUI != null)
                shrineMenuUI.Close();

            GameManager.Instance?.LeaveShrine();
            onShrineClosed.Invoke();
        }

        /// <summary>
        /// Purchase an upgrade by its zero-based index.
        /// Index maps to <see cref="UpgradeType"/> enum order.
        /// </summary>
        public bool TryPurchaseUpgrade(int upgradeIndex)
        {
            if (upgradeIndex < 0 || upgradeIndex >= UpgradeCosts.Length) return false;

            UpgradeType type = (UpgradeType)upgradeIndex;

            if (!UpgradeSystem.Instance.CanAfford(type)) return false;

            UpgradeSystem.Instance.ApplyUpgrade(type);
            SaveSystem.Save();
            onUpgradePurchased.Invoke();
            AudioManager.Instance?.PlaySound(SoundType.ShardPickup);

            return true;
        }

        /// <summary>Returns the shard cost of an upgrade slot.</summary>
        public float GetUpgradeCost(int upgradeIndex)
        {
            return (upgradeIndex >= 0 && upgradeIndex < UpgradeCosts.Length)
                ? UpgradeCosts[upgradeIndex] : 0f;
        }

        /// <summary>Returns the display name of an upgrade slot.</summary>
        public string GetUpgradeName(int upgradeIndex)
        {
            return (upgradeIndex >= 0 && upgradeIndex < UpgradeNames.Length)
                ? UpgradeNames[upgradeIndex] : string.Empty;
        }

        public int UpgradeCount => UpgradeCosts.Length;

        // =========================================================================
        //  Private helpers
        // =========================================================================

        private void HealPlayer()
        {
            PlayerHealth ph = GetPlayerHealth();
            if (ph != null) ph.HealToFull();

            PlayerMana pm = GetPlayerMana();
            if (pm != null) pm.RestoreToFull();
        }

        private PlayerHealth GetPlayerHealth()
        {
            if (_playerTransform == null) return null;
            return _playerTransform.GetComponent<PlayerHealth>();
        }

        private PlayerMana GetPlayerMana()
        {
            if (_playerTransform == null) return null;
            return _playerTransform.GetComponent<PlayerMana>();
        }

        private void AnimateLight()
        {
            if (shrineLight == null) return;
            _lightTime += Time.deltaTime * lightPulseSpeed;
            shrineLight.intensity = lightBaseIntensity
                + Mathf.Sin(_lightTime) * lightPulseAmount;
        }

        private ParticleSystem CreateSparkParticles()
        {
            GameObject go = new GameObject("ShrineSparkParticles");
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(0f, 0.5f, 0f);

            ParticleSystem ps = go.AddComponent<ParticleSystem>();

            ParticleSystem.MainModule main = ps.main;
            main.loop       = true;
            main.startLifetime  = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
            main.startSpeed     = new ParticleSystem.MinMaxCurve(0.5f, 2.5f);
            main.startSize      = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
            main.startColor     = new ParticleSystem.MinMaxGradient(
                new Color(1.0f, 0.65f, 0.15f, 1f),
                new Color(1.0f, 0.90f, 0.50f, 1f));
            main.gravityModifier = -0.3f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 20f;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius    = 0.3f;

            ps.Play();
            return ps;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.65f, 0.2f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, interactRange);
        }
#endif
    }
}
