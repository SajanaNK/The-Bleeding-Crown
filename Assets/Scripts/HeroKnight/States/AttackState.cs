using HeroKnightSandbox.Audio;
using HeroKnightSandbox.Enemy;
using UnityEngine;

namespace HeroKnightSandbox.States
{
    public class AttackState : PlayerState
    {
        private const float ExitDelay = 0.4f;
        private const float AttackHitWindow = 0.2f;

        private bool hasHitThisSwing;

        public AttackState(HeroKnightController controller, HeroKnightContext context) : base(controller, context) { }

        public override void Enter()
        {
            Context.SetVelocityX(0f);
            hasHitThisSwing = false;

            Context.ComboCount++;
            if (Context.ComboCount > 3 || Context.TimeSinceAttack > Context.AttackComboResetWindow)
            {
                Context.ComboCount = 1;
            }

            Context.Animator.SetTrigger("Attack" + Context.ComboCount);
            Context.TimeSinceAttack = 0f;
            // Exclusive: attack clips run ~2s against a 0.25s combo window, so mashing
            // through a combo piled up several full-length clips at once (the same bug
            // footsteps/block had - see RandomAudioPlayer.PlayExclusive). Each new swing
            // now cuts the previous swing's sound off instead of layering onto it.
            RandomAudioPlayer.PlayExclusive(Context.AudioSource, Context.AttackClips);
        }

        public override void Tick()
        {
            if (!hasHitThisSwing && Context.TimeSinceAttack > AttackHitWindow)
            {
                hasHitThisSwing = true;
                TryHitEnemy();
            }

            if (Context.Controls.AttackPressed && Context.TimeSinceAttack > Context.AttackComboWindow)
            {
                Controller.ChangeState(Controller.Attack);
                return;
            }

            if (Context.TimeSinceAttack > ExitDelay)
            {
                Controller.ChangeState(Context.IsGrounded
                    ? (Mathf.Abs(Context.Controls.MoveX) > Mathf.Epsilon ? (PlayerState)Controller.Run : Controller.Idle)
                    : Controller.Fall);
            }
        }

        private void TryHitEnemy()
        {
            foreach (EnemyController enemy in EnemyRegistry.All)
            {
                Vector3 offset = enemy.Position - Context.Transform.position;
                // Full 2D distance, not just X: an X-only check let attacks land on an
                // enemy standing on a platform directly overhead (or below), regardless
                // of how large the vertical gap actually was.
                if (offset.magnitude <= Context.AttackHitRadius && Mathf.Sign(offset.x) == Context.FacingDirection)
                {
                    enemy.TakeDamage(Context.AttackDamage);
                    return;
                }
            }
        }
    }
}
