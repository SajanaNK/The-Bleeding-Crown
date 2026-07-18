using UnityEngine;

namespace HeroKnightSandbox.States
{
    public class LedgeGrabState : PlayerState
    {
        private const float RegrabCooldown = 0.3f;

        private bool grabbedRight;

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
            grabbedRight = Context.WallSensorR1.State() && Context.WallSensorR2.State() && !Context.LedgeSensorR.State();
            float snappedX = grabbedRight
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
                // Climb direction must follow which side was actually grabbed, not
                // Context.FacingDirection -- that only updates from live joystick input
                // (via UpdateFacing()), so it can still point away from the grabbed wall
                // (e.g. the player holds away from the wall while sliding, or the grab
                // was entered without any preceding movement at all), which previously
                // sent the climb offset the wrong direction, off the ledge into open air.
                int climbDirection = grabbedRight ? 1 : -1;
                Vector3 offset = new Vector3(
                    Context.LedgeClimbOffset.x * climbDirection,
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
