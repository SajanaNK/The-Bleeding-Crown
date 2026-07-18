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

        public Sensor_HeroKnight GroundSensor;
        public Sensor_HeroKnight WallSensorR1;
        public Sensor_HeroKnight WallSensorR2;
        public Sensor_HeroKnight WallSensorL1;
        public Sensor_HeroKnight WallSensorL2;
        public Sensor_HeroKnight LedgeSensorR;
        public Sensor_HeroKnight LedgeSensorL;

        public float MoveSpeed = 4.0f;
        public float JumpForce = 7.5f;
        public Vector2 LedgeClimbOffset = new Vector2(0.3f, 1.1f);
        public int FacingDirection = 1;

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
