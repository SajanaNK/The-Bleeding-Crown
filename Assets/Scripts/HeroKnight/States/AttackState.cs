using UnityEngine;

namespace HeroKnightSandbox.States
{
    public class AttackState : PlayerState
    {
        private const float ExitDelay = 0.4f;

        public AttackState(HeroKnightController controller, HeroKnightContext context) : base(controller, context) { }

        public override void Enter()
        {
            Context.SetVelocityX(0f);

            Context.ComboCount++;
            if (Context.ComboCount > 3 || Context.TimeSinceAttack > Context.AttackComboResetWindow)
            {
                Context.ComboCount = 1;
            }

            Context.Animator.SetTrigger("Attack" + Context.ComboCount);
            Context.TimeSinceAttack = 0f;
        }

        public override void Tick()
        {
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
    }
}
