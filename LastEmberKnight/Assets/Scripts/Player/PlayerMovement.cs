using UnityEngine;

namespace LastEmberKnight
{
    /// <summary>
    /// Thin adapter for movement stat upgrades applied to PlayerController.
    /// Add this alongside PlayerController on the player GameObject so that
    /// UpgradeSystem can call movement-related methods without coupling to
    /// PlayerController internals.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerMovement : MonoBehaviour
    {
        private PlayerController _controller;

        private void Awake() => _controller = GetComponent<PlayerController>();

        /// <summary>
        /// Reduce the dash cooldown timer by <paramref name="seconds"/>.
        /// Delegates to PlayerController.ReduceDashCooldown.
        /// </summary>
        public void ReduceDashCooldown(float seconds)
        {
            if (_controller != null)
                _controller.ReduceDashCooldown(seconds);
        }
    }
}
