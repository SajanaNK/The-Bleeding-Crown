using UnityEngine;
using UnityEngine.UI;

namespace HeroKnightSandbox.UI
{
    /// <summary>
    /// Respawn button shown by SafetyNetTrigger while the player is standing on the
    /// SafetyNet. Hidden via CanvasGroup rather than SetActive(false) so Awake()
    /// still runs at scene load and Instance is always ready by the time a trigger
    /// fires. Wires its own listener at runtime, same convention as RestartButton.
    /// </summary>
    [RequireComponent(typeof(Button))]
    [RequireComponent(typeof(CanvasGroup))]
    public class RespawnPromptUI : MonoBehaviour
    {
        public static RespawnPromptUI Instance { get; private set; }

        private CanvasGroup canvasGroup;
        private HeroKnightController player;

        private void Awake()
        {
            Instance = this;
            canvasGroup = GetComponent<CanvasGroup>();
            GetComponent<Button>().onClick.AddListener(OnRespawnClicked);
            Hide();
        }

        private void OnDestroy()
        {
            // Without this, a scene reload/rebuild that destroys this instance leaves
            // Instance pointing at a dead object - Unity's overloaded null-check on a
            // destroyed MonoBehaviour reads true, so clearing it here lets
            // SafetyNetTrigger's `Instance == null` guard skip safely instead of
            // throwing MissingReferenceException on the next OnTriggerExit2D.
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Show(HeroKnightController targetPlayer)
        {
            player = targetPlayer;
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        public void Hide()
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            player = null;
        }

        private void OnRespawnClicked()
        {
            player?.Respawn();
            Hide();
        }
    }
}
