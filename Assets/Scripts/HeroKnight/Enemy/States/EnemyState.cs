namespace HeroKnightSandbox.Enemy
{
    public abstract class EnemyState
    {
        protected readonly EnemyController Controller;
        protected readonly EnemyContext Context;

        protected EnemyState(EnemyController controller, EnemyContext context)
        {
            Controller = controller;
            Context = context;
        }

        public virtual void Enter() { }
        public virtual void Exit() { }
        public virtual void Tick() { }
    }
}
