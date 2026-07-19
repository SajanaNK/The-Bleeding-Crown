using UnityEngine;

namespace HeroKnightSandbox.Enemy
{
    public class PatrolState : EnemyState
    {
        // Elapsed time actually spent ticking Patrol - persists across Enter/Exit (not
        // reset in Enter()) so resuming after Attack/Hurt continues the ping-pong from
        // the same phase, not from wherever it would be had it never paused.
        private float elapsed;
        private float duration;

        public PatrolState(EnemyController controller, EnemyContext context) : base(controller, context) { }

        public override void Enter()
        {
            // Recomputed every Enter (cheap) rather than cached once, in case MoveSpeed
            // or the path's endpoints ever change between visits to Patrol.
            //
            // Deliberately NOT using Platformer.Mechanics.PatrolPath.CreateMover()/Mover
            // here: Mover.Position is Mathf.PingPong(Time.time - startTime, duration) -
            // a pure function of real wall-clock time since construction, with no way to
            // pause it. While the enemy is in Attack/Hurt (which never touch Transform),
            // that clock keeps running in the background; the instant Patrol reads
            // Mover.Position again it snaps to wherever the uninterrupted clock says the
            // enemy should be by now, which can be anywhere along the patrol range -
            // confirmed live as the enemy teleporting behind the player mid-fight after
            // an Attack/Hurt cycle. Tracking our own `elapsed`, advanced only inside this
            // state's own Tick(), reproduces the same ping-pong math without that clock
            // continuing to run while paused.
            duration = (Context.PatrolPath.endPosition - Context.PatrolPath.startPosition).magnitude / Context.MoveSpeed;
            Context.Animator.SetInteger("AnimState", 2);
        }

        public override void Tick()
        {
            elapsed += Time.deltaTime;
            float p = Mathf.InverseLerp(0, duration, Mathf.PingPong(elapsed, duration));
            Vector2 target = Context.PatrolPath.transform.TransformPoint(
                Vector2.Lerp(Context.PatrolPath.startPosition, Context.PatrolPath.endPosition, p));

            float dx = target.x - Context.Transform.position.x;
            if (Mathf.Abs(dx) > Mathf.Epsilon)
            {
                Vector3 scale = Context.Transform.localScale;
                scale.x = dx > 0f ? -1f : 1f;
                Context.Transform.localScale = scale;
            }

            Context.Transform.position = new Vector3(target.x, target.y, Context.Transform.position.z);

            float distance = Vector2.Distance(Context.Transform.position, Context.Player.transform.position);
            if (distance < Context.AttackRange)
            {
                Controller.ChangeState(Controller.Attack);
            }
        }
    }
}
