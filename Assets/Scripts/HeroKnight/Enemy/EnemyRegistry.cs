using System.Collections.Generic;

namespace HeroKnightSandbox.Enemy
{
    public static class EnemyRegistry
    {
        private static readonly List<EnemyController> enemies = new List<EnemyController>();

        public static void Register(EnemyController enemy) => enemies.Add(enemy);
        public static void Unregister(EnemyController enemy) => enemies.Remove(enemy);
        public static IReadOnlyList<EnemyController> All => enemies;
    }
}
