// Assets/Scripts/HeroKnight/States/RollState.cs
using UnityEngine;

namespace HeroKnightSandbox.States
{
    public class RollState : PlayerState
    {
        private float timer;

        public RollState(HeroKnightController controller, HeroKnightContext context) : base(controller, context) { }

        public override void Enter()
        {
            timer = 0f;
            Context.Animator.SetTrigger("Roll");
            Context.Body.velocity = new Vector2(Context.FacingDirection * Context.RollForce, Context.Body.velocity.y);
        }

        public override void Tick()
        {
            timer += Time.deltaTime;

            if (timer < Context.RollDuration)
            {
                return;
            }

            if (!Context.IsGrounded)
            {
                Controller.ChangeState(Controller.Fall);
                return;
            }

            Controller.ChangeState(Mathf.Abs(Context.Controls.MoveX) > Mathf.Epsilon
                ? (PlayerState)Controller.Run
                : Controller.Idle);
        }
    }
}
