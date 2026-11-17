using System.Collections;
using UnityEngine;

public class PlayerStatus : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;

    [Header("Health Regeneration")]
    [SerializeField] private float regenerationDelay = 5f;
    [SerializeField] private float regenerationInterval = 1f;
    [SerializeField] private int regenerationAmount = 1;

    [Header("Toxin Debuff")]
    [SerializeField] private float toxinDamageMultiplier = 1.25f;
    [SerializeField] private float toxinDuration = 4f;

    private bool isDead;
    private bool isToxinActive;

    private Animator animator;
    private Coroutine toxinCoroutine;
    private Coroutine regenerationCoroutine;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => isDead;
    public bool IsToxinActive => isToxinActive;

    private void Awake()
    {
        currentHealth = maxHealth;

        animator = GetComponent<Animator>();
    }

    public void TakeDamage(int damage)
    {
        if (isDead)
            return;

        int finalDamage = damage;

        if (isToxinActive)
        {
            finalDamage = Mathf.RoundToInt(
                damage * toxinDamageMultiplier
            );
        }

        currentHealth -= finalDamage;

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
            return;
        }

        Hurt();

        if (regenerationCoroutine != null)
        {
            StopCoroutine(regenerationCoroutine);
        }

        regenerationCoroutine = StartCoroutine(RegenerationRoutine());
    }

    private void Hurt()
    {
        animator.SetTrigger("Hurt");
    }

    public void ApplyToxinDebuff()
    {
        if (isDead)
            return;

        if (toxinCoroutine != null)
        {
            StopCoroutine(toxinCoroutine);
        }

        toxinCoroutine = StartCoroutine(ToxinRoutine());
    }

    private IEnumerator ToxinRoutine()
    {
        isToxinActive = true;

        yield return new WaitForSeconds(toxinDuration);

        isToxinActive = false;
        toxinCoroutine = null;
    }

    private IEnumerator RegenerationRoutine()
    {
        yield return new WaitForSeconds(regenerationDelay);

        while (!isDead && currentHealth < maxHealth)
        {
            yield return new WaitForSeconds(regenerationInterval);

            if (currentHealth < maxHealth)
            {
                currentHealth = Mathf.Min(
                    currentHealth + regenerationAmount,
                    maxHealth
                );
            }
        }

        regenerationCoroutine = null;
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;

        if (regenerationCoroutine != null)
        {
            StopCoroutine(regenerationCoroutine);
            regenerationCoroutine = null;
        }

        animator.SetTrigger("Die");
        gameObject.SetActive(false);
    }
}