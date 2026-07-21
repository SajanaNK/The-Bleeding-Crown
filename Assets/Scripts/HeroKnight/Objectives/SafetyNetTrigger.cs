using HeroKnightSandbox.UI;
using UnityEngine;

namespace HeroKnightSandbox.Objectives
{
    /// <summary>
    /// Shows the Respawn prompt (see RespawnPromptUI) while the player is standing
    /// on the SafetyNet, so a missed jump into the pit below the level always has a
    /// way out instead of a soft-lock. No damage/penalty - SafetyNet exists purely
    /// to catch falls.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class SafetyNetTrigger : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            // No GameObject tags are used anywhere in this project - identify the
            // player by component instead of CompareTag.
            HeroKnightController player = other.GetComponentInParent<HeroKnightController>();
            if (player == null || RespawnPromptUI.Instance == null)
            {
                return;
            }

            RespawnPromptUI.Instance.Show(player);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            HeroKnightController player = other.GetComponentInParent<HeroKnightController>();
            if (player == null || RespawnPromptUI.Instance == null)
            {
                return;
            }

            RespawnPromptUI.Instance.Hide();
        }
    }
}
