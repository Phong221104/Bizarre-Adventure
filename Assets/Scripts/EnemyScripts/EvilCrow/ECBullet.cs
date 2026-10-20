using UnityEngine;

public class ECBullet : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private float speed = 8f;
    [SerializeField] private float lifeTime = 5f;

    public GameObject target;
    public GameObject hitEffect;

    private Vector2 direction;

    Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        target = GameObject.FindGameObjectWithTag("Player");

        direction = (target.transform.position - transform.position).normalized;

        Invoke(nameof(DestroySelf), lifeTime);
    }

    private void FixedUpdate()
    {
        rb.MovePosition(rb.position + direction * speed * Time.fixedDeltaTime);
    }

    public void DestroySelf()
    {
        GameObject effect = Instantiate(hitEffect, transform.position, Quaternion.identity);
        Destroy(effect, 0.05f);
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            collision.GetComponent<PlayerHealth>().TakeDamage(damage);
        }

        GameObject effect = Instantiate(hitEffect, transform.position, Quaternion.identity);
        Destroy(effect, 0.7f);
        Destroy(gameObject);
    }
}