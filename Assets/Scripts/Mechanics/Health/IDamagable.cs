namespace Health
{
    public interface IDamagable
    {
        DamageOutArgs TakeDamage(DamageArgs args);
    }
}
