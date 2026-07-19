using UnityEngine;

namespace HeroKnightSandbox.Enemy
{
    public class EnemyController : MonoBehaviour
    {
        [SerializeField] private HeroKnightController player;
        [SerializeField] private Platformer.Mechanics.PatrolPath patrolPath;

        // Defaults match EnemyContext's own field defaults, so existing Light Bandit
        // prefab instances (serialized before these fields existed) behave identically
        // once Unity fills them in with these same values. Heavy Bandit overrides them
        // via CreateEnemy()'s stat parameters.
        [SerializeField] private int maxHP = 3;
        [SerializeField] private float moveSpeed = 2.0f;
        [SerializeField] private int attackDamage = 1;
        [SerializeField] private float attackRange = 1.0f;
        [SerializeField] private float attackWindup = 0.4f;
        [SerializeField] private float attackCooldown = 0.4f;
        [SerializeField] private float detectionRange = 4.0f;

        private EnemyContext context;
        private EnemyState currentState;

        public PatrolState Patrol { get; private set; }
        public ChaseState Chase { get; private set; }
        public AttackState Attack { get; private set; }
        public HurtState Hurt { get; private set; }
        public DeadState Dead { get; private set; }

        private void Awake()
        {
            context = new EnemyContext
            {
                Transform = transform,
                Animator = GetComponent<Animator>(),
                PatrolPath = patrolPath,
                Player = player,
                MaxHP = maxHP,
                MoveSpeed = moveSpeed,
                AttackDamage = attackDamage,
                AttackRange = attackRange,
                AttackWindup = attackWindup,
                AttackCooldown = attackCooldown,
                DetectionRange = detectionRange,
            };
            context.CurrentHP = context.MaxHP;

            Patrol = new PatrolState(this, context);
            Chase = new ChaseState(this, context);
            Attack = new AttackState(this, context);
            Hurt = new HurtState(this, context);
            Dead = new DeadState(this, context);
        }

        private void OnEnable()
        {
            EnemyRegistry.Register(this);
        }

        private void OnDisable()
        {
            EnemyRegistry.Unregister(this);
        }

        private void Start()
        {
            ChangeState(Patrol);
        }

        private void Update()
        {
            currentState.Tick();
        }

        public void ChangeState(EnemyState next)
        {
            currentState?.Exit();
            currentState = next;
            currentState.Enter();
        }

        public void TakeDamage(int amount)
        {
            if (currentState == Dead || currentState == Hurt)
            {
                return;
            }

            context.CurrentHP -= amount;
            if (context.CurrentHP <= 0)
            {
                ChangeState(Dead);
            }
            else
            {
                ChangeState(Hurt);
            }
        }

        public Vector3 Position => context.Transform.position;
    }
}
