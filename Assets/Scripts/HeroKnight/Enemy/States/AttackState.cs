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
