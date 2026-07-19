using UnityEngine;

namespace HeroKnightSandbox.Enemy
{
    public class PatrolState : EnemyState
    {
        private Platformer.Mechanics.PatrolPath.Mover mover;

        public PatrolState(EnemyController controller, EnemyContext context) : base(controller, context) { }

        public override void Enter()
        {
            mover = Context.PatrolPath.CreateMover(Context.MoveSpeed);
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
