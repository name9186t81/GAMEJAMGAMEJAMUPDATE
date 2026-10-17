using UnityEngine;

namespace Movement
{
    public interface IDirectionsProvider
    {
        Vector2 LookDirection { get; }
        Vector2 MovementDirection { get; }
    }
}
