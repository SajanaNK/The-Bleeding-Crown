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
        [SerializeField] private AudioClip completionSound;
        [SerializeField] private GameObject confettiPrefab;
        [SerializeField] private Transform player;

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
                AudioSource.PlayClipAtPoint(completionSound, player.position);
                // ConfettiCelebration.prefab's particle systems have playOnAwake off (this
                // asset pack's convention - see EmitParticlesOnLand.cs elsewhere in the
                // project), so instantiating alone doesn't start it; Play() must be called
                // explicitly. withChildren defaults true, so this also starts the
                // "RainbowLines" child system.
                GameObject confetti = Instantiate(confettiPrefab, player.position, Quaternion.identity);
                confetti.GetComponent<ParticleSystem>().Play();
            }
        }
    }
}
