using UnityEngine;

public class BreakableObject : MonoBehaviour
{
    [Header("Gold Drop")]
    [SerializeField] private GameObject goldPrefab;
    [SerializeField] private int minGoldDrop;
    [SerializeField] private int maxGoldDrop;

    [Header("Effect")]
    [SerializeField] private GameObject explosionPrefab;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Bullet"))
            return;

        Die();
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

    private void Die()
    {
        DropGold();

        if (explosionPrefab != null)
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}