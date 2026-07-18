using UnityEngine;

namespace HeroKnightSandbox.States
{
    public class JumpState : PlayerState
    {
        public JumpState(HeroKnightController controller, HeroKnightContext context) : base(controller, context) { }

        public override void Enter()
        {
            Context.Animator.SetTrigger("Jump");
            Context.Animator.SetBool("Grounded", false);
            Context.Body.velocity = new Vector2(Context.Body.velocity.x, Context.JumpForce);
            Context.GroundSensor.Disable(0.2f);
        }

        public override void Tick()
        {
            Context.UpdateFacing();
            Context.Animator.SetFloat("AirSpeedY", Context.Body.velocity.y);

            if (Context.Body.velocity.y <= 0f)
            {
                Controller.ChangeState(Controller.Fall);
            }
        }

        public override void FixedTick()
        {
            Context.SetVelocityX(Context.Controls.MoveX * Context.MoveSpeed);
        }
    }
}
