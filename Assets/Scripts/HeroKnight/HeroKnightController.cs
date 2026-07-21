using Cinemachine;
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
    [RequireComponent(typeof(AudioSource))]
    public class HeroKnightController : MonoBehaviour, IGetHealthSystem
    {
        [SerializeField] private TouchHeroKnightInput input;
        [SerializeField] private AudioClip[] attackClips;
        [SerializeField] private AudioClip[] blockClips;
        [SerializeField] private AudioClip[] jumpClips;
        [SerializeField] private AudioClip[] footstepClips;
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
        // The vendor asset pack's HeroKnight_WallSlide.anim has an AE_SlideDust
        // AnimationEvent baked in partway through its loop - this is the vendor's own
        // SlideDust.prefab (self-destroying via its own animation's destroyEvent), never
        // removed when this project's custom state machine replaced the vendor's
        // monolithic Demo/HeroKnight.cs script. Without a receiver, Unity logs "has no
        // receiver" every time the clip plays.
        [SerializeField] private GameObject slideDustPrefab;
        // Only used to notify Cinemachine of the instant teleport in Respawn() below -
        // without this, CinemachineFramingTransposer reads the sudden death-site-to-
        // spawn-position jump as the target actually moving that far in one frame and
        // spends the next several frames rapidly panning/whipping the camera across the
        // whole level to "catch up", which reads as screen glitching and drags in enough
        // off-screen geometry at once to cause an FPS drop.
        [SerializeField] private CinemachineVirtualCamera followCamera;

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
                AudioSource = GetComponent<AudioSource>(),
                AttackClips = attackClips,
                BlockClips = blockClips,
                JumpClips = jumpClips,
                FootstepClips = footstepClips,
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
            Vector3 previousPosition = context.Transform.position;
            context.Health.HealComplete();
            context.Body.bodyType = RigidbodyType2D.Dynamic;
            context.Body.velocity = Vector2.zero;
            context.Transform.position = spawnPosition;
            if (followCamera != null)
            {
                followCamera.OnTargetObjectWarped(context.Transform, spawnPosition - previousPosition);
            }
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

        // Animation Event, fired by HeroKnight_WallSlide.anim partway through its loop -
        // matches the vendor Demo/HeroKnight.cs's own AE_SlideDust spawn-position/flip
        // logic, ported to this project's Context-held facing direction and sensors.
        private void AE_SlideDust()
        {
            if (slideDustPrefab == null)
            {
                return;
            }

            HeroKnightSandbox.Sensors.Sensor_HeroKnight sensor =
                context.FacingDirection == 1 ? context.WallSensorR2 : context.WallSensorL2;

            GameObject dust = Instantiate(slideDustPrefab, sensor.transform.position, transform.localRotation);
            dust.transform.localScale = new Vector3(context.FacingDirection, 1f, 1f);
        }
    }
}
