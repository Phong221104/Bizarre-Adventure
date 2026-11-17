
using System.Collections.Generic;
using UnityEngine;

public class BurdenBullet : Bullet
{
    private readonly HashSet<EnemyHealth> hitEnemies =
        new HashSet<EnemyHealth>();

    private bool isActive;

    protected override void Awake()
    {
        base.Awake();
    }

    public override void Fire(Vector3 position, Quaternion rotation)
    {
        hitEnemies.Clear();

        base.Fire(position, rotation);

        isActive = true;
    }

    protected override void Update()
    {
        if (!isActive)
            return;

        timer -= Time.deltaTime;

        if (timer <= 0f)
            ReturnPool();
    }

    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        DealDamage(collision);
    }

    private void DealDamage(Collider2D collision)
    {
        if (!isActive || collision == null)
            return;

        EnemyHealth enemy =
            collision.GetComponentInParent<EnemyHealth>();

        if (enemy == null)
            return;

        if (!hitEnemies.Add(enemy))
            return;

        enemy.TakeDamage(damage);

        //if (hitEffectPrefab != null)
        //{
        //    GameObject effect = Instantiate(
        //        hitEffectPrefab,
        //        collision.ClosestPoint(transform.position),
        //        transform.rotation
        //    );

        //    Destroy(effect, hitEffectLifeTime);
        //}
    }

    protected override void ReturnPool()
    {
        if (!isActive)
            return;

        isActive = false;

        BulletPoolManager.Instance.ReturnBullet(this);
    }
}
