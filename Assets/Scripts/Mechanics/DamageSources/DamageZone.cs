using Health;
using UnityEngine;

namespace Mechanics
{
    public sealed class DamageZone : MonoBehaviour
    {
        [SerializeField] private LayerMask _targetMask;
        [SerializeField] private Vector2 _size;
        private static Collider2D[] _cache = new Collider2D[32];

        private void FixedUpdate()
        {
            Vector2 center = transform.position;

            var overlap = Physics2D.OverlapArea(center - _size * 0.5f, center + _size * 0.5f, new ContactFilter2D() { layerMask = _targetMask }, _cache);

            for(int i = 0; i < overlap; i++)
            {
                if (_cache[i].TryGetComponent<IDamagable>(out var damagable))
                {
                    damagable.TakeDamage(new DamageArgs() { Damage = 1 });
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, _size);
        }
    }
}
