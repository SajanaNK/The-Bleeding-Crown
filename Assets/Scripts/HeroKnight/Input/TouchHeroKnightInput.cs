using HeroKnightSandbox.UI;
using UnityEngine;

namespace HeroKnightSandbox.Input
{
    public class TouchHeroKnightInput : MonoBehaviour, IHeroKnightInput
    {
        [SerializeField] private VirtualJoystick joystick;
        [SerializeField] private TouchButton jumpButton;
        [SerializeField] private TouchButton attackButton;
        [SerializeField] private TouchButton blockButton;
        [SerializeField] private TouchButton rollButton;

        public float MoveX => joystick != null ? joystick.Direction.x : 0f;

        // PC TESTING ONLY - keyboard fallback for the 4 action buttons (mouse-clicking
        // small touch buttons repeatedly during testing is painful without a touchscreen).
        // Remove this block (and the four `||` clauses below) before shipping.
        public bool JumpPressed => (jumpButton != null && jumpButton.WasPressedThisFrame) || UnityEngine.Input.GetKeyDown(KeyCode.Space);
        public bool AttackPressed => (attackButton != null && attackButton.WasPressedThisFrame) || UnityEngine.Input.GetKeyDown(KeyCode.J);
        public bool BlockHeld => (blockButton != null && blockButton.IsHeld) || UnityEngine.Input.GetKey(KeyCode.K);
        public bool RollPressed => (rollButton != null && rollButton.WasPressedThisFrame) || UnityEngine.Input.GetKeyDown(KeyCode.L);
    }
}
