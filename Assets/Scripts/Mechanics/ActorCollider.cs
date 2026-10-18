using UnityEngine;

namespace Mechanics
{
    public sealed class ActorCollider : MonoBehaviour
    {
        public enum Flags
        {
            Hitbox = 1 << 0,
            Hurtbox = 1 << 1,
            WeakPoint = 1 << 2,
            HitboxDetailed = 1 << 3,
            Special = 1 << 4
        }

        public Flags SelfType;
        public AbstractGameActor AttachedActor;
        public Collider2D Collider;
    }
}
