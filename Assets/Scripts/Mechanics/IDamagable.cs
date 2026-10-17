using Health;
using Movement;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mechanics
{
    public abstract class IDamagable : MonoBehaviour, IDirectionsProvider, ISpeedProvider, Health.IDamagable, IHealth
    {
        [SerializeField] private int _maxHealth;
        [SerializeField] private float _baseSpeed;

        protected int _currentHealth;
        private GroundDetector _groundDetector;

        protected Vector2 _lookDirection;
        protected Vector2 _movementDirection;

        public event Action<DamageArgs> OnDamage;
        public event Action<DamageArgs> OnDeath;

        private void Awake()
        {
            _groundDetector = gameObject.AddComponent<NormalsGroundedDetector>();
            _currentHealth = _maxHealth;
            Init();
        }

        protected virtual void Init() { }

        protected GroundDetector GroundDetector => _groundDetector;

        protected abstract Vector2 GetVelocity();
        protected abstract ActorCollider[] GetAllColliders();

        public List<ActorCollider> GetSpecificColliders(ActorCollider.Flags mask)
        {
            var cols = GetAllColliders();

            List<ActorCollider> res = new List<ActorCollider>();
            for(int i = 0; i < cols.Length; i++)
            {
                if ((cols[i].SelfType & mask) != 0)
                {
                    res.Add(cols[i]);
                }
            }

            return res;
        }

        public DamageOutArgs TakeDamage(DamageArgs args)
        {
            _currentHealth -= args.Damage;

            if(_currentHealth <= 0)
            {
                OnDeath?.Invoke(args);
            }
            else
            {
                OnDamage?.Invoke(args);
            }

            return new DamageOutArgs();
        }

        public ActorCollider[] AllColliders => GetAllColliders();

        public Vector2 LookDirection => _lookDirection;

        public Vector2 MovementDirection => _movementDirection;

        public float BaseSpeed => _baseSpeed;

        public Vector2 CurrentSpeed => GetVelocity();

        public int MaxHealth => _maxHealth;

        public int CurrentHealth => _currentHealth;
    }
}
