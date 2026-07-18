using UnityEngine;

namespace HeroKnightSandbox.States
{
    public class FallState : PlayerState
    {
        public FallState(HeroKnightController controller, HeroKnightContext context) : base(controller, context) { }

        public override void Tick()
        {
            Context.UpdateFacing();
            Context.Animator.SetFloat("AirSpeedY", Context.Body.velocity.y);

            if (Context.IsGrounded)
            {
                Context.Animator.SetBool("Grounded", true);
                Controller.ChangeState(Mathf.Abs(Context.Controls.MoveX) > Mathf.Epsilon
                    ? (PlayerState)Controller.Run
                    : Controller.Idle);
                return;
            }

            if (LedgeGrabState.CanGrab(Context))
            {
                Controller.ChangeState(Controller.LedgeGrab);
                return;
            }

            if (Context.IsWallSliding)
            {
                Controller.ChangeState(Controller.WallSlide);
            }
        }

        public override void FixedTick()
        {
            Context.SetVelocityX(Context.Controls.MoveX * Context.MoveSpeed);
        }
    }
}
