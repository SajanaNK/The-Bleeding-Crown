using TMPro;
using UnityEngine;

namespace HeroKnightSandbox.Objectives
{
    /// <summary>
    /// Top-left checklist for the two sandbox objectives, plus a centered
    /// "Sandbox Complete!" panel shown once when both are satisfied.
    /// </summary>
    public class ObjectivesHUD : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI enemiesLine;
        [SerializeField] private TextMeshProUGUI goalLine;
        [SerializeField] private GameObject completePanel;

        private bool completeShown;

        private void Update()
        {
            ObjectivesController objectives = ObjectivesController.Instance;
            if (objectives == null)
            {
                return;
            }

            bool enemiesDone = objectives.EnemiesRemaining <= 0;
            enemiesLine.text = $"Defeat enemies: {objectives.TotalEnemies - objectives.EnemiesRemaining}/{objectives.TotalEnemies}" +
                                (enemiesDone ? " [DONE]" : "");
            goalLine.text = "Reach the end: " + (objectives.GoalReached ? "[DONE]" : "[ ]");

            if (!completeShown && objectives.IsComplete)
            {
                completeShown = true;
                completePanel.SetActive(true);
            }
        }
    }
}
