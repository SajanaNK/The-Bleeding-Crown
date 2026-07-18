// Assets/Scripts/HeroKnight/States/LedgeGrabState.cs
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
                Context.LedgeSensorR.Disable(RegrabCooldown);
                Context.LedgeSensorL.Disable(RegrabCooldown);
                Controller.ChangeState(Controller.Fall);
            }
        }
    }
}
