// =============================================================================
//  MobileControls.cs  –  On-screen virtual controller for mobile / editor testing
// =============================================================================
// Auto-detects platform (Application.isMobilePlatform || editor override).
// Layout:
//   Left side:  D-pad (Left arrow, Right arrow, Up for wall-jump, Down for slam)
//   Right side: Attack (large), Dash, Skill (cycles skills), Food, Jump
// All buttons 80×80 px minimum touch targets, semi-transparent (alpha 0.6).
// Exposes public static bool fields read by PlayerController each frame.
// =============================================================================
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace LastEmberKnight
{
    [DefaultExecutionOrder(-30)]
    public class MobileControls : MonoBehaviour
    {
        // ─── Singleton ────────────────────────────────────────────────────────────
        public static MobileControls Instance { get; private set; }

        // ─── Public Static Input State (read by PlayerController) ─────────────────
        public static bool MoveLeft    { get; private set; }
        public static bool MoveRight   { get; private set; }
        public static bool Jump        { get; private set; }
        public static bool WallJump    { get; private set; }
        public static bool Slam        { get; private set; }
        public static bool Attack      { get; private set; }
        public static bool Dash        { get; private set; }
        public static bool Skill       { get; private set; }
        public static bool UseFood     { get; private set; }

        // One-frame pulse flags (true only the frame the button is pressed)
        public static bool AttackDown  { get; private set; }
        public static bool DashDown    { get; private set; }
        public static bool SkillDown   { get; private set; }
        public static bool JumpDown    { get; private set; }

        // ─── Inspector: Control Root ──────────────────────────────────────────────
        [Header("Control Root")]
        [SerializeField] private CanvasGroup controlsRoot;
        [SerializeField] private bool        forceShowInEditor = true;

        // ─── Inspector: D-Pad Buttons ─────────────────────────────────────────────
        [Header("D-Pad")]
        [SerializeField] private MobileButton leftButton;
        [SerializeField] private MobileButton rightButton;
        [SerializeField] private MobileButton upButton;
        [SerializeField] private MobileButton downButton;

        // ─── Inspector: Action Buttons ────────────────────────────────────────────
        [Header("Action Buttons")]
        [SerializeField] private MobileButton jumpButton;
        [SerializeField] private MobileButton attackButton;
        [SerializeField] private MobileButton dashButton;
        [SerializeField] private MobileButton skillButton;
        [SerializeField] private MobileButton foodButton;

        // ─── Constants ────────────────────────────────────────────────────────────
        private const float BUTTON_ALPHA = 0.6f;
        private const float MIN_BUTTON_SIZE = 80f;

        // ─── State ────────────────────────────────────────────────────────────────
        private bool _prevAttack;
        private bool _prevDash;
        private bool _prevSkill;
        private bool _prevJump;
        private bool _isMobilePlatform;

        // ─────────────────────────────────────────────────────────────────────────
        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            _isMobilePlatform = Application.isMobilePlatform
                             || (Application.isEditor && forceShowInEditor);

            SetControlsVisible(_isMobilePlatform);
            EnforceMinimumButtonSizes();
            SetButtonAlphas(BUTTON_ALPHA);
        }

        private void LateUpdate()
        {
            if (!_isMobilePlatform)
            {
                ClearAllInputs();
                return;
            }

            ReadButtonStates();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        #region Private Helpers

        private void ReadButtonStates()
        {
            // Held states
            MoveLeft  = leftButton  != null && leftButton.IsHeld;
            MoveRight = rightButton != null && rightButton.IsHeld;
            WallJump  = upButton    != null && upButton.IsHeld;
            Slam      = downButton  != null && downButton.IsHeld;
            Jump      = jumpButton  != null && jumpButton.IsHeld;
            Attack    = attackButton != null && attackButton.IsHeld;
            Dash      = dashButton   != null && dashButton.IsHeld;
            Skill     = skillButton  != null && skillButton.IsHeld;
            UseFood   = foodButton   != null && foodButton.IsHeld;

            // One-frame press flags
            AttackDown = Attack && !_prevAttack;
            DashDown   = Dash   && !_prevDash;
            SkillDown  = Skill  && !_prevSkill;
            JumpDown   = Jump   && !_prevJump;

            _prevAttack = Attack;
            _prevDash   = Dash;
            _prevSkill  = Skill;
            _prevJump   = Jump;
        }

        private static void ClearAllInputs()
        {
            MoveLeft = MoveRight = Jump = WallJump = Slam = false;
            Attack = Dash = Skill = UseFood = false;
            AttackDown = DashDown = SkillDown = JumpDown = false;
        }

        private void SetControlsVisible(bool visible)
        {
            if (controlsRoot == null) return;
            controlsRoot.alpha          = visible ? 1f : 0f;
            controlsRoot.interactable   = visible;
            controlsRoot.blocksRaycasts = visible;
        }

        private void EnforceMinimumButtonSizes()
        {
            EnforceSingleButtonSize(leftButton);
            EnforceSingleButtonSize(rightButton);
            EnforceSingleButtonSize(upButton);
            EnforceSingleButtonSize(downButton);
            EnforceSingleButtonSize(jumpButton);
            EnforceSingleButtonSize(attackButton);
            EnforceSingleButtonSize(dashButton);
            EnforceSingleButtonSize(skillButton);
            EnforceSingleButtonSize(foodButton);
        }

        private static void EnforceSingleButtonSize(MobileButton btn)
        {
            if (btn == null) return;
            RectTransform rt = btn.GetComponent<RectTransform>();
            if (rt == null) return;
            Vector2 size = rt.sizeDelta;
            if (size.x < MIN_BUTTON_SIZE) size.x = MIN_BUTTON_SIZE;
            if (size.y < MIN_BUTTON_SIZE) size.y = MIN_BUTTON_SIZE;
            rt.sizeDelta = size;
        }

        private void SetButtonAlphas(float alpha)
        {
            SetButtonAlpha(leftButton,   alpha);
            SetButtonAlpha(rightButton,  alpha);
            SetButtonAlpha(upButton,     alpha);
            SetButtonAlpha(downButton,   alpha);
            SetButtonAlpha(jumpButton,   alpha);
            SetButtonAlpha(attackButton, alpha);
            SetButtonAlpha(dashButton,   alpha);
            SetButtonAlpha(skillButton,  alpha);
            SetButtonAlpha(foodButton,   alpha);
        }

        private static void SetButtonAlpha(MobileButton btn, float alpha)
        {
            if (btn == null) return;
            Image img = btn.GetComponent<Image>();
            if (img == null) return;
            Color c = img.color; c.a = alpha; img.color = c;
        }

        #endregion
    }

    // =========================================================================
    //  MobileButton  –  lightweight press/hold tracker using IPointer interfaces
    // =========================================================================
    public class MobileButton : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        /// <summary>True while this button is being held by any pointer.</summary>
        public bool IsHeld { get; private set; }

        public void OnPointerDown(PointerEventData eventData) => IsHeld = true;
        public void OnPointerUp(PointerEventData eventData)   => IsHeld = false;
        public void OnPointerExit(PointerEventData eventData) => IsHeld = false;
    }
}
