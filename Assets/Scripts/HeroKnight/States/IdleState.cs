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

            if (TryGroundedActionTransitions())
            {
                return;
            }

            if (Mathf.Abs(Context.Controls.MoveX) > Mathf.Epsilon)
            {
                Controller.ChangeState(Controller.Run);
            }
        }
    }
}
