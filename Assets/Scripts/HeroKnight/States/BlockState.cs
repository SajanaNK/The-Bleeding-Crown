using HeroKnightSandbox.Audio;
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
            // Exclusive: player_block_3.mp3 runs 8s, long enough that rapidly toggling
            // block would otherwise stack several full-length clips (the same bug
            // footsteps had - see RandomAudioPlayer.PlayExclusive).
            RandomAudioPlayer.PlayExclusive(Context.AudioSource, Context.BlockClips);
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
