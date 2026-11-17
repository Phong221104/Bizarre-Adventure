using UnityEngine;

public class RG_ShockWave : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private float speed = 8f;
    [SerializeField] private float lifeTime = 3f;

    public GameObject target;

    private Vector2 direction;

    Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        Invoke(nameof(DestroySelf), lifeTime);
    }

    public void DestroySelf()
    {
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            collision.GetComponent<PlayerStatus>().TakeDamage(damage);
        }
    }
}
