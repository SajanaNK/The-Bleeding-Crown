using UnityEngine;

namespace HeroKnightSandbox.Sensors
{
    // Verbatim namespaced copy of the vendor demo's Sensor_HeroKnight — field naming
    // (m_ prefixes) intentionally matches the original rather than this project's style.
    public class Sensor_HeroKnight : MonoBehaviour
    {
        private int m_ColCount = 0;
        private float m_DisableTimer;

        private void OnEnable()
        {
            m_ColCount = 0;
        }

        public bool State()
        {
            if (m_DisableTimer > 0)
                return false;
            return m_ColCount > 0;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Ground/wall/ledge sensors must only react to solid level geometry.
            // Skipping other triggers matters concretely in this project: the
            // Cinemachine camera confiner's bounding shape is itself a trigger
            // collider spanning nearly the whole level, and since the character's
            // own Rigidbody2D satisfies Physics2D's "one side needs a Rigidbody2D"
            // requirement for trigger events, every sensor would otherwise register
            // a permanent, never-exiting overlap with it the moment it spawns.
            if (other.isTrigger)
            {
                return;
            }

            m_ColCount++;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.isTrigger)
            {
                return;
            }

            m_ColCount--;
        }

        private void Update()
        {
            m_DisableTimer -= Time.deltaTime;
        }

        public void Disable(float duration)
        {
            m_DisableTimer = duration;
        }
    }
}
