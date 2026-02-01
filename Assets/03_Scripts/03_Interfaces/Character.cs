using UI;
using UnityEngine;
using UnityEngine.TextCore.Text;

public interface ICharacter
{
    float MaxHealth { get; }
    float CurrentHealth { get; }

    void TakeDamage(float damage);
    void Heal(float amount);
    void Die();
}


public abstract class Character : MonoBehaviour, ICharacter
{
    [SerializeField] protected float maxHealth = 10f;
    [SerializeField] protected float currentHealth;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    private UIManager manager;

    protected virtual void Awake()
    {
        currentHealth = maxHealth;
        manager = UIManager.instance;
    }

    public virtual void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        manager.SetHealth(currentHealth);

        if (currentHealth <= 0)
            Die();
    }

    public virtual void Heal(float amount)
    {
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
    }

    public abstract void Die();
}
