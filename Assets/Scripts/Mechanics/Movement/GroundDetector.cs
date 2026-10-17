using System;
using UnityEngine;

namespace Movement
{
    public abstract class GroundDetector : MonoBehaviour
    {
        public enum CollisionDetails
        {
            Ground = 1 << 0,
            TouchingWall = 1 << 1,
            TouchingCeiling = 1 << 2
        }

        public struct CollisionInfo
        {
            public Vector2 Point;
            public Vector2 Normal;
        }

        public abstract CollisionDetails Details { get; }
        public abstract bool IsGrounded { get; }
        public abstract float TimeSinceGrounded { get; }

        public Vector2 GravityDirection = Vector2.down;

        public event Action<CollisionDetails> OnCollision;
        public event Action<CollisionDetails> OnCollisionLoss;

        public event Action OnLanding;
        public event Action OnLeavingGround;

        public abstract CollisionInfo GetCollisionInfo(CollisionDetails type);

        protected void InvokeLanding() => OnLanding?.Invoke();
        protected void InvokeLeavingGround() => OnLeavingGround?.Invoke();

        protected void InvokeCollision(int id) => OnCollision?.Invoke((CollisionDetails)(1 << id));
        protected void InvokeCollisionLoss(int id) => OnCollisionLoss?.Invoke((CollisionDetails)(1 << id));
    }
}
