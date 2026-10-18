using UnityEngine;

namespace Effects
{
    public interface IReadOnlyBodyPoint
    {
        Vector2 Position { get; }
        Vector2 PrevPosition { get; }
        Vector2 Velocity { get; }

        float Mass { get; }
        float Radius { get; }
        int Index { get; }

        bool SimulatesCollisions { get; }
        RaycastHit2D? LastCollision { get; }
    }
}
