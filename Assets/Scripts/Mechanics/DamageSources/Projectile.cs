using System;
using System.Collections.Generic;
using UnityEngine;
using Utils;
using Health;

namespace Mechanics
{
    public class Projectile : MonoBehaviour
    {
        public class ProjectileDestroyData
        {
            public bool HitAnything;
            public float RemainingLifeTime;
            public RaycastHit2D Target;
            public Health.IDamagable Damagable;
            public ActorCollider ActorCollider;
            public Projectile Projectile;
        }

        [SerializeField] private float _baseSpeed;
        [SerializeField] private bool _findDetailedCollision;
        [SerializeField] private bool _projectileHasSize;
        [SerializeField] private float _projectileSize;
        [SerializeField] private float _destroyTime;
        [SerializeField] private float _maxLifeTime;
        [SerializeField] private LayerMask _hitMask;

        public int AssignedTeam; //used for ui highlighting
        public float SpeedMod = 1;
        public float SizeMod = 1;
        public float DamageMod = 1;
        public Vector2 Direction;
        public HashSet<Collider2D> IgnoredColliders = new HashSet<Collider2D>();

        public event Action<ProjectileDestroyData> OnDestroyBegin;
        public event Action<Projectile> OnDestroyEnd;

        private bool _markedForDestroy;
        private float _elapsed;
        private Vector2 _prevPosition;

        private void Awake()
        {
            _prevPosition = transform.position;
        }

        private void Update()
        {
            _prevPosition = transform.position;
        }

        private void LateUpdate()
        {
            if (_markedForDestroy)
            {
                _elapsed += Time.deltaTime;

                if(_elapsed > _destroyTime && (_elapsed - Time.deltaTime) < _destroyTime)
                {
                    OnDestroyEnd?.Invoke(this);
                }
                return;
            }

            _elapsed += Time.deltaTime;

            if(_elapsed > _maxLifeTime && (_elapsed - Time.deltaTime) < _maxLifeTime)
            {
                OnDestroyBegin?.Invoke(new ProjectileDestroyData()
                {
                    ActorCollider = null,
                    HitAnything = false,
                    Target = default,
                    Damagable = null,
                    RemainingLifeTime = 0,
                    Projectile = this
                });
                _markedForDestroy = true;
                _elapsed = 0f;
            }

            transform.position += (Vector3)Direction * (_baseSpeed * SpeedMod * Time.deltaTime);
            transform.localScale = Vector3.one * SizeMod * _projectileSize;
            Vector2 dir = _prevPosition.DirectionNormalized(transform.position, out float dist);
            RaycastHit2D trace = Trace(_prevPosition, dir, dist,
                    t => !IgnoredColliders.Contains(t.collider) &&
                    (!t.transform.TryGetComponent<ActorCollider>(out var comp) || (comp.SelfType & ActorCollider.Flags.Hitbox) != 0));

            if (trace)
            {
                Debug.Log("HIT TARGET - " + trace.collider + " + " + trace.transform.gameObject);
                ActorCollider cct = trace.transform.GetComponent<ActorCollider>();
                if (_findDetailedCollision 
                    && cct != null
                    && TryFindDetailedCollision(trace, cct, dir, out trace, out cct))
                {
                }

                OnDestroyBegin?.Invoke(new ProjectileDestroyData()
                {
                    ActorCollider = cct,
                    HitAnything = true,
                    Target = trace,
                    Damagable = trace.transform.GetComponent<Health.IDamagable>(),
                    RemainingLifeTime = _maxLifeTime - _elapsed,
                    Projectile = this
                });
                _markedForDestroy = true;
                _elapsed = 0f;
            }
        }

        private bool TryFindDetailedCollision(in RaycastHit2D originalHit, ActorCollider creatureCollider, in Vector2 dir, out RaycastHit2D newHit, out ActorCollider newColiider)
        {
            //check if its already detailed hitbox
            if((creatureCollider.SelfType & ActorCollider.Flags.HitboxDetailed) != 0)
            {
                newHit = originalHit;
                newColiider = creatureCollider;
                return true;
            }

            const float MAX_DISTANCE = 1f;
            //try to find in original direction first
            var newTrace = Trace(originalHit.point, dir, MAX_DISTANCE, t =>
                (t.transform.TryGetComponent<ActorCollider>(out var comp)
                && (comp.SelfType & ActorCollider.Flags.HitboxDetailed) != 0)
                && (comp.AttachedActor == creatureCollider.AttachedActor));
            if (newTrace)
            {
                newHit = newTrace;
                newColiider = newHit.transform.GetComponent<ActorCollider>();
                return true;
            }

            //try to find in opposite direction
            newTrace = Trace(originalHit.point, -dir, MAX_DISTANCE, t =>
                (t.transform.TryGetComponent<ActorCollider>(out var comp)
                && (comp.SelfType & ActorCollider.Flags.HitboxDetailed) != 0)
                && (comp.AttachedActor == creatureCollider.AttachedActor));
            if (newTrace)
            {
                newHit = newTrace;
                newColiider = newHit.transform.GetComponent<ActorCollider>();
                return true;
            }

            //nothing was found|use undetailed collider
            newHit = originalHit;
            newColiider = creatureCollider;
            return false;
        }

        private RaycastHit2D Trace(Vector2 start, Vector2 dir, float dist, Func<RaycastHit2D, bool> filter)
        {
            RaycastHit2D trace;
            if (_projectileHasSize)
            {
                float size = _projectileSize * SizeMod;
                trace = MiscUtils.CircleCastWithFilter(_prevPosition, size, dir, dist, _hitMask, filter);
            }
            else
            {
                trace = MiscUtils.RaycastWithFilter(_prevPosition, dir, dist, _hitMask, filter);
            }

            return trace;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireSphere(transform.position, _projectileSize);
        }

        public float BaseSpeed => _baseSpeed;
        public bool IsGettingDestroyed => _markedForDestroy;
    }
}
