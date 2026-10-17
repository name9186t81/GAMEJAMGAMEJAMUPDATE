using UnityEngine;

namespace Movement
{
    public interface IVelocityProvider
    {
        Vector2 Velocity { get; set; }
    }
}
