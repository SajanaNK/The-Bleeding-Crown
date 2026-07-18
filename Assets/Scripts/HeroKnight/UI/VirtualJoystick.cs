using UnityEngine;
using UnityEngine.EventSystems;

namespace HeroKnightSandbox.UI
{
    /// <summary>
    /// Drag-based on-screen joystick. Drag anywhere on `background`; the `handle`
    /// follows, clamped to `handleRange`, and Direction reports the clamped
    /// offset normalized to [-1, 1] on each axis.
    /// </summary>
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform background;
        [SerializeField] private RectTransform handle;
        [SerializeField] private float handleRange = 100f;

        public Vector2 Direction { get; private set; } = Vector2.zero;

        public void OnPointerDown(PointerEventData eventData)
        {
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                background, eventData.position, eventData.pressEventCamera, out localPoint))
            {
                Vector2 clamped = Vector2.ClampMagnitude(localPoint, handleRange);
                handle.anchoredPosition = clamped;
                Direction = clamped / handleRange;
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (handle != null)
            {
                handle.anchoredPosition = Vector2.zero;
            }
            Direction = Vector2.zero;
        }
    }
}
