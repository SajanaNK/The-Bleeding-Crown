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
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
