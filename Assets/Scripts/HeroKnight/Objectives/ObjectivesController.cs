using HeroKnightSandbox.Enemy;
using UnityEngine;

namespace HeroKnightSandbox.Objectives
{
    /// <summary>
    /// Tracks the sandbox's two win conditions: defeating every enemy present at
    /// Start() and reaching the goal zone past the ledge climb. Manual static
    /// singleton, matching GameController.Instance's own pattern.
    /// </summary>
    public class ObjectivesController : MonoBehaviour
    {
        public static ObjectivesController Instance { get; private set; }

        public int TotalEnemies { get; private set; }
        public int EnemiesRemaining { get; private set; }
        public bool GoalReached { get; private set; }
        public bool IsComplete => EnemiesRemaining <= 0 && GoalReached;

        private void OnEnable()
        {
            Instance = this;
        }

        private void OnDisable()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start()
        {
            TotalEnemies = EnemyRegistry.All.Count;
            EnemiesRemaining = TotalEnemies;
        }

        private void Update()
        {
            EnemiesRemaining = EnemyRegistry.All.Count;
        }

        public void MarkGoalReached()
        {
            GoalReached = true;
        }
    }
}
