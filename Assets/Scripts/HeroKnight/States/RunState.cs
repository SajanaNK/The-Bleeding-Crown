using HeroKnightSandbox.Audio;
using UnityEngine;

namespace HeroKnightSandbox.States
{
    public class RunState : PlayerState
    {
        private float footstepTimer;

        public RunState(HeroKnightController controller, HeroKnightContext context) : base(controller, context) { }

        public override void Enter()
        {
            Context.Animator.SetInteger("AnimState", 1);
            // Starts at the interval (not 0) so a footstep plays immediately on the first
            // step rather than waiting a full FootstepInterval after Run begins.
            footstepTimer = Context.FootstepInterval;
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
                return;
            }

            footstepTimer += Time.deltaTime;
            if (footstepTimer >= Context.FootstepInterval)
            {
                footstepTimer = 0f;
                // Exclusive: footstep clips can run longer than FootstepInterval, and
                // isPlaying doesn't reliably track PlayOneShot voices to gate on instead
                // (see RandomAudioPlayer.PlayExclusive) - this cuts the previous footstep
                // off cleanly rather than layering a new one on top of it.
                RandomAudioPlayer.PlayExclusive(Context.AudioSource, Context.FootstepClips);
            }
        }

        public override void FixedTick()
        {
            Context.SetVelocityX(Context.Controls.MoveX * Context.MoveSpeed);
        }
    }
}
