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
            timer = 0f;
            hasDealtDamage = false;
            Context.Animator.SetTrigger("Attack");
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
                Controller.ChangeState(Controller.Patrol);
            }
        }
    }
}
