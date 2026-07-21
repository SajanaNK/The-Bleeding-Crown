using HeroKnightSandbox.Audio;
using UnityEngine;

namespace HeroKnightSandbox.Enemy
{
    public class RangedAttackState : EnemyState
    {
        private float timer;
        private bool hasFired;

        public RangedAttackState(EnemyController controller, EnemyContext context) : base(controller, context) { }

        public override void Enter()
        {
            float dx = Context.Player.transform.position.x - Context.Transform.position.x;
            if (Mathf.Abs(dx) > Mathf.Epsilon)
            {
                Vector3 scale = Context.Transform.localScale;
                scale.x = dx > 0f ? -1f : 1f;
                Context.Transform.localScale = scale;
            }

            timer = 0f;
            hasFired = false;
            // No throw animation exists in the vendor pack - reusing the melee swing
            // clip/trigger so a projectile still appears in sync with a visible attack.
            Context.Animator.SetTrigger("Attack");
            RandomAudioPlayer.Play(Context.AudioSource, Context.AttackClips);
        }

        public override void Tick()
        {
            timer += Time.deltaTime;

            if (!hasFired && timer >= Context.AttackWindup)
            {
                hasFired = true;
                // localScale.x < 0 means facing right (see PatrolState/AttackState's
                // shared convention), so the projectile flies the opposite sign.
                float direction = Context.Transform.localScale.x < 0f ? 1f : -1f;
                Projectile.Spawn(Context.Transform.position, direction, Context.Player, Context.AttackDamage,
                    Context.ProjectileSprite, visualHeight: Context.ProjectileHeight);
            }

            if (timer >= Context.AttackWindup + Context.AttackCooldown)
            {
                // See AttackState's matching comment - re-check distance instead of always
                // falling back to Patrol, which would otherwise snap this enemy back onto
                // its patrol line after every single shot even with the player still in range.
                float distance = Vector2.Distance(Context.Transform.position, Context.Player.transform.position);
                Controller.ChangeState(Context.PlayerWithinHeight && distance <= Context.DetectionRange
                    ? Controller.Chase
                    : Controller.Patrol);
            }
        }
    }
}
