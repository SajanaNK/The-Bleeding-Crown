using CodeMonkey.HealthSystemCM;
using HeroKnightSandbox.Input;
using HeroKnightSandbox.Sensors;
using UnityEngine;

namespace HeroKnightSandbox
{
    /// <summary>
    /// Mutable data shared across all player states. Owned and populated by
    /// HeroKnightController; states read/write it but never own it.
    /// </summary>
    public class HeroKnightContext
    {
        public Rigidbody2D Body;
        public Animator Animator;
        public SpriteRenderer SpriteRenderer;
        public Transform Transform;
        public IHeroKnightInput Controls;
        public AudioSource AudioSource;
        public AudioClip[] AttackClips;
        public AudioClip[] BlockClips;
        public AudioClip[] JumpClips;
        public AudioClip[] FootstepClips;

        // Fully qualified: see the matching comment in HeroKnightController.cs — the
        // vendor's global-scope Sensor_HeroKnight class shadows the `using
        // HeroKnightSandbox.Sensors;` import above for any bare reference here.
        public HeroKnightSandbox.Sensors.Sensor_HeroKnight GroundSensor;
        public HeroKnightSandbox.Sensors.Sensor_HeroKnight WallSensorR1;
        public HeroKnightSandbox.Sensors.Sensor_HeroKnight WallSensorR2;
        public HeroKnightSandbox.Sensors.Sensor_HeroKnight WallSensorL1;
        public HeroKnightSandbox.Sensors.Sensor_HeroKnight WallSensorL2;
        public HeroKnightSandbox.Sensors.Sensor_HeroKnight LedgeSensorR;
        public HeroKnightSandbox.Sensors.Sensor_HeroKnight LedgeSensorL;

        public float MoveSpeed = 4.0f;
        public float JumpForce = 7.5f;
        public Vector2 LedgeClimbOffset = new Vector2(0.3f, 1.6f);
        public float LedgeHangOffset = 0.3f;
        public float RollForce = 6.0f;
        public float RollDuration = 8.0f / 14.0f;
        public float TimeSinceAttack = 0f;
        public int ComboCount = 0;
        public float AttackComboWindow = 0.25f;
        public float AttackComboResetWindow = 1.0f;
        public int FacingDirection = 1;

        public HealthSystem Health;
        public int AttackDamage = 1;
        public float AttackHitRadius = 1.0f;
        public float InvulnerabilityDuration = 0.5f;
        public float HurtDuration = 0.3f;
        public float InvulnerabilityTimer = 0f;
        public float DeathDuration = 1.0f;
        public float FootstepInterval = 0.35f;

        public bool IsInvulnerable => InvulnerabilityTimer > 0f;

        public bool IsGrounded => GroundSensor.State();

        public bool IsWallSliding =>
            (WallSensorR1.State() && WallSensorR2.State()) ||
            (WallSensorL1.State() && WallSensorL2.State());

        public void UpdateFacing()
        {
            if (Controls.MoveX > 0)
            {
                FacingDirection = 1;
                SpriteRenderer.flipX = false;
            }
            else if (Controls.MoveX < 0)
            {
                FacingDirection = -1;
                SpriteRenderer.flipX = true;
            }
        }

        public void SetVelocityX(float x)
        {
            Body.velocity = new Vector2(x, Body.velocity.y);
        }
    }
}
