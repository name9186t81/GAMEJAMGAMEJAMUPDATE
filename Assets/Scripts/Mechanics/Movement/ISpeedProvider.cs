using UnityEngine;

namespace Movement
{
    public interface ISpeedProvider
    {
        float BaseSpeed { get; }
        Vector2 CurrentSpeed { get; }
    }
}
