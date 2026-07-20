using CodeMonkey.HealthSystemCM;
using HeroKnightSandbox.Health;
using UnityEngine;

namespace HeroKnightSandbox.Enemy
{
    public class EnemyController : MonoBehaviour, IGetHealthSystem
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

        // Picks RangedAttackState instead of the melee AttackState in Awake() below.
        // Default false, so existing melee prefab instances are unaffected.
        [SerializeField] private bool ranged;
        [SerializeField] private Sprite projectileSprite;
        [SerializeField] private GameObject healthBarPrefab;

        private EnemyContext context;
        private EnemyState currentState;

        public PatrolState Patrol { get; private set; }
        public ChaseState Chase { get; private set; }
        public EnemyState Attack { get; private set; }
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
                ProjectileSprite = projectileSprite,
                MoveSpeed = moveSpeed,
                AttackDamage = attackDamage,
                AttackRange = attackRange,
                AttackWindup = attackWindup,
                AttackCooldown = attackCooldown,
                DetectionRange = detectionRange,
            };
            context.Health = new HealthSystem(maxHP);

            Patrol = new PatrolState(this, context);
            Chase = new ChaseState(this, context);
            Attack = ranged ? (EnemyState)new RangedAttackState(this, context) : new AttackState(this, context);
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

            if (healthBarPrefab != null)
            {
                GameObject bar = Instantiate(healthBarPrefab);
                bar.AddComponent<EnemyHealthBarFollow>().Target = transform;
                // No getHealthSystemGameObject to assign ahead of time (this instance
                // doesn't exist until now) - CodeMonkey's HealthBarUI supports wiring
                // it directly instead; see its own SerializeField tooltip.
                bar.GetComponentInChildren<HealthBarUI>().SetHealthSystem(context.Health);
            }
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

            context.Health.Damage(amount);
            if (context.Health.IsDead())
            {
                ChangeState(Dead);
            }
            else
            {
                ChangeState(Hurt);
            }
        }

        public HealthSystem GetHealthSystem() => context.Health;

        public Vector3 Position => context.Transform.position;
    }
}
