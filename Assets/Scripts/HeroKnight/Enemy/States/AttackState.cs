using HeroKnightSandbox.Audio;
using UnityEngine;

namespace HeroKnightSandbox.Enemy
{
    public class AttackState : EnemyState
    {
        private float timer;
        private bool hasDealtDamage;

        public AttackState(EnemyController controller, EnemyContext context) : base(controller, context) { }

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
            hasDealtDamage = false;
            Context.Animator.SetTrigger("Attack");
            RandomAudioPlayer.Play(Context.AudioSource, Context.AttackClips);
        }

        public override void Tick()
        {
            timer += Time.deltaTime;

            if (!hasDealtDamage && timer >= Context.AttackWindup)
            {
                hasDealtDamage = true;
                float distance = Vector2.Distance(Context.Transform.position, Context.Player.transform.position);
                if (distance < Context.AttackRange)
                {
                    Context.Player.TakeDamage(Context.AttackDamage);
                }
            }

            if (timer >= Context.AttackWindup + Context.AttackCooldown)
            {
                // Re-check distance rather than always falling back to Patrol - Patrol's
                // phase-resync snaps this enemy's position onto its (small) patrol line the
                // instant it re-enters, which yanked the enemy away from a player standing
                // right next to it after every single swing (chase out, hit once, snap
                // back, re-detect, repeat) - looked exactly like it could never leave its
                // patrol area.
                float distance = Vector2.Distance(Context.Transform.position, Context.Player.transform.position);
                Controller.ChangeState(Context.PlayerWithinHeight && distance <= Context.DetectionRange
                    ? Controller.Chase
                    : Controller.Patrol);
            }
        }
    }
}
