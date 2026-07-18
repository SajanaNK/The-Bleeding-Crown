using UnityEngine;

namespace HeroKnightSandbox.States
{
    public class LedgeGrabState : PlayerState
    {
        private const float RegrabCooldown = 0.3f;

        public LedgeGrabState(HeroKnightController controller, HeroKnightContext context) : base(controller, context) { }

        /// <summary>
        /// A ledge is grabbable when a wall sensor pair is touching a wall but the
        /// ledge sensor positioned above it is clear — the wall ends at this height.
        /// </summary>
        public static bool CanGrab(HeroKnightContext context)
        {
            bool rightLedge = context.WallSensorR1.State() && context.WallSensorR2.State() && !context.LedgeSensorR.State();
            bool leftLedge = context.WallSensorL1.State() && context.WallSensorL2.State() && !context.LedgeSensorL.State();
            return rightLedge || leftLedge;
        }

        public override void Enter()
        {
            bool rightLedge = Context.WallSensorR1.State() && Context.WallSensorR2.State() && !Context.LedgeSensorR.State();
            float snappedX = rightLedge
                ? Context.WallSensorR2.transform.position.x
                : Context.WallSensorL2.transform.position.x;
            Context.Transform.position = new Vector3(snappedX, Context.Transform.position.y, Context.Transform.position.z);

            Context.Body.bodyType = RigidbodyType2D.Kinematic;
            Context.Body.velocity = Vector2.zero;
            Context.Animator.SetTrigger("LedgeGrab");
        }

        public override void Exit()
        {
            Context.Body.bodyType = RigidbodyType2D.Dynamic;
        }

        public override void Tick()
        {
            if (Context.Controls.JumpPressed)
            {
                Vector3 offset = new Vector3(
                    Context.LedgeClimbOffset.x * Context.FacingDirection,
                    Context.LedgeClimbOffset.y,
                    0f);
                Context.Transform.position += offset;
                Controller.ChangeState(Controller.Idle);
                return;
            }

            if (Context.Controls.RollPressed)
            {
                Context.WallSensorR1.Disable(RegrabCooldown);
                Context.WallSensorR2.Disable(RegrabCooldown);
                Context.WallSensorL1.Disable(RegrabCooldown);
                Context.WallSensorL2.Disable(RegrabCooldown);
                Controller.ChangeState(Controller.Fall);
            }
        }
    }
}
