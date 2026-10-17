using System;

namespace Health
{
    public interface IHealth : IDamagable
    {
        public enum Flags
        {
            Invincible
        }

        public event Action<DamageArgs> OnDamage;
        public event Action<DamageArgs> OnDeath;

        int MaxHealth { get; }
        int CurrentHealth { get; }
    }
}
