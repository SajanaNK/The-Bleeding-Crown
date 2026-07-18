using UnityEngine;

namespace HeroKnightSandbox.States
{
    public class WallSlideState : PlayerState
    {
        private const float MaxSlideSpeed = -1.5f;

        public WallSlideState(HeroKnightController controller, HeroKnightContext context) : base(controller, context) { }

        public override void Enter()
        {
            Context.Animator.SetBool("WallSlide", true);
        }

        public override void Exit()
        {
            Context.Animator.SetBool("WallSlide", false);
        }

        public override void Tick()
        {
            if (Context.IsGrounded)
            {
                Controller.ChangeState(Controller.Idle);
                return;
            }

            if (LedgeGrabState.CanGrab(Context))
            {
                Controller.ChangeState(Controller.LedgeGrab);
                return;
            }

            if (!Context.IsWallSliding)
            {
                Controller.ChangeState(Controller.Fall);
            }
        }

        public override void FixedTick()
        {
            float clampedY = Context.Body.velocity.y < MaxSlideSpeed ? MaxSlideSpeed : Context.Body.velocity.y;
            Context.Body.velocity = new Vector2(0f, clampedY);
        }
    }
}
