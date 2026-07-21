using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HeroKnightSandbox.UI
{
    /// <summary>
    /// Loads a named scene when clicked. Reusable for the Start Screen's Play
    /// button and the in-game Pause menu's "Quit to Start Screen" button.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class LoadSceneButton : MonoBehaviour
    {
        [SerializeField] private string sceneName;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(Load);
        }

        private void Load()
        {
            if (ScreenTransition.Instance != null)
            {
                ScreenTransition.Instance.LoadScene(sceneName);
                return;
            }

            // Fallback for a scene that hasn't been rebuilt since ScreenTransition was
            // added, so navigation never silently breaks.
            // Time.timeScale persists across scene loads, so if this is clicked from a
            // paused state (timeScale 0) the loaded scene would start out frozen.
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
        }
    }
}
