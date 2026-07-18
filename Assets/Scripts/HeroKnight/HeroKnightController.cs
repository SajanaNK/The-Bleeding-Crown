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

        private HeroKnightContext context;
        private PlayerState currentState;

        public IdleState Idle { get; private set; }
        public RunState Run { get; private set; }

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
            };

            Idle = new IdleState(this, context);
            Run = new RunState(this, context);
        }

        private void Start()
        {
            ChangeState(Idle);
        }

        private void Update()
        {
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
