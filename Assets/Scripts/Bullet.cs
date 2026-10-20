using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float speed;
    [SerializeField] private float lifeTime;
    [SerializeField] private int damage;

    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private float hitEffectLifeTime = 0.5f;

    private Rigidbody2D rb;

    private float timer;

    public int Damage => damage;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Fire(Vector3 position, Quaternion rotation)
    {
        transform.SetPositionAndRotation(position, rotation);

        gameObject.SetActive(true);

        timer = lifeTime;

        rb.linearVelocity = transform.right * speed;
    }

    private void Update()
    {
        timer -= Time.deltaTime;

        if (timer <= 0)
            ReturnPool();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision == null) return;

        EnemyHealth enemy = collision.GetComponent<EnemyHealth>();

        if (enemy == null)
            return;

        enemy.TakeDamage(damage);

        if (hitEffectPrefab != null)
        {
            GameObject effect = Instantiate(
                hitEffectPrefab,
                transform.position,
                transform.rotation);

            Destroy(effect, hitEffectLifeTime);
        }

        ReturnPool();
    }

    private void ReturnPool()
    {
        rb.linearVelocity = Vector2.zero;

        BulletPoolManager.Instance.ReturnBullet(this);
    }
}