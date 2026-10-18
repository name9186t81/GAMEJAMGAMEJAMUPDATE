using UnityEngine;

namespace Mechanics
{
    public interface IInteractable
    {
        void Interact(AbstractGameActor target);
        Vector2 Position { get; }
    }
}
