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
            // Snapping to the wall sensor's own X sits the character's whole body
            // (roughly half its collider width) overlapping the wall's footprint, not
            // hanging beside it -- the sensor's position marks where the *hand* should
            // reach, not where the body's center belongs. Pulling back by
            // LedgeHangOffset, away from the wall on the grabbed side, keeps the body
            // clear while the hands still read as gripping the edge.
            float wallX = grabbedRight
                ? Context.WallSensorR2.transform.position.x
                : Context.WallSensorL2.transform.position.x;
            float snappedX = wallX - (grabbedRight ? Context.LedgeHangOffset : -Context.LedgeHangOffset);
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
                // Without this, a climb that lands slightly above the true resting
                // surface (gravity hasn't settled it onto solid ground yet -- confirmed
                // live: Grounded read false immediately after climbing) lets the very
                // next brief fall re-touch the wall sensors before the character clears
                // them, re-triggering CanGrab() and looping straight back into
                // LedgeGrabState. The drop branch below already disables the sensors for
                // this same reason; the climb branch needs the identical guard.
                Context.WallSensorR1.Disable(RegrabCooldown);
                Context.WallSensorR2.Disable(RegrabCooldown);
                Context.WallSensorL1.Disable(RegrabCooldown);
                Context.WallSensorL2.Disable(RegrabCooldown);
                // A dedicated one-shot trigger, not a comparison against AnimState/Grounded:
                // both of those already hold the "exit" value throughout the whole grab
                // (AnimState is untouched since before the wall-slide; Grounded is false
                // the entire time we're hanging), so a condition-based exit transition on
                // either one fires almost immediately on entering the LedgeGrab animator
                // state rather than waiting for this actual climb decision.
                Context.Animator.SetTrigger("LedgeClimb");
                Controller.ChangeState(Controller.Idle);
                return;
            }

            if (Context.Controls.RollPressed)
            {
                Context.WallSensorR1.Disable(RegrabCooldown);
                Context.WallSensorR2.Disable(RegrabCooldown);
                Context.WallSensorL1.Disable(RegrabCooldown);
                Context.WallSensorL2.Disable(RegrabCooldown);
                Context.Animator.SetTrigger("LedgeDrop");
                Controller.ChangeState(Controller.Fall);
            }
        }
    }
}
