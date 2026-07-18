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

        public virtual void Enter() { }
        public virtual void Tick() { }
        public virtual void FixedTick() { }
        public virtual void Exit() { }
    }
}
