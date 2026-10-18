using UnityEngine;

namespace Effects
{
    public sealed class BodyConnection
    {
        public enum ConnectionType
        {
            Normal,
            Pull,
            Push
        }

        public BodyPoint Point1;
        public BodyPoint Point2;
        public float MaxDistance;
        public float Symmetry;
        public ConnectionType Type;

        public BodyConnection(BodyPoint point1, BodyPoint point2, float maxDistance, float symmetry, ConnectionType connectionType)
        {
            Point1 = point1;
            Point2 = point2;
            MaxDistance = maxDistance;
            Type = connectionType;

            if(symmetry > 0)
            {
                Symmetry = symmetry;
            }
            else
            {
                Symmetry = Point1.Mass / (Point1.Mass + Point2.Mass);
            }
        }

        public void Update()
        {
            Vector2 pos1 = Point1.Position;
            Vector2 pos2 = Point2.Position;

            float dist = (pos2 - pos1).sqrMagnitude;
            if(dist < Mathf.Epsilon)
            {
                return;
            }

            if( Type == ConnectionType.Normal ||
                Type == ConnectionType.Push && dist < MaxDistance * MaxDistance ||
                Type == ConnectionType.Pull && dist > MaxDistance * MaxDistance)
            {
                dist = Mathf.Sqrt(dist);
                Vector2 direction = (pos2 - pos1) / dist;

                Point1.Velocity += direction * ((1 - Symmetry) * (dist - MaxDistance));
                Point1.Position += direction * ((1 - Symmetry) * (dist - MaxDistance));
                Point2.Velocity -= direction * (Symmetry * (dist - MaxDistance));
                Point2.Position -= direction * (Symmetry * (dist - MaxDistance));
            }
        }
    }
}