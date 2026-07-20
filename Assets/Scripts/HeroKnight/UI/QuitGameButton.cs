using UnityEngine;
using UnityEngine.UI;

namespace HeroKnightSandbox.UI
{
    /// <summary>
    /// Quits the application when clicked. Start Screen only - a running mobile/touch
    /// build has no in-game equivalent (players use the OS home button instead).
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class QuitGameButton : MonoBehaviour
    {
        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(Application.Quit);
        }
    }
}
