namespace HeroKnightSandbox.States
{
    public abstract class PlayerState
    {
        protected readonly HeroKnightController Controller;
        protected readonly HeroKnightContext Context;

        protected PlayerState(HeroKnightController controller, HeroKnightContext context)
        {
            Controller = controller;
            Context = context;
        }

        /// <summary>
        /// Shared Attack/Block/Roll/Fall/Jump transition checks used by both
        /// IdleState and RunState. Returns true if a transition was made.
        /// </summary>
        protected bool TryGroundedActionTransitions()
        {
            if (Context.Controls.AttackPressed && Context.TimeSinceAttack > Context.AttackComboWindow)
            {
                Controller.ChangeState(Controller.Attack);
                return true;
            }

            if (Context.Controls.BlockHeld)
            {
                Controller.ChangeState(Controller.Block);
                return true;
            }

            if (Context.Controls.RollPressed)
            {
                Controller.ChangeState(Controller.Roll);
                return true;
            }

            if (!Context.IsGrounded)
            {
                Controller.ChangeState(Controller.Fall);
                return true;
            }

            if (Context.Controls.JumpPressed)
            {
                Controller.ChangeState(Controller.Jump);
                return true;
            }

            return false;
        }

        public virtual void Enter() { }
        public virtual void Tick() { }
        public virtual void FixedTick() { }
        public virtual void Exit() { }
    }
}
