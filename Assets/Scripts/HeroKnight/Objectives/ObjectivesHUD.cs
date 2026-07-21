using System.Collections;
using HeroKnightSandbox.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        [SerializeField] private Image enemiesCheckbox;
        [SerializeField] private Image goalCheckbox;
        [SerializeField] private Sprite checkboxDoneSprite;
        [SerializeField] private Sprite checkboxEmptySprite;
        [SerializeField] private GameObject completePanel;
        [SerializeField] private AudioClip completionSound;
        [SerializeField] private GameObject confettiPrefab;
        [SerializeField] private Transform player;

        private bool completeShown;
        private bool enemiesDoneShown;
        private bool goalDoneShown;

        private void Update()
        {
            ObjectivesController objectives = ObjectivesController.Instance;
            if (objectives == null)
            {
                return;
            }

            bool enemiesDone = objectives.EnemiesRemaining <= 0;
            enemiesLine.text = $"Defeat enemies: {objectives.TotalEnemies - objectives.EnemiesRemaining}/{objectives.TotalEnemies}";
            goalLine.text = "Reach the end";

            SetCheckbox(enemiesCheckbox, enemiesDone, ref enemiesDoneShown);
            SetCheckbox(goalCheckbox, objectives.GoalReached, ref goalDoneShown);

            if (!completeShown && objectives.IsComplete)
            {
                completeShown = true;
                completePanel.GetComponent<FadePanel>().SetVisible(true);
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

        // Swaps the checkbox sprite and, the moment it first flips to done, briefly pops
        // it larger so completing an objective actually reads as an event rather than a
        // silent icon swap.
        private void SetCheckbox(Image checkbox, bool done, ref bool doneShown)
        {
            checkbox.sprite = done ? checkboxDoneSprite : checkboxEmptySprite;
            if (done && !doneShown)
            {
                doneShown = true;
                StartCoroutine(Pulse(checkbox.transform));
            }
        }

        private static IEnumerator Pulse(Transform target)
        {
            const float duration = 0.25f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float scale = Mathf.Lerp(1.6f, 1f, t / duration);
                target.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }
            target.localScale = Vector3.one;
        }
    }
}
