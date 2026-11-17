
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] protected float speed;
    [SerializeField] protected float lifeTime;
    [SerializeField] protected int damage;

    [SerializeField] protected GameObject hitEffectPrefab;
    [SerializeField] protected float hitEffectLifeTime = 0.5f;

    protected Rigidbody2D rb;
    protected float timer;

    public int Damage => damage;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public virtual void Fire(Vector3 position, Quaternion rotation)
    {
        transform.SetPositionAndRotation(position, rotation);

        gameObject.SetActive(true);

        timer = lifeTime;

        rb.linearVelocity = transform.right * speed;
    }

    protected virtual void Update()
    {
        timer -= Time.deltaTime;

        if (timer <= 0)
            ReturnPool();
    }

    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision == null)
            return;

        EnemyHealth enemy = collision.GetComponent<EnemyHealth>();

        if (enemy == null)
            return;

        enemy.TakeDamage(damage);

        if (hitEffectPrefab != null)
        {
            GameObject effect = Instantiate(
                hitEffectPrefab,
                transform.position,
                transform.rotation
            );

            Destroy(effect, hitEffectLifeTime);
        }

        ReturnPool();
    }

    protected virtual void ReturnPool()
    {
        rb.linearVelocity = Vector2.zero;

        BulletPoolManager.Instance.ReturnBullet(this);
    }
}
