using UnityEngine;

namespace LastEmberKnight
{
    /// <summary>
    /// Static utility class providing shared AI helper methods.
    /// Used by all enemy scripts for common platform navigation queries.
    /// </summary>
    public static class EnemyAI
    {
        // ── Visibility / Detection ─────────────────────────────────────────────

        /// <summary>
        /// Returns true if there is an unobstructed line of sight between origin and the player.
        /// Uses a Physics2D.Linecast against the obstruction layer.
        /// </summary>
        public static bool CanSeePlayer(
            Transform origin,
            Transform player,
            LayerMask obstructionMask,
            float maxDistance = Mathf.Infinity)
        {
            if (origin == null || player == null) return false;

            Vector2 dir = (Vector2)player.position - (Vector2)origin.position;
            float dist = dir.magnitude;

            if (dist > maxDistance) return false;

            // Cast a ray; if nothing blocks it the enemy can see the player
            RaycastHit2D hit = Physics2D.Linecast(origin.position, player.position, obstructionMask);
            return !hit.collider;
        }

        // ── Direction Helpers ──────────────────────────────────────────────────

        /// <summary>
        /// Returns the normalised 2D direction from source to player.
        /// Returns Vector2.zero if either transform is null.
        /// </summary>
        public static Vector2 GetDirectionToPlayer(Transform source, Transform player)
        {
            if (source == null || player == null) return Vector2.zero;
            return ((Vector2)player.position - (Vector2)source.position).normalized;
        }

        /// <summary>
        /// Returns the horizontal sign (-1 or +1) pointing from source toward player.
        /// </summary>
        public static float GetHorizontalSignToPlayer(Transform source, Transform player)
        {
            if (source == null || player == null) return 0f;
            return Mathf.Sign(player.position.x - source.position.x);
        }

        // ── Edge Detection ─────────────────────────────────────────────────────

        /// <summary>
        /// Returns true if moving in moveDir (±1) from the enemy's current position
        /// would take it off the edge of a platform.
        /// Uses an offset ground-check position in front of the enemy's foot.
        /// </summary>
        public static bool IsAtEdge(
            Transform enemyTransform,
            Transform groundCheckPoint,
            float moveDir,
            LayerMask groundLayer,
            float edgeLookAhead = 0.35f,
            float checkRadius   = 0.15f)
        {
            if (groundCheckPoint == null) return false;

            Vector3 checkPos = groundCheckPoint.position
                             + Vector3.right * Mathf.Sign(moveDir) * edgeLookAhead;

            return !Physics2D.OverlapCircle(checkPos, checkRadius, groundLayer);
        }

        // ── Patrol Helpers ─────────────────────────────────────────────────────

        /// <summary>
        /// Returns the next patrol point index given the current index and the total count.
        /// Wraps around to 0 when the end is reached.
        /// </summary>
        public static int NextPatrolIndex(int current, int total)
        {
            if (total <= 0) return 0;
            return (current + 1) % total;
        }

        /// <summary>
        /// Returns a random patrol point position within a given range from origin.
        /// Useful for enemies without pre-placed patrol waypoints.
        /// The returned point is cast downward to find the ground position.
        /// </summary>
        public static Vector3 FindPatrolPoint(
            Vector3 origin,
            float rangeX,
            LayerMask groundLayer,
            float castDistance = 5f)
        {
            float randomX = origin.x + Random.Range(-rangeX, rangeX);
            Vector2 castOrigin = new Vector2(randomX, origin.y + 1f);

            RaycastHit2D hit = Physics2D.Raycast(castOrigin, Vector2.down, castDistance, groundLayer);
            if (hit.collider != null)
                return hit.point;

            // Fallback: return the same height
            return new Vector3(randomX, origin.y, origin.z);
        }

        // ── Platform Navigation ────────────────────────────────────────────────

        /// <summary>
        /// Simple check: returns true if there is ground directly below the given point.
        /// </summary>
        public static bool HasGroundBelow(Vector3 position, LayerMask groundLayer, float checkDist = 0.3f)
        {
            return Physics2D.Raycast(position, Vector2.down, checkDist, groundLayer).collider != null;
        }

        /// <summary>
        /// Returns the vertical distance to the nearest ground below the given position.
        /// Returns Mathf.Infinity if no ground is found within maxDist.
        /// </summary>
        public static float DistanceToGroundBelow(Vector3 position, LayerMask groundLayer, float maxDist = 10f)
        {
            RaycastHit2D hit = Physics2D.Raycast(position, Vector2.down, maxDist, groundLayer);
            return hit.collider != null ? hit.distance : Mathf.Infinity;
        }

        /// <summary>
        /// Checks if the enemy and player are on the same horizontal platform tier
        /// (i.e. their Y positions are within tolerance).
        /// </summary>
        public static bool IsOnSameLevel(Transform enemy, Transform player, float yTolerance = 0.8f)
        {
            if (enemy == null || player == null) return false;
            return Mathf.Abs(enemy.position.y - player.position.y) <= yTolerance;
        }

        /// <summary>
        /// Returns the nearest wall direction (-1 left / +1 right / 0 none)
        /// by casting short rays horizontally.
        /// </summary>
        public static float GetWallDirection(Vector3 position, LayerMask groundLayer, float castDist = 0.4f)
        {
            bool leftWall  = Physics2D.Raycast(position, Vector2.left,  castDist, groundLayer).collider != null;
            bool rightWall = Physics2D.Raycast(position, Vector2.right, castDist, groundLayer).collider != null;

            if (leftWall)  return -1f;
            if (rightWall) return  1f;
            return 0f;
        }
    }
}
