using CodeMonkey.HealthSystemCM;
using HeroKnightSandbox.Input;
using HeroKnightSandbox.Sensors;
using HeroKnightSandbox.States;
using UnityEngine;

namespace HeroKnightSandbox
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class HeroKnightController : MonoBehaviour, IGetHealthSystem
    {
        [SerializeField] private TouchHeroKnightInput input;
        // Fully qualified rather than relying on the `using HeroKnightSandbox.Sensors;`
        // import above: the vendor asset pack's Demo/Sensor_HeroKnight.cs declares a
        // same-named class directly in the global namespace, and C# always resolves a
        // bare type name against an enclosing namespace's own global-scope declarations
        // before it ever considers a `using` import for that name. An unqualified
        // `Sensor_HeroKnight` here silently binds to the vendor's class instead of ours
        // -- no compile error, just a field of the wrong type that later refuses any
        // SerializedProperty assignment of our actual sensor component.
        [SerializeField] private HeroKnightSandbox.Sensors.Sensor_HeroKnight groundSensor;
        [SerializeField] private HeroKnightSandbox.Sensors.Sensor_HeroKnight wallSensorR1;
        [SerializeField] private HeroKnightSandbox.Sensors.Sensor_HeroKnight wallSensorR2;
        [SerializeField] private HeroKnightSandbox.Sensors.Sensor_HeroKnight wallSensorL1;
        [SerializeField] private HeroKnightSandbox.Sensors.Sensor_HeroKnight wallSensorL2;
        [SerializeField] private HeroKnightSandbox.Sensors.Sensor_HeroKnight ledgeSensorR;
        [SerializeField] private HeroKnightSandbox.Sensors.Sensor_HeroKnight ledgeSensorL;

        private HeroKnightContext context;
        // Fully qualified: see the matching comment on the sensor fields above. A newly
        // added vendor package (Assets/SPUM/Core/Script/Data/SPUM_Prefabs.cs) declares
        // `public enum PlayerState` directly in the global namespace, which now silently
        // wins over the `using HeroKnightSandbox.States;` import above for every bare
        // `PlayerState` reference in this file (this class lives in the parent
        // `HeroKnightSandbox` namespace, not `.States`, so it has no same-namespace
        // declaration to fall back on the way files inside States/ do). Left unqualified,
        // ChangeState()'s parameter type silently became the SPUM enum instead of our
        // class, causing "cannot convert FallState to PlayerState" and similar errors at
        // every call site despite FallState correctly extending the real PlayerState.
        private HeroKnightSandbox.States.PlayerState currentState;

        public IdleState Idle { get; private set; }
        public RunState Run { get; private set; }
        public JumpState Jump { get; private set; }
        public FallState Fall { get; private set; }
        public WallSlideState WallSlide { get; private set; }
        public LedgeGrabState LedgeGrab { get; private set; }
        public RollState Roll { get; private set; }
        public BlockState Block { get; private set; }
        public AttackState Attack { get; private set; }
        public HurtState Hurt { get; private set; }
        public DeathState Death { get; private set; }

        private Vector3 spawnPosition;

        private void Awake()
        {
            context = new HeroKnightContext
            {
                Body = GetComponent<Rigidbody2D>(),
                Animator = GetComponent<Animator>(),
                SpriteRenderer = GetComponent<SpriteRenderer>(),
                Transform = transform,
                Controls = input,
                GroundSensor = groundSensor,
                WallSensorR1 = wallSensorR1,
                WallSensorR2 = wallSensorR2,
                WallSensorL1 = wallSensorL1,
                WallSensorL2 = wallSensorL2,
                LedgeSensorR = ledgeSensorR,
                LedgeSensorL = ledgeSensorL,
            };

            Idle = new IdleState(this, context);
            Run = new RunState(this, context);
            Jump = new JumpState(this, context);
            Fall = new FallState(this, context);
            WallSlide = new WallSlideState(this, context);
            LedgeGrab = new LedgeGrabState(this, context);
            Roll = new RollState(this, context);
            Block = new BlockState(this, context);
            Attack = new AttackState(this, context);
            Hurt = new HurtState(this, context);
            Death = new DeathState(this, context);

            context.Health = new HealthSystem(5f);
            spawnPosition = transform.position;
        }

        public HealthSystem GetHealthSystem() => context.Health;

        private void Start()
        {
            ChangeState(Idle);
        }

        private void Update()
        {
            context.TimeSinceAttack += Time.deltaTime;
            if (context.InvulnerabilityTimer > 0f)
            {
                context.InvulnerabilityTimer -= Time.deltaTime;
            }

            context.Animator.SetBool("Grounded", context.IsGrounded);
            currentState.Tick();
        }

        private void FixedUpdate()
        {
            currentState.FixedTick();
        }

        public void ChangeState(HeroKnightSandbox.States.PlayerState next)
        {
            currentState?.Exit();
            currentState = next;
            currentState.Enter();
        }

        public void TakeDamage(int amount)
        {
            if (context.IsInvulnerable || currentState == Hurt || currentState == Death || currentState == Block)
            {
                return;
            }

            context.Health.Damage(amount);
            if (context.Health.IsDead())
            {
                ChangeState(Death);
                return;
            }

            context.InvulnerabilityTimer = context.InvulnerabilityDuration;
            ChangeState(Hurt);
        }

        public void Respawn()
        {
            context.Health.HealComplete();
            context.Body.bodyType = RigidbodyType2D.Dynamic;
            context.Body.velocity = Vector2.zero;
            context.Transform.position = spawnPosition;
            context.InvulnerabilityTimer = context.InvulnerabilityDuration;
            // The vendor Animator Controller's Death state has no outgoing transitions of
            // its own (m_Transitions: [] - it's only ever meant to be a dead end), so
            // ChangeState(Idle) alone leaves the Animator frozen on Death's last frame even
            // though the C# state machine has already moved on. A dedicated Respawn trigger,
            // wired by HeroKnightSandboxSetup.AddDeathRespawnToAnimator(), forces it out -
            // same fix as LedgeGrab's missing exit transition.
            context.Animator.SetTrigger("Respawn");
            ChangeState(Idle);
        }
    }
}
