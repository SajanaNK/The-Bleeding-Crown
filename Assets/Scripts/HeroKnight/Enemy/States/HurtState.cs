using UnityEngine;

namespace HeroKnightSandbox.Enemy
{
    public class HurtState : EnemyState
    {
        private float timer;

        public HurtState(EnemyController controller, EnemyContext context) : base(controller, context) { }

        public override void Enter()
        {
            timer = 0f;
            Context.Animator.SetTrigger("Hurt");
        }

        public override void Tick()
        {
            timer += Time.deltaTime;
            if (timer >= Context.HurtDuration)
            {
                Controller.ChangeState(Controller.Patrol);
            }
        }
    }
}
