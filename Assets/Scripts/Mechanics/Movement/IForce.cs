using System;
using UnityEngine;

namespace Movement
{
    public interface IForce
    {
        event Action RemoveCallback;

        void Init(ISpeedProvider prov1, IDirectionsProvider prov2);
        void Update(float delta);

        Vector2 GetInstantVelocity();
        Vector2 GetVelocity();
    }
}
