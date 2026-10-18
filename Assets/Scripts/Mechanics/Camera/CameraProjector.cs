using UnityEngine;

namespace Mechanics
{
    public sealed class CameraProjector : MonoBehaviour, IInteractable
    {
        private bool _gettingDestroyed;
        private GameObject _target;

        public Vector2 Position => transform.position;

        private void Start()
        {
            if (PlayerEvents.Exists)
            {
                PlayerEvents.Instance.OnDeath += Death;
            }
        }

        private void Death(Health.DamageArgs obj)
        {
            if (_gettingDestroyed) return;
            _gettingDestroyed = true;
            Destroy(_target);
            Destroy(gameObject);
        }

        public void Interact(AbstractGameActor target)
        {
            if (_gettingDestroyed) return;
            if (target is Player player)
            {
                player.AddProjector();
            }
            _gettingDestroyed = true;
            Destroy(_target);
            Destroy(gameObject);
        }

        public void Project(Vector2 worldPos, GameCamera cam)
        {
            _target = cam.Project(worldPos);
        }

        private void OnDestroy()
        {
            Destroy(_target);
        }
    }
}
