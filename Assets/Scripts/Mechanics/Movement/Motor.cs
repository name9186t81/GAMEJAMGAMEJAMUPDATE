using UnityEngine;

namespace Movement
{
    public sealed class Motor
    {
        public Vector2 InstantVelocity;
        public Vector2 Velocity;
        public float Damping = 0.95f;
    }
}
