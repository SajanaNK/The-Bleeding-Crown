using UnityEngine;

namespace HeroKnightSandbox.Enemy
{
    public class PatrolState : EnemyState
    {
        private Platformer.Mechanics.PatrolPath.Mover mover;

        public PatrolState(EnemyController controller, EnemyContext context) : base(controller, context) { }

        public override void Enter()
        {
            // Created once and reused across every re-entry (not recreated per Enter()):
            // Mover's Position is a function of real elapsed time since creation, so a
            // fresh Mover here would restart the oscillation from PatrolPath.startPosition
            // every time Patrol resumes after Attack/Hurt, snapping the enemy back to its
            // patrol range's start point instead of continuing from its current position.
            if (mover == null)
            {
                mover = Context.PatrolPath.CreateMover(Context.MoveSpeed);
            }

            Context.Animator.SetInteger("AnimState", 2);
        }

        public override void Tick()
        {
            Vector2 target = mover.Position;
            float dx = target.x - Context.Transform.position.x;
            if (Mathf.Abs(dx) > Mathf.Epsilon)
            {
                Vector3 scale = Context.Transform.localScale;
                scale.x = dx > 0f ? -1f : 1f;
                Context.Transform.localScale = scale;
            }

            Context.Transform.position = new Vector3(target.x, target.y, Context.Transform.position.z);

            float distance = Vector2.Distance(Context.Transform.position, Context.Player.transform.position);
            if (distance < Context.AttackRange)
            {
                Controller.ChangeState(Controller.Attack);
            }
        }
    }
}
