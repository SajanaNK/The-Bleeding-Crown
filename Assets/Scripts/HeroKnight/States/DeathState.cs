using UnityEngine;

namespace HeroKnightSandbox.States
{
    public class DeathState : PlayerState
    {
        private float timer;

        public DeathState(HeroKnightController controller, HeroKnightContext context) : base(controller, context) { }

        public override void Enter()
        {
            timer = 0f;
            Context.Body.velocity = Vector2.zero;
            Context.Body.bodyType = RigidbodyType2D.Kinematic;
            Context.Animator.SetTrigger("Death");
        }

        public override void Tick()
        {
            timer += Time.deltaTime;
            if (timer >= Context.DeathDuration)
            {
                Controller.Respawn();
            }
        }
    }
}
