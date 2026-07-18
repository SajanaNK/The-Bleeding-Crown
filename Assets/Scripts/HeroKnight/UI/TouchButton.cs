using UnityEngine;
using UnityEngine.EventSystems;

namespace HeroKnightSandbox.UI
{
    /// <summary>
    /// One-shot press (WasPressedThisFrame) and press-and-hold (IsHeld) in a single
    /// component, so the same class backs Jump/Attack/Roll (one-shot) and Block (hold).
    /// </summary>
    public class TouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public bool IsHeld { get; private set; }
        public bool WasPressedThisFrame { get; private set; }

        public void OnPointerDown(PointerEventData eventData)
        {
            IsHeld = true;
            WasPressedThisFrame = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            IsHeld = false;
        }

        private void LateUpdate()
        {
            // Cleared after all Update()s this frame have had a chance to observe it.
            WasPressedThisFrame = false;
        }
    }
}
