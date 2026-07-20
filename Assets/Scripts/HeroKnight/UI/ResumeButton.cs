using UnityEngine;
using UnityEngine.UI;

namespace HeroKnightSandbox.UI
{
    /// <summary>
    /// Resumes the game when clicked, via PauseController.Instance.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ResumeButton : MonoBehaviour
    {
        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(() => PauseController.Instance.Resume());
        }
    }
}
