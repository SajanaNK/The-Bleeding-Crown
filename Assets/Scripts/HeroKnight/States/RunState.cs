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

            if (TryGroundedActionTransitions())
            {
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
