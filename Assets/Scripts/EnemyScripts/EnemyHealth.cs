using UnityEngine;

public abstract class EnemyHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] protected int maxHealth = 5;
    [SerializeField] protected int contactDamage = 1;

    [SerializeField] private GameObject goldPrefab;
    [SerializeField] private int minGoldDrop;
    [SerializeField] private int maxGoldDrop;

    [SerializeField] protected int currentHealth;
    [SerializeField] protected bool isDead;
    public System.Action OnDeath;

    protected Animator animator;

    protected virtual void Awake()
    {
        currentHealth = maxHealth;
        animator = GetComponent<Animator>();
    }

    public virtual void TakeDamage(int damage)
    {
        if (isDead)
            return;

        currentHealth -= damage;

        PlayHurtEffect();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void DropGold()
    {
        if (goldPrefab == null || maxGoldDrop <= 0)
            return;

        int goldAmount = Random.Range(minGoldDrop, maxGoldDrop + 1);

        for (int i = 0; i < goldAmount; i++)
        {
            Vector3 randomOffset = new Vector3(
                Random.Range(-0.3f, 0.3f),
                Random.Range(-0.3f, 0.3f),
                0f
            );

            Instantiate(goldPrefab, transform.position + randomOffset, Quaternion.identity);
        }
    }

    protected virtual void Die()
    {
        if (isDead)
            return;

        isDead = true;
        DropGold();
        OnDeath?.Invoke();

        animator.SetTrigger("Die");

        GetComponent<Collider2D>().enabled = false;
        GetComponent<Rigidbody2D>().simulated = false;

        Destroy(gameObject, 0.5f);
    }

    protected abstract void PlayHurtEffect();

    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player"))
            return;

        collision.gameObject.GetComponent<PlayerStatus>().TakeDamage(contactDamage);
    }
}