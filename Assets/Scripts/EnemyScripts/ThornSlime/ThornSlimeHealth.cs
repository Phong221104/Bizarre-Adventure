using System.Collections;
using UnityEngine;

public class ThornSlimeHealth : EnemyHealth
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float flashTime = 0.08f;
    [SerializeField] private int damage = 1;

    private Color originalColor;

    protected override void Awake()
    {
        base.Awake();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        originalColor = spriteRenderer.color;
    }

    protected override void PlayHurtEffect()
    {
        StopAllCoroutines();
        StartCoroutine(FlashWhite());
    }

    private IEnumerator FlashWhite()
    {
        spriteRenderer.color = Color.white;

        yield return new WaitForSeconds(flashTime);

        spriteRenderer.color = originalColor;
    }

    protected override void OnCollisionEnter2D(
        Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player"))
            return;

        PlayerHealth playerHealth =
            collision.gameObject.GetComponent<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
        }
    }
}