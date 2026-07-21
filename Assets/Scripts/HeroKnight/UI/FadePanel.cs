using System.Collections;
using UnityEngine;

namespace HeroKnightSandbox.UI
{
    /// <summary>
    /// Fades a CanvasGroup in/out instead of the panel's GameObject just popping
    /// active/inactive - used for the Pause panel's show/hide.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class FadePanel : MonoBehaviour
    {
        [SerializeField] private float fadeDuration = 0.15f;

        private CanvasGroup canvasGroup;
        private Coroutine activeFade;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        public void SetVisible(bool visible)
        {
            if (activeFade != null)
            {
                StopCoroutine(activeFade);
            }

            if (visible)
            {
                gameObject.SetActive(true);
            }
            activeFade = StartCoroutine(Fade(visible));
        }

        private IEnumerator Fade(bool visible)
        {
            float from = canvasGroup.alpha;
            float to = visible ? 1f : 0f;
            float t = 0f;
            while (t < fadeDuration)
            {
                // Unscaled - Time.timeScale is 0 while paused, which is exactly when
                // this panel needs to animate.
                t += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(from, to, t / fadeDuration);
                yield return null;
            }
            canvasGroup.alpha = to;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
            if (!visible)
            {
                gameObject.SetActive(false);
            }
            activeFade = null;
        }
    }
}
