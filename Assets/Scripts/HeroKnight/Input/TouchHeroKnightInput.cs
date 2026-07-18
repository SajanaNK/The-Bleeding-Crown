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
        public bool JumpPressed => jumpButton != null && jumpButton.WasPressedThisFrame;
        public bool AttackPressed => attackButton != null && attackButton.WasPressedThisFrame;
        public bool BlockHeld => blockButton != null && blockButton.IsHeld;
        public bool RollPressed => rollButton != null && rollButton.WasPressedThisFrame;
    }
}
