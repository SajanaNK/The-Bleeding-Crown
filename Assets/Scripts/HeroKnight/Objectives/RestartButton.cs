using HeroKnightSandbox.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HeroKnightSandbox.Objectives
{
    /// <summary>
    /// Reloads the current scene when clicked, resetting player, enemies, and
    /// objectives to their exact starting state. Wires its own listener at
    /// runtime rather than relying on an editor-time persistent listener.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class RestartButton : MonoBehaviour
    {
        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(Restart);
        }

        private void Restart()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            if (ScreenTransition.Instance != null)
            {
                ScreenTransition.Instance.LoadScene(sceneName);
                return;
            }

            // Fallback for a scene that hasn't been rebuilt since ScreenTransition was
            // added, so navigation never silently breaks.
            // Time.timeScale persists across scene loads, so if this is clicked from the
            // Pause menu (timeScale 0) the reloaded scene would start out frozen.
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
        }
    }
}
