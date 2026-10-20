using System.Collections;
using UnityEngine;

public class RuinGuardHealth : EnemyHealth
{
    [Header("Hurt")]
    [SerializeField] private float hurtFlashTime = 0.05f;

    [Header("Death")]
    [SerializeField] private float destroyDelay = 4f;

    private SpriteRenderer spriteRenderer;
    private Coroutine hurtCoroutine;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    protected override void Awake()
    {
        base.Awake();

        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    protected override void PlayHurtEffect()
    {
        if (isDead)
            return;

        if (hurtCoroutine != null)
            StopCoroutine(hurtCoroutine);

        hurtCoroutine = StartCoroutine(HurtFlash());
    }

    private IEnumerator HurtFlash()
    {
        if (spriteRenderer == null)
            yield break;

        Color originalColor = spriteRenderer.color;

        spriteRenderer.color = Color.white;

        yield return new WaitForSeconds(hurtFlashTime);

        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
    }

    protected override void Die()
    {
        if (isDead)
            return;

        isDead = true;

        RuinGuardAI ai = GetComponent<RuinGuardAI>();

        if (ai != null)
        {
            ai.Die();
        }
        else
        {
            animator.SetTrigger("Death");
        }

        Collider2D[] colliders =
            GetComponents<Collider2D>();

        foreach (Collider2D collider in colliders)
        {
            collider.enabled = false;
        }

        Destroy(gameObject, destroyDelay);
    }
}
