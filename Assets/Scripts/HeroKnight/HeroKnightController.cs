using HeroKnightSandbox.Input;
using HeroKnightSandbox.Sensors;
using HeroKnightSandbox.States;
using UnityEngine;

namespace HeroKnightSandbox
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class HeroKnightController : MonoBehaviour
    {
        [SerializeField] private TouchHeroKnightInput input;
        [SerializeField] private Sensor_HeroKnight groundSensor;
        [SerializeField] private Sensor_HeroKnight wallSensorR1;
        [SerializeField] private Sensor_HeroKnight wallSensorR2;
        [SerializeField] private Sensor_HeroKnight wallSensorL1;
        [SerializeField] private Sensor_HeroKnight wallSensorL2;
        [SerializeField] private Sensor_HeroKnight ledgeSensorR;
        [SerializeField] private Sensor_HeroKnight ledgeSensorL;

        private HeroKnightContext context;
        private PlayerState currentState;

        public IdleState Idle { get; private set; }
        public RunState Run { get; private set; }
        public JumpState Jump { get; private set; }
        public FallState Fall { get; private set; }
        public WallSlideState WallSlide { get; private set; }
        public LedgeGrabState LedgeGrab { get; private set; }
        public RollState Roll { get; private set; }
        public BlockState Block { get; private set; }
        public AttackState Attack { get; private set; }

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
        }

        private void Start()
        {
            ChangeState(Idle);
        }

        private void Update()
        {
            context.TimeSinceAttack += Time.deltaTime;
            context.Animator.SetBool("Grounded", context.IsGrounded);
            currentState.Tick();
        }

        private void FixedUpdate()
        {
            currentState.FixedTick();
        }

        public void ChangeState(PlayerState next)
        {
            currentState?.Exit();
            currentState = next;
            currentState.Enter();
        }
    }
}
