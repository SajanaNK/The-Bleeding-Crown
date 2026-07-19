using UnityEngine;

namespace HeroKnightSandbox.Enemy
{
    public class ChaseState : EnemyState
    {
        public ChaseState(EnemyController controller, EnemyContext context) : base(controller, context) { }

        public override void Enter()
        {
            Context.Animator.SetInteger("AnimState", 2);
        }

        public override void Tick()
        {
            Vector2 current = Context.Transform.position;
            Vector2 playerPosition = Context.Player.transform.position;

            float dx = playerPosition.x - current.x;
            if (Mathf.Abs(dx) > Mathf.Epsilon)
            {
                Vector3 scale = Context.Transform.localScale;
                scale.x = dx > 0f ? -1f : 1f;
                Context.Transform.localScale = scale;
            }

            // Clamped to the patrol path's own X extent - the only ground this enemy
            // knows for certain is solid, since it (like Patrol) has no ground sensor of
            // its own. Without this, chasing the player across a gap or off a ledge left
            // the enemy hanging in open air: it's Kinematic (never falls) and this loop
            // held its Y fixed, so it just floated wherever the player's X led it.
            Vector2 lineStart = Context.PatrolPath.transform.TransformPoint(Context.PatrolPath.startPosition);
            Vector2 lineEnd = Context.PatrolPath.transform.TransformPoint(Context.PatrolPath.endPosition);
            float minX = Mathf.Min(lineStart.x, lineEnd.x);
            float maxX = Mathf.Max(lineStart.x, lineEnd.x);
            float targetX = Mathf.Clamp(playerPosition.x, minX, maxX);

            Vector2 next = Vector2.MoveTowards(current, new Vector2(targetX, current.y), Context.MoveSpeed * Time.deltaTime);
            Context.Transform.position = new Vector3(next.x, next.y, Context.Transform.position.z);

            float distance = Vector2.Distance(Context.Transform.position, Context.Player.transform.position);
            if (distance < Context.AttackRange)
            {
                Controller.ChangeState(Controller.Attack);
                return;
            }

            if (distance > Context.DetectionRange)
            {
                Controller.ChangeState(Controller.Patrol);
            }
        }
    }
}
