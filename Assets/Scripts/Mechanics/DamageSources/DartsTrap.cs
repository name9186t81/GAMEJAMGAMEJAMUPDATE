using Audio;
using Health;
using System;
using UnityEngine;
using UnityEngine.Events;
using Utils;

namespace Mechanics
{
    public class DartsTrap : MonoBehaviour
    {
        [SerializeField] private LayerMask _fireMask;
        [SerializeField] private UnityEvent _onFire;
        [SerializeField] private AudioClip _shootSound;
        
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

            Debug.Log(projection1 + " " + projection2 + " " + (projection1 - projection2) + " " + (projection1 - projection2).sqrMagnitude); 
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
                }

                AudioController.Instance.PlayAudio(_shootSound, transform.position, 1f, 1f);
                OnFire?.Invoke(ray);
                _onFire?.Invoke();
            }
        }
    }
}
