using UnityEngine;

namespace HeroKnightSandbox.Enemy
{
    public class DeadState : EnemyState
    {
        private float timer;

        public DeadState(EnemyController controller, EnemyContext context) : base(controller, context) { }

        public override void Enter()
        {
            timer = 0f;
            Context.Animator.SetTrigger("Death");
        }

        public override void Tick()
        {
            timer += Time.deltaTime;
            if (timer >= Context.DeathDuration)
            {
                Object.Destroy(Controller.gameObject);
            }
        }
    }
}
