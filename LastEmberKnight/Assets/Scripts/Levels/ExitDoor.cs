using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace LastEmberKnight
{
    /// <summary>
    /// Placed on the level's exit door. Becomes interactive only after all enemies
    /// in the level are dead. Tracks Untouchable (no damage taken) achievement.
    /// Player presses E within range to advance to the next level.
    /// </summary>
    public class ExitDoor : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("Activation")]
        [SerializeField] private float interactRange = 1.5f;
        [SerializeField] private string enemyTag     = "Enemy";

        [Header("Boss Mode")]
        [SerializeField] private bool bossMode = false;

        [Header("Visual – Light")]
        [SerializeField] private Light doorLight;
        [SerializeField] private float activeLightIntensity   = 2.0f;
        [SerializeField] private float inactiveLightIntensity = 0.3f;
        [SerializeField] private Color activeLightColor       = new Color(1f, 0.90f, 0.35f);
        [SerializeField] private Color inactiveLightColor     = new Color(0.4f, 0.4f, 0.4f);

        [Header("Visual – Particles")]
        [SerializeField] private ParticleSystem ascendingParticles;

        [Header("UI")]
        [SerializeField] private GameObject completeBanner;     // "Level Complete!" UI element
        [SerializeField] private GameObject untouchableLabel;   // "Untouchable!" label

        [Header("Events")]
        public UnityEvent onDoorActivated;
        public UnityEvent onLevelComplete;

        // ── Runtime ───────────────────────────────────────────────────────────
        private bool _active      = false;
        private bool _transitioning = false;
        private Transform _playerTransform;
        private float _lightPulse = 0f;
        private bool _untouchable = false;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            SetupLight();
            SetupParticles();

            if (completeBanner   != null) completeBanner.SetActive(false);
            if (untouchableLabel != null) untouchableLabel.SetActive(false);
        }

        private void Start()
        {
            GameObject p = GameObject.FindGameObjectWithTag(GameConstants.TagPlayer);
            if (p != null) _playerTransform = p.transform;

            // Hook into player's damage event to track Untouchable
            if (_playerTransform != null)
            {
                PlayerHealth ph = _playerTransform.GetComponent<PlayerHealth>();
                if (ph != null)
                    ph.OnDamageTaken += HandlePlayerDamageTaken;
            }

            RefreshActiveState();
        }

        private void OnDestroy()
        {
            if (_playerTransform != null)
            {
                PlayerHealth ph = _playerTransform.GetComponent<PlayerHealth>();
                if (ph != null)
                    ph.OnDamageTaken -= HandlePlayerDamageTaken;
            }
        }

        private void Update()
        {
            RefreshActiveState();
            AnimateLight();

            if (!_active || _transitioning || _playerTransform == null) return;

            float dist = Vector2.Distance(transform.position, _playerTransform.position);
            if (dist <= interactRange && Input.GetKeyDown(KeyCode.E))
            {
                AdvanceLevel();
            }
        }

        // =========================================================================
        //  Public API
        // =========================================================================

        /// <summary>Force the door into boss mode (requires boss dead, not enemy sweep).</summary>
        public void SetBossMode(bool isBoss)
        {
            bossMode = isBoss;
        }

        /// <summary>Force-activate the door (e.g. called by BossBase on defeat).</summary>
        public void Activate()
        {
            if (_active) return;
            _active = true;
            OnActivated();
        }

        // =========================================================================
        //  Private helpers
        // =========================================================================

        private void RefreshActiveState()
        {
            if (_active) return;

            bool shouldBeActive = AreAllEnemiesDead();
            if (shouldBeActive)
            {
                _active = true;
                OnActivated();
            }
        }

        private bool AreAllEnemiesDead()
        {
            // Boss mode: check for surviving boss objects
            if (bossMode)
            {
                GameObject[] bosses = GameObject.FindGameObjectsWithTag(GameConstants.TagBoss);
                return bosses == null || bosses.Length == 0;
            }

            // Normal mode: check for surviving enemies
            GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
            return enemies == null || enemies.Length == 0;
        }

        private void OnActivated()
        {
            // Swap light to active color
            if (doorLight != null)
            {
                doorLight.color     = activeLightColor;
                doorLight.intensity = activeLightIntensity;
            }

            // Start golden ascending particles
            if (ascendingParticles != null)
                ascendingParticles.Play();

            onDoorActivated.Invoke();
            AudioManager.Instance?.PlaySound(SoundType.LevelComplete, 0.6f);
        }

        private void AdvanceLevel()
        {
            if (_transitioning) return;
            _transitioning = true;

            StartCoroutine(LevelCompleteSequence());
        }

        private IEnumerator LevelCompleteSequence()
        {
            // Show UI banners
            if (completeBanner != null) completeBanner.SetActive(true);

            if (_untouchable)
            {
                if (untouchableLabel != null) untouchableLabel.SetActive(true);
                AchievementTracker.Instance?.CheckAchievement("Untouchable");
            }

            AudioManager.Instance?.PlaySound(SoundType.LevelComplete);
            onLevelComplete.Invoke();

            yield return new WaitForSeconds(2.0f);

            GameManager.Instance?.NextLevel();
        }

        private void HandlePlayerDamageTaken(float amount)
        {
            if (amount > 0f)
                _untouchable = false;
        }

        private void AnimateLight()
        {
            if (doorLight == null) return;
            if (!_active) return;

            _lightPulse += Time.deltaTime * 2.5f;
            doorLight.intensity = activeLightIntensity
                + Mathf.Sin(_lightPulse) * 0.4f;
        }

        // ── Setup ─────────────────────────────────────────────────────────────

        private void SetupLight()
        {
            if (doorLight == null)
            {
                GameObject lightGO = new GameObject("DoorLight");
                lightGO.transform.SetParent(transform);
                lightGO.transform.localPosition = new Vector3(0f, 1f, -1f);
                doorLight = lightGO.AddComponent<Light>();
                doorLight.type  = LightType.Point;
                doorLight.range = 6f;
            }

            doorLight.color     = inactiveLightColor;
            doorLight.intensity = inactiveLightIntensity;
        }

        private void SetupParticles()
        {
            if (ascendingParticles != null) return;

            GameObject go = new GameObject("DoorParticles");
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(0f, 0.5f, 0f);

            ParticleSystem ps = go.AddComponent<ParticleSystem>();

            ParticleSystem.MainModule main = ps.main;
            main.loop            = true;
            main.playOnAwake     = false;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(1.0f, 2.0f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(0.5f, 2.0f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.05f, 0.18f);
            main.startColor      = new ParticleSystem.MinMaxGradient(
                new Color(1.0f, 0.90f, 0.30f, 1f),
                new Color(1.0f, 1.0f,  0.80f, 1f));
            main.gravityModifier = -0.8f;          // float upward
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 25f;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Rectangle;
            shape.scale     = new Vector3(0.8f, 2.0f, 1f);

            ascendingParticles = ps;
        }

        // ── Init Untouchable flag ─────────────────────────────────────────────

        private void OnEnable()
        {
            _untouchable = true;    // Reset each time the door is enabled (new level)
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactRange);
        }
#endif
    }
}
