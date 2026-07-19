using UnityEngine;

namespace HeroKnightSandbox.Enemy
{
    public class PatrolState : EnemyState
    {
        private float elapsed;
        private float duration;

        public PatrolState(EnemyController controller, EnemyContext context) : base(controller, context) { }

        public override void Enter()
        {
            Vector2 start = Context.PatrolPath.transform.TransformPoint(Context.PatrolPath.startPosition);
            Vector2 end = Context.PatrolPath.transform.TransformPoint(Context.PatrolPath.endPosition);
            Vector2 current = Context.Transform.position;

            duration = Vector2.Distance(start, end) / Context.MoveSpeed;

            // Resync the ping-pong phase to match wherever the enemy actually is right
            // now (projected onto the patrol line), rather than trusting a clock that
            // may be totally disconnected from its real position. Patrol can be
            // (re-)entered from Attack/Hurt (which never move the enemy - resyncing
            // lands on the same spot, a no-op) or from Chase (which moves the enemy
            // anywhere within DetectionRange of the player, off the patrol line
            // entirely) - without this, resuming Patrol would snap straight back onto
            // the line, the same class of bug as the earlier Mover-based teleport.
            float segmentLengthSq = (end - start).sqrMagnitude;
            float p = segmentLengthSq > Mathf.Epsilon
                ? Mathf.Clamp01(Vector2.Dot(current - start, end - start) / segmentLengthSq)
                : 0f;
            elapsed = p * duration;

            Context.Animator.SetInteger("AnimState", 2);
        }

        public override void Tick()
        {
            elapsed += Time.deltaTime;
            float phase = Mathf.InverseLerp(0, duration, Mathf.PingPong(elapsed, duration));
            Vector2 target = Context.PatrolPath.transform.TransformPoint(
                Vector2.Lerp(Context.PatrolPath.startPosition, Context.PatrolPath.endPosition, phase));

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
            else if (distance < Context.DetectionRange)
            {
                Controller.ChangeState(Controller.Chase);
            }
        }
    }
}
