// =============================================================================
//  HUDManager.cs  –  In-game heads-up display for The Last Ember Knight
// =============================================================================
// Singleton MonoBehaviour managing all in-game HUD elements:
//   HP bar (red, dark-red below 30%, smooth lerp)
//   MP bar (blue glow)
//   Soul bar (orange, 0-100)
//   Shard counter (gold TextMeshPro)
//   Food inventory (5 slots with food-type icons)
//   Skill cooldown overlays (3 slots, radial darkening)
//   Floating damage numbers (TextMeshPro, rise + fade over 0.8s)
//   CanvasGroup alpha pulse glow on all bar groups
// =============================================================================
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LastEmberKnight
{
    [DefaultExecutionOrder(-50)]
    public class HUDManager : MonoBehaviour
    {
        // ─── Singleton ────────────────────────────────────────────────────────────
        public static HUDManager Instance { get; private set; }

        // ─── HP Bar ───────────────────────────────────────────────────────────────
        [Header("HP Bar")]
        [SerializeField] private Slider     hpSlider;
        [SerializeField] private Image      hpFill;
        [SerializeField] private CanvasGroup hpGlow;

        private static readonly Color HP_NORMAL    = new Color(0.85f, 0.10f, 0.10f, 1f);
        private static readonly Color HP_CRITICAL  = new Color(0.50f, 0.04f, 0.04f, 1f);
        private const float HP_LERP_SPEED          = 6f;
        private const float HP_CRITICAL_THRESHOLD  = 0.30f;

        // ─── MP Bar ───────────────────────────────────────────────────────────────
        [Header("MP Bar")]
        [SerializeField] private Slider      mpSlider;
        [SerializeField] private Image       mpFill;
        [SerializeField] private CanvasGroup mpGlow;

        private static readonly Color MP_COLOR = new Color(0.10f, 0.40f, 0.95f, 1f);
        private const float MP_LERP_SPEED      = 5f;

        // ─── Soul Bar ─────────────────────────────────────────────────────────────
        [Header("Soul Bar")]
        [SerializeField] private Slider      soulSlider;
        [SerializeField] private Image       soulFill;
        [SerializeField] private CanvasGroup soulGlow;

        private static readonly Color SOUL_COLOR = new Color(0.95f, 0.55f, 0.05f, 1f);
        private const float SOUL_LERP_SPEED       = 4f;

        // ─── Shard Counter ────────────────────────────────────────────────────────
        [Header("Shard Counter")]
        [SerializeField] private TextMeshProUGUI shardText;
        private static readonly Color SHARD_GOLD = new Color(1f, 0.82f, 0.10f, 1f);

        // ─── Food Inventory (5 slots) ─────────────────────────────────────────────
        [Header("Food Inventory")]
        [SerializeField] private Image[] foodSlots = new Image[5];
        [SerializeField] private Sprite[] foodIcons;           // indexed by (int)FoodType
        [SerializeField] private Sprite   emptyFoodSprite;

        // ─── Skill Cooldown Overlays (3 slots) ────────────────────────────────────
        [Header("Skill Buttons")]
        [SerializeField] private Image[]            skillCooldownOverlays = new Image[3];
        [SerializeField] private TextMeshProUGUI[]  skillCooldownTexts    = new TextMeshProUGUI[3];
        [SerializeField] private float[]            skillMaxCooldowns     = new float[3];

        private float[] _skillCooldownTimers = new float[3];
        private static readonly Color COOLDOWN_DARK = new Color(0f, 0f, 0f, 0.65f);

        // ─── Damage Numbers ───────────────────────────────────────────────────────
        [Header("Damage Numbers")]
        [SerializeField] private TextMeshProUGUI damageNumberPrefab;
        [SerializeField] private Canvas          worldCanvas;
        [SerializeField] private Camera          uiCamera;

        private const float DMG_FLOAT_DURATION = 0.8f;
        private const float DMG_FLOAT_HEIGHT   = 1.8f;

        private static readonly Color DMG_NORMAL = new Color(1f, 1f, 1f, 1f);
        private static readonly Color DMG_CRIT   = new Color(1f, 0.85f, 0.10f, 1f);

        // Pool of pre-allocated damage number labels
        private readonly Queue<TextMeshProUGUI> _dmgPool = new Queue<TextMeshProUGUI>();
        private const int DMG_POOL_SIZE = 16;

        // ─── Glow Pulse ───────────────────────────────────────────────────────────
        private const float GLOW_PULSE_SPEED = 2.0f;
        private const float GLOW_MIN         = 0.35f;
        private const float GLOW_MAX         = 0.85f;

        // ─── Interpolation Targets ────────────────────────────────────────────────
        private float _targetHP   = 1f;
        private float _targetMP   = 1f;
        private float _targetSoul = 0f;

        // ─────────────────────────────────────────────────────────────────────────
        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitialiseUI();
            PreWarmDamagePool();
        }

        private void Update()
        {
            LerpBars();
            PulseGlows();
            TickSkillCooldowns();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        #region Initialisation

        private void InitialiseUI()
        {
            if (hpFill   != null) hpFill.color   = HP_NORMAL;
            if (mpFill   != null) mpFill.color   = MP_COLOR;
            if (soulFill != null) soulFill.color = SOUL_COLOR;

            if (shardText != null)
            {
                shardText.color = SHARD_GOLD;
                shardText.text  = "x0";
            }

            // Empty food slots
            for (int i = 0; i < foodSlots.Length; i++)
                if (foodSlots[i] != null)
                {
                    foodSlots[i].sprite = emptyFoodSprite;
                    foodSlots[i].color  = new Color(1f, 1f, 1f, 0.25f);
                }

            // Zero out skill cooldown overlays
            for (int i = 0; i < skillCooldownOverlays.Length; i++)
            {
                if (skillCooldownOverlays[i] != null)
                {
                    skillCooldownOverlays[i].fillAmount = 0f;
                    skillCooldownOverlays[i].color      = COOLDOWN_DARK;
                    skillCooldownOverlays[i].type       = Image.Type.Filled;
                    skillCooldownOverlays[i].fillMethod  = Image.FillMethod.Radial360;
                    skillCooldownOverlays[i].fillOrigin  = (int)Image.Origin360.Top;
                    skillCooldownOverlays[i].fillClockwise = true;
                }
                if (skillCooldownTexts.Length > i && skillCooldownTexts[i] != null)
                    skillCooldownTexts[i].text = "";
            }
        }

        private void PreWarmDamagePool()
        {
            if (damageNumberPrefab == null || worldCanvas == null) return;
            for (int i = 0; i < DMG_POOL_SIZE; i++)
            {
                TextMeshProUGUI obj = Instantiate(damageNumberPrefab, worldCanvas.transform);
                obj.gameObject.SetActive(false);
                _dmgPool.Enqueue(obj);
            }
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        #region Public Bar / Counter API

        /// <summary>Smooth HP bar update. current and max are raw stat values.</summary>
        public void UpdateHP(float current, float max)
        {
            if (max <= 0f) return;
            _targetHP = Mathf.Clamp01(current / max);
        }

        /// <summary>Smooth MP bar update.</summary>
        public void UpdateMP(float current, float max)
        {
            if (max <= 0f) return;
            _targetMP = Mathf.Clamp01(current / max);
        }

        /// <summary>Smooth Soul bar update. Soul is 0-100; pass raw value.</summary>
        public void UpdateSoul(float current, float max = 100f)
        {
            if (max <= 0f) return;
            _targetSoul = Mathf.Clamp01(current / max);
        }

        /// <summary>Update the shard counter text immediately.</summary>
        public void UpdateShards(int count)
        {
            if (shardText != null)
                shardText.text = "x" + count.ToString();
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        #region Public Food API

        /// <summary>
        /// Set a single food slot. slotIndex 0-4.
        /// foodTypeIndex is (int)FoodType; pass -1 to clear.
        /// </summary>
        public void SetFoodSlot(int slotIndex, int foodTypeIndex)
        {
            if (slotIndex < 0 || slotIndex >= foodSlots.Length) return;
            Image slot = foodSlots[slotIndex];
            if (slot == null) return;

            bool hasFood = foodTypeIndex >= 0
                        && foodIcons != null
                        && foodTypeIndex < foodIcons.Length
                        && foodIcons[foodTypeIndex] != null;

            slot.sprite = hasFood ? foodIcons[foodTypeIndex] : emptyFoodSprite;
            slot.color  = hasFood ? Color.white : new Color(1f, 1f, 1f, 0.25f);
        }

        /// <summary>Refresh all 5 slots at once from an array of FoodType int values (-1 = empty).</summary>
        public void UpdateAllFoodSlots(int[] slotTypes)
        {
            for (int i = 0; i < 5; i++)
            {
                int typeIdx = (slotTypes != null && i < slotTypes.Length) ? slotTypes[i] : -1;
                SetFoodSlot(i, typeIdx);
            }
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        #region Public Skill Cooldown API

        /// <summary>
        /// Start a cooldown timer on skill slot skillIndex (0-2).
        /// Uses skillMaxCooldowns[skillIndex] as the duration.
        /// </summary>
        public void TriggerSkillCooldown(int skillIndex)
        {
            if (skillIndex < 0 || skillIndex >= _skillCooldownTimers.Length) return;
            _skillCooldownTimers[skillIndex] = skillMaxCooldowns[skillIndex];
            if (skillCooldownOverlays[skillIndex] != null)
                skillCooldownOverlays[skillIndex].fillAmount = 1f;
        }

        /// <summary>Set cooldown fill directly (0 = ready, 1 = full cooldown).</summary>
        public void SetSkillCooldownNormalised(int skillIndex, float normalised)
        {
            if (skillIndex < 0 || skillIndex >= skillCooldownOverlays.Length) return;
            if (skillCooldownOverlays[skillIndex] != null)
                skillCooldownOverlays[skillIndex].fillAmount = Mathf.Clamp01(normalised);
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        #region Public Damage Numbers API

        /// <summary>
        /// Spawn a floating damage number at worldPos.
        /// Crits are gold and displayed bold.
        /// </summary>
        public void ShowDamageNumber(Vector3 worldPos, float amount, bool isCrit)
        {
            if (worldCanvas == null) return;
            StartCoroutine(SpawnDamageNumber(worldPos, amount, isCrit));
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        #region Private Update Helpers

        private void LerpBars()
        {
            float dt = Time.deltaTime;

            if (hpSlider != null)
            {
                hpSlider.value = Mathf.Lerp(hpSlider.value, _targetHP, dt * HP_LERP_SPEED);
                if (hpFill != null)
                    hpFill.color = _targetHP < HP_CRITICAL_THRESHOLD ? HP_CRITICAL : HP_NORMAL;
            }

            if (mpSlider != null)
                mpSlider.value = Mathf.Lerp(mpSlider.value, _targetMP, dt * MP_LERP_SPEED);

            if (soulSlider != null)
                soulSlider.value = Mathf.Lerp(soulSlider.value, _targetSoul, dt * SOUL_LERP_SPEED);
        }

        private void PulseGlows()
        {
            float alpha = Mathf.Lerp(GLOW_MIN, GLOW_MAX,
                              (Mathf.Sin(Time.time * GLOW_PULSE_SPEED) + 1f) * 0.5f);
            SetGroupAlpha(hpGlow,   alpha);
            SetGroupAlpha(mpGlow,   alpha);
            SetGroupAlpha(soulGlow, alpha);
        }

        private static void SetGroupAlpha(CanvasGroup g, float a)
        {
            if (g != null) g.alpha = a;
        }

        private void TickSkillCooldowns()
        {
            for (int i = 0; i < _skillCooldownTimers.Length; i++)
            {
                if (_skillCooldownTimers[i] <= 0f) continue;

                _skillCooldownTimers[i] -= Time.deltaTime;
                if (_skillCooldownTimers[i] < 0f) _skillCooldownTimers[i] = 0f;

                float max = (skillMaxCooldowns != null && i < skillMaxCooldowns.Length)
                    ? skillMaxCooldowns[i] : 1f;

                if (skillCooldownOverlays[i] != null && max > 0f)
                    skillCooldownOverlays[i].fillAmount = _skillCooldownTimers[i] / max;

                if (skillCooldownTexts.Length > i && skillCooldownTexts[i] != null)
                {
                    bool ready = _skillCooldownTimers[i] <= 0.05f;
                    skillCooldownTexts[i].text    = ready ? "" : _skillCooldownTimers[i].ToString("F1");
                    skillCooldownTexts[i].enabled = !ready;
                }
            }
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        #region Damage Number Pool + Animation

        private TextMeshProUGUI GetPooledDmgLabel()
        {
            while (_dmgPool.Count > 0)
            {
                TextMeshProUGUI obj = _dmgPool.Dequeue();
                if (obj == null) continue;
                if (!obj.gameObject.activeInHierarchy) return obj;
                _dmgPool.Enqueue(obj); // still active, skip
                break;
            }
            // Allocate new if pool exhausted
            if (damageNumberPrefab == null) return null;
            TextMeshProUGUI fresh = Instantiate(damageNumberPrefab,
                worldCanvas != null ? worldCanvas.transform : transform);
            return fresh;
        }

        private IEnumerator SpawnDamageNumber(Vector3 worldPos, float amount, bool isCrit)
        {
            TextMeshProUGUI dmgText = GetPooledDmgLabel();
            if (dmgText == null) yield break;

            dmgText.gameObject.SetActive(true);

            // Convert world → canvas local position
            Camera cam = uiCamera != null ? uiCamera : Camera.main;
            Vector2 screenPos = cam != null
                ? (Vector2)cam.WorldToScreenPoint(worldPos)
                : (Vector2)worldPos;

            if (worldCanvas != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    worldCanvas.GetComponent<RectTransform>(),
                    screenPos,
                    worldCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : cam,
                    out Vector2 localPos);
                dmgText.rectTransform.localPosition = localPos;
            }

            // Style
            dmgText.text      = isCrit ? Mathf.RoundToInt(amount) + "!" : Mathf.RoundToInt(amount).ToString();
            dmgText.color     = isCrit ? DMG_CRIT : DMG_NORMAL;
            dmgText.fontSize  = isCrit ? 38f : 28f;
            dmgText.fontStyle = isCrit ? FontStyles.Bold : FontStyles.Normal;

            Vector2 startLocal = dmgText.rectTransform.localPosition;
            // Small random horizontal drift
            float xDrift = Random.Range(-12f, 12f) * (isCrit ? 1.5f : 1f);

            float elapsed = 0f;
            while (elapsed < DMG_FLOAT_DURATION)
            {
                elapsed += Time.deltaTime;
                float t  = elapsed / DMG_FLOAT_DURATION;

                // Rise with ease-out curve
                float rise = DMG_FLOAT_HEIGHT * 100f * Mathf.Sin(t * Mathf.PI * 0.5f);
                dmgText.rectTransform.localPosition = startLocal + new Vector2(xDrift * t, rise);

                // Fade out in second 40%
                float alpha = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
                Color c = dmgText.color;
                c.a = alpha;
                dmgText.color = c;

                yield return null;
            }

            dmgText.gameObject.SetActive(false);
            _dmgPool.Enqueue(dmgText);
        }

        #endregion
    }
}
