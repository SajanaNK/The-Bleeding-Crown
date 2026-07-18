using UnityEngine;

namespace HeroKnightSandbox.States
{
    public class BlockState : PlayerState
    {
        public BlockState(HeroKnightController controller, HeroKnightContext context) : base(controller, context) { }

        public override void Enter()
        {
            Context.SetVelocityX(0f);
            Context.Animator.SetTrigger("Block");
            Context.Animator.SetBool("IdleBlock", true);
        }

        public override void Exit()
        {
            Context.Animator.SetBool("IdleBlock", false);
        }

        public override void Tick()
        {
            if (!Context.Controls.BlockHeld)
            {
                Controller.ChangeState(Mathf.Abs(Context.Controls.MoveX) > Mathf.Epsilon
                    ? (PlayerState)Controller.Run
                    : Controller.Idle);
            }
        }
    }
}
