using UnityEngine;

namespace HeroKnightSandbox.States
{
    public class IdleState : PlayerState
    {
        public IdleState(HeroKnightController controller, HeroKnightContext context) : base(controller, context) { }

        public override void Enter()
        {
            Context.SetVelocityX(0f);
            Context.Animator.SetInteger("AnimState", 0);
        }

        public override void Tick()
        {
            Context.UpdateFacing();

            if (Context.Controls.AttackPressed && Context.TimeSinceAttack > Context.AttackComboWindow)
            {
                Controller.ChangeState(Controller.Attack);
                return;
            }

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

            if (Mathf.Abs(Context.Controls.MoveX) > Mathf.Epsilon)
            {
                Controller.ChangeState(Controller.Run);
            }
        }
    }
}
