using Health;
using System;
using UnityEngine;
using Utils;

namespace Mechanics
{
    public class DartsTrap : MonoBehaviour
    {
        [SerializeField] private LayerMask _fireMask;

        public event Action<RaycastHit2D> OnFire;

        private void Start()
        {
            if (PlayerEvents.Exists)
            {
                PlayerEvents.Instance.OnPictureTaken += TryToShoot;
            }
        }

        private void TryToShoot(Vector2 obj)
        {
            Vector2 playerPos = PlayerEvents.Instance.Player.transform.position;

            Vector2 projection1 = transform.up * playerPos;
            Vector2 projection2 = transform.up * new Vector2(transform.position.x, transform.position.y);

            if ((projection1 - projection2).sqrMagnitude < 1f)
            {
                var ray = Physics2D.Raycast(transform.position, transform.right, 9999999999, _fireMask);

                if (ray)
                {
                    if(ray.transform.TryGetComponent<AbstractGameActor>(out var damagable))
                    {
                        damagable.TakeDamage(new DamageArgs()
                        {
                            Damage = 1
                        });
                    }

                    OnFire?.Invoke(ray);
                }
            }
        }
    }
}
