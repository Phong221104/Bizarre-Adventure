using UnityEngine;

public class ASArrow : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private float speed = 8f;
    [SerializeField] private float lifeTime = 5f;

    public GameObject target;
    private Vector2 direction;

    Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        target = GameObject.FindGameObjectWithTag("Player");

        direction = (target.transform.position - transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        Invoke(nameof(DestroySelf), lifeTime);
    }

    private void FixedUpdate()
    {
        rb.MovePosition(rb.position + direction * speed * Time.fixedDeltaTime);
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

        Destroy(gameObject);
    }
}
