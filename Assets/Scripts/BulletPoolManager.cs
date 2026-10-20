using System.Collections.Generic;
using UnityEngine;

public class BulletPoolManager : MonoBehaviour
{
    public static BulletPoolManager Instance { get; private set; }

    [System.Serializable]
    public class BulletPool
    {
        public GameObject prefab;
        public int size = 20;

        [HideInInspector]
        public Queue<Bullet> bullets = new Queue<Bullet>();
    }

    [SerializeField]
    private List<BulletPool> pools;

    private Dictionary<GameObject, BulletPool> poolLookup;

    private Dictionary<Bullet, BulletPool> bulletLookup;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        poolLookup = new Dictionary<GameObject, BulletPool>();
        bulletLookup = new Dictionary<Bullet, BulletPool>();

        foreach (var pool in pools)
        {
            poolLookup.Add(pool.prefab, pool);

            for (int i = 0; i < pool.size; i++)
            {
                GameObject obj = Instantiate(pool.prefab, transform);

                obj.SetActive(false);

                Bullet bullet = obj.GetComponent<Bullet>();

                pool.bullets.Enqueue(bullet);

                bulletLookup.Add(bullet, pool);
            }
        }
    }

    public Bullet Spawn(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        BulletPool pool = poolLookup[prefab];

        Bullet bullet;

        if (pool.bullets.Count > 0)
        {
            bullet = pool.bullets.Dequeue();
        }
        else
        {
            GameObject obj = Instantiate(prefab, transform);

            bullet = obj.GetComponent<Bullet>();

            bulletLookup.Add(bullet, pool);
        }

        bullet.Fire(pos, rot);

        return bullet;
    }

    public void ReturnBullet(Bullet bullet)
    {
        bullet.gameObject.SetActive(false);

        bulletLookup[bullet].bullets.Enqueue(bullet);
    }
}