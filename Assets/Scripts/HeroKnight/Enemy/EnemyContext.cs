using CodeMonkey.HealthSystemCM;
using UnityEngine;

namespace HeroKnightSandbox.Enemy
{
    /// <summary>
    /// Mutable data shared across all enemy states. Owned and populated by
    /// EnemyController; states read/write it but never own it.
    /// </summary>
    public class EnemyContext
    {
        public Transform Transform;
        public Animator Animator;
        public Platformer.Mechanics.PatrolPath PatrolPath;
        public HeroKnightController Player;
        public Sprite ProjectileSprite;

        public HealthSystem Health;
        public float MoveSpeed = 2.0f;
        public float AttackRange = 1.0f;
        public float DetectionRange = 4.0f;
        public float DetectionHeightTolerance = 1.0f;
        public int AttackDamage = 1;
        public float AttackWindup = 0.4f;
        public float AttackCooldown = 0.4f;
        public float HurtDuration = 0.25f;
        public float DeathDuration = 1.0f;

        // Detection/attack range checks are otherwise a plain 2D distance, which doesn't
        // know a gap or platform edge is impassable - an enemy standing above/below the
        // player on a separate platform could still "notice" and pace at them (or, for
        // ranged, even hit them) if the horizontal component alone was close enough.
        // Gating on height keeps aggro to enemies roughly reachable at the player's level.
        public bool PlayerWithinHeight =>
            Mathf.Abs(Transform.position.y - Player.transform.position.y) <= DetectionHeightTolerance;
    }
}
