using UnityEngine;

namespace HeroKnightSandbox.Objectives
{
    /// <summary>
    /// Fires ObjectivesController.MarkGoalReached() once the player enters this
    /// trigger, then disables itself. Placed just past the ledge climb so it's
    /// only reachable after traversing the wall.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class GoalZoneTrigger : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            // No GameObject tags are used anywhere in this project - identify the
            // player by component instead of CompareTag.
            HeroKnightController player = other.GetComponentInParent<HeroKnightController>();
            if (player == null)
            {
                return;
            }

            ObjectivesController.Instance.MarkGoalReached();
            gameObject.SetActive(false);
        }
    }
}
