using UnityEngine;

namespace HeroKnightSandbox.Enemy
{
    public class ChaseState : EnemyState
    {
        // Ground probe used to confirm a step is safe before taking it - see HasGroundBelow.
        private const float ProbeStartHeight = 0.5f;
        private const float ProbeDistance = 0.5f;

        // Reused every HasGroundBelow() call instead of letting Physics2D.RaycastAll
        // allocate a fresh array every frame for every chasing enemy - that was a steady
        // GC churn source, worst exactly when jumping near enemies (height/distance
        // changes are what toggle Chase on/off, so a jump can flip several enemies into
        // this state and its per-frame raycast at once).
        private readonly RaycastHit2D[] groundProbeResults = new RaycastHit2D[8];

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

            Vector2 desired = Vector2.MoveTowards(current, new Vector2(playerPosition.x, current.y), Context.MoveSpeed * Time.deltaTime);
            Vector2 next = HasGroundBelow(desired) ? desired : current;
            Context.Transform.position = new Vector3(next.x, next.y, Context.Transform.position.z);

            float distance = Vector2.Distance(Context.Transform.position, Context.Player.transform.position);
            if (Context.PlayerWithinHeight && distance < Context.AttackRange)
            {
                Controller.ChangeState(Controller.Attack);
                return;
            }

            if (!Context.PlayerWithinHeight || distance > Context.DetectionRange)
            {
                Controller.ChangeState(Controller.Patrol);
            }
        }

        // Kinematic and with no ground sensor of its own (unlike the player), Chase is the
        // one state that can walk this enemy anywhere within DetectionRange - potentially
        // well beyond its patrol path, across a gap, or off a ledge. A short downward probe
        // from just above the candidate position confirms solid (non-trigger) ground is
        // actually there before committing to the step, so it can safely chase across the
        // whole platform it's standing on without ever stepping into open air (it would
        // just hang there, since it never falls).
        private bool HasGroundBelow(Vector2 position)
        {
            Vector2 origin = position + Vector2.up * ProbeStartHeight;
            int hitCount = Physics2D.RaycastNonAlloc(origin, Vector2.down, groundProbeResults, ProbeStartHeight + ProbeDistance);
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit2D hit = groundProbeResults[i];
                // The probe origin sits inside this enemy's own body collider (it's
                // centered roughly on the sprite, well above its feet), so an unfiltered
                // Raycast always self-hits at distance 0 and never actually reaches the
                // ground below - silently turning this whole check into a no-op that
                // always said "yes, ground". Skipping own/trigger colliders here is what
                // makes the probe mean anything.
                if (hit.collider.isTrigger || hit.collider.transform == Context.Transform
                    || hit.collider.transform.IsChildOf(Context.Transform))
                {
                    continue;
                }

                return true;
            }

            return false;
        }
    }
}
