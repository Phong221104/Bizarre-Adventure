using UnityEngine;

public class TS_Spike : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private Collider2D damageCollider;

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();

        if (damageCollider == null)
            damageCollider = GetComponent<Collider2D>();

        damageCollider.enabled = false;
    }

    public void EnableHitbox()
    {
        damageCollider.enabled = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!damageCollider.enabled)
            return;

        if (!collision.CompareTag("Player"))
            return;

        PlayerHealth playerHealth =
            collision.GetComponent<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
        }
    }

    public void DestroySelf()
    {
        Destroy(gameObject);
    }
}