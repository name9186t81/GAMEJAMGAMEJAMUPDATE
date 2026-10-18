using System;
using System.Collections.Generic;
using UnityEngine;

namespace Effects
{
    public sealed class BodyPoint : IReadOnlyBodyPoint
    {
        public Vector2? ForcedPosition;
        public Vector2 Position;
        public Vector2 PrevPosition;
        public Vector2 LastSafePosition;
        public Vector2 Velocity;
        public Vector2 InstantVelocity;
        public float Mass;
        public float Bounciness = 0f; // DO NOT SET TO NEGATIVES
        public float Radius;
        public int Index;

        public bool DetailedCollisions = true;
        public float LastCollisionTime = 999f;
        public RaycastHit2D? LastCollision;
        public RaycastHit2D[] LastCollisions;
        public Collider2D OwnCollider;
        public HashSet<Collider2D> IgnoredColliders;

        private static RaycastHit2D[] _cached = new RaycastHit2D[32];
        private static ContactFilter2D _filter = new ContactFilter2D() { layerMask = ~(1 << 30 | 1 << 6), useDepth = false, useTriggers = false };

        public BodyPoint(float mass, float size)
        {
            Mass = mass;
            Radius = size;
        }

        public void Update(float tickRate = PointsUtils.TICK_RATE)
        {
            DoCollisions(tickRate);
        }

        public void HardReset(Vector2 pos)
        {
            Position = PrevPosition = pos;
            Velocity = Vector2.zero;
        }

        private void DoCollisions(float tickRate)
        {
            var p = this;
            if (p.Position.x > 100_000_000 || p.Position.y > 100_000_000)
            {
                Debug.LogError("OBJECT POSITION OVERFLOW - BREAKING COLLISION DETECTION");
                return;
            }
            Vector2 dir = p.Position - p.PrevPosition;
            float length = dir.magnitude;

            if (length <= Mathf.Epsilon) return;

            dir /= length;

            int overlap = Physics2D.CircleCast(p.PrevPosition, Radius, dir, _filter, _cached, length);
            if (TryFindClosestCollision(overlap, out var hit))
            {
                Vector2 pos = Position;
                Position = PrevPosition = hit.point + hit.normal * (Radius);
                Vector2 remaining = pos - Position;
                Vector2 slide = remaining - Vector2.Dot(remaining, hit.normal) * hit.normal;
                Position += slide;
                PrevPosition += slide;

                float reflect = Vector2.Dot(Velocity, hit.normal);

                if (reflect < 0)
                {
                    Velocity = Velocity - (1 + Bounciness) * reflect * hit.normal;
                }

                LastCollision = hit;
                LastCollisionTime = 0;
                //Debug.DrawLine(p.Position, p.Position + hit.normal, Color.cyan, tickRate);
            }
            else
            {
                LastSafePosition = Position;
                LastCollisionTime += tickRate;
            }
        }

        private bool TryFindClosestCollision(int length, out RaycastHit2D hit)
        {
            List<RaycastHit2D> collisions = null;
            if (DetailedCollisions)
            {
                collisions = new List<RaycastHit2D>();
            }

            hit = _cached[0];
            float closest = 9999f;
            for (int i = 0; i < length; ++i)
            {
                if (_cached[i].collider != OwnCollider && (IgnoredColliders == null || !IgnoredColliders.Contains(_cached[i].collider)))
                {
                    if (DetailedCollisions)
                    {
                        collisions.Add(_cached[i]);
                    }
                    float dist = (_cached[i].point - Position).sqrMagnitude;
                    if(dist < closest)
                    {
                        closest = dist;
                        hit = _cached[i];
                    }
                }
            }

            if (DetailedCollisions)
            {
                LastCollisions = collisions.ToArray();
            }
            return closest < 9999f;
        }

        public bool HasCollision => LastCollision.HasValue;

        Vector2 IReadOnlyBodyPoint.Position => Position;

        Vector2 IReadOnlyBodyPoint.PrevPosition => PrevPosition;

        Vector2 IReadOnlyBodyPoint.Velocity => Velocity;

        float IReadOnlyBodyPoint.Mass => Mass;

        float IReadOnlyBodyPoint.Radius => Radius;

        int IReadOnlyBodyPoint.Index => Index;

        bool IReadOnlyBodyPoint.SimulatesCollisions => true;

        RaycastHit2D? IReadOnlyBodyPoint.LastCollision => LastCollision;
    }
}