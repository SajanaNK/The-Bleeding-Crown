using UnityEngine;
using UnityEngine.EventSystems;

namespace HeroKnightSandbox.UI
{
    /// <summary>
    /// Scales a button up slightly on hover and down on press, so buttons that were
    /// previously static (Unity's default Button.Transition does nothing unless
    /// targetGraphic is wired up, which none of this project's buttons do) get visible
    /// feedback. Works on both UI Button and TouchButton - only pointer events matter.
    /// Also fires an optional click sound (see clickClip) off the same press event.
    /// </summary>
    public class ButtonPunch : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private float hoverScale = 1.06f;
        [SerializeField] private float pressScale = 0.94f;
        [SerializeField] private float animSpeed = 12f;
        // Only set on the menu-style buttons KenneyButtonSkin builds - left null (and
        // silent) on the touch action/pause buttons, which already have their own distinct
        // gameplay sfx and would otherwise double up on every tap.
        [SerializeField] private AudioClip clickClip;

        private RectTransform rectTransform;
        private AudioSource audioSource;
        private float targetScale = 1f;
        private bool isHovered;

        private void Awake()
        {
            rectTransform = (RectTransform)transform;
            audioSource = GetComponent<AudioSource>();
        }

        private void Update()
        {
            if (rectTransform.localScale.x != targetScale)
            {
                float s = Mathf.MoveTowards(rectTransform.localScale.x, targetScale, animSpeed * Time.unscaledDeltaTime);
                rectTransform.localScale = new Vector3(s, s, 1f);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;
            targetScale = hoverScale;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
            targetScale = 1f;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            targetScale = pressScale;

            if (clickClip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clickClip);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            targetScale = isHovered ? hoverScale : 1f;
        }
    }
}
