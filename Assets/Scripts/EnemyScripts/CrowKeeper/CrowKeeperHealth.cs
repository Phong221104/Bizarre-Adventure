using System.Collections;
using UnityEngine;

public class CrowKeeperHealth : EnemyHealth
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Material onDamageMat;
    [SerializeField] private float flashTime = 0.1f;

    private Material originalMat;

    protected override void Awake()
    {
        base.Awake();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        originalMat = spriteRenderer.material;
    }

    protected override void PlayHurtEffect()
    {
        StopAllCoroutines();
        StartCoroutine(OnDamageEffect());
    }

    private IEnumerator OnDamageEffect()
    {
        spriteRenderer.material = onDamageMat;

        yield return new WaitForSeconds(flashTime);

        spriteRenderer.material = originalMat;
    }
}
