using UnityEngine;

namespace HeroKnightSandbox.States
{
    public class RunState : PlayerState
    {
        public RunState(HeroKnightController controller, HeroKnightContext context) : base(controller, context) { }

        public override void Enter()
        {
            Context.Animator.SetInteger("AnimState", 1);
        }

        public override void Tick()
        {
            Context.UpdateFacing();

            if (Context.Controls.BlockHeld)
            {
                Controller.ChangeState(Controller.Block);
                return;
            }

            if (Context.Controls.RollPressed)
            {
                Controller.ChangeState(Controller.Roll);
                return;
            }

            if (!Context.IsGrounded)
            {
                Controller.ChangeState(Controller.Fall);
                return;
            }

            if (Context.Controls.JumpPressed)
            {
                Controller.ChangeState(Controller.Jump);
                return;
            }

            if (Mathf.Abs(Context.Controls.MoveX) <= Mathf.Epsilon)
            {
                Controller.ChangeState(Controller.Idle);
            }
        }

        public override void FixedTick()
        {
            Context.SetVelocityX(Context.Controls.MoveX * Context.MoveSpeed);
        }
    }
}
