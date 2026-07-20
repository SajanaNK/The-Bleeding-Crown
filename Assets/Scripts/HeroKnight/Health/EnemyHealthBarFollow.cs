using UnityEngine;

namespace HeroKnightSandbox.Health
{
    /// <summary>
    /// Keeps an instantiated health bar positioned above an enemy's head by
    /// following its world position directly, rather than being parented to
    /// it - enemies flip facing by negating their root Transform's
    /// localScale.x (see PatrolState.Tick()), which would visually mirror a
    /// parented bar's fill direction every time the enemy turns around.
    /// Destroys itself once its target is gone (enemy died and was
    /// destroyed).
    /// </summary>
    public class EnemyHealthBarFollow : MonoBehaviour
    {
        public Transform Target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 1.2f, 0f);

        private void Awake()
        {
            // pfHealthBarUIWorldCanvas.prefab's RectTransform is 40x6 units at
            // scale 1 (sized for the CodeMonkey demo's larger scene) - shrunk
            // here to read as a small bar above this project's ~1-unit-tall
            // enemy sprites.
            transform.localScale = Vector3.one * 0.025f;
        }

        private void LateUpdate()
        {
            if (Target == null)
            {
                Destroy(gameObject);
                return;
            }

            transform.position = Target.position + offset;
        }
    }
}
