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

        public int MaxHP = 3;
        public int CurrentHP;
        public float MoveSpeed = 2.0f;
        public float AttackRange = 1.0f;
        public float DetectionRange = 4.0f;
        public int AttackDamage = 1;
        public float AttackWindup = 0.4f;
        public float AttackCooldown = 0.4f;
        public float HurtDuration = 0.25f;
        public float DeathDuration = 1.0f;
    }
}
