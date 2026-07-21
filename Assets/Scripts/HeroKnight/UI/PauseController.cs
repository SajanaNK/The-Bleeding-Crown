using UnityEngine;

namespace HeroKnightSandbox.UI
{
    /// <summary>
    /// Toggles Time.timeScale and the pause panel from the touch Pause button.
    /// Manual static singleton, matching ObjectivesController's own pattern,
    /// so ResumeButton can reach it without a direct scene reference.
    /// </summary>
    public class PauseController : MonoBehaviour
    {
        public static PauseController Instance { get; private set; }

        [SerializeField] private TouchButton pauseButton;
        [SerializeField] private GameObject pausePanel;

        private bool isPaused;

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

        private void Update()
        {
            if (pauseButton.WasPressedThisFrame)
            {
                SetPaused(!isPaused);
            }
        }

        public void Resume()
        {
            SetPaused(false);
        }

        private void SetPaused(bool paused)
        {
            isPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            pausePanel.GetComponent<FadePanel>().SetVisible(paused);
        }
    }
}
