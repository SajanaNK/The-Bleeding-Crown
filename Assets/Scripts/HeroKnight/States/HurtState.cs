using UnityEngine;

namespace HeroKnightSandbox.States
{
    public class HurtState : PlayerState
    {
        private float timer;

        public HurtState(HeroKnightController controller, HeroKnightContext context) : base(controller, context) { }

        public override void Enter()
        {
            timer = 0f;
            Context.SetVelocityX(0f);
            Context.Animator.SetTrigger("Hurt");
        }

        public override void Tick()
        {
            timer += Time.deltaTime;
            if (timer > Context.HurtDuration)
            {
                Controller.ChangeState(Context.IsGrounded
                    ? (Mathf.Abs(Context.Controls.MoveX) > Mathf.Epsilon ? (PlayerState)Controller.Run : Controller.Idle)
                    : Controller.Fall);
            }
        }
    }
}
