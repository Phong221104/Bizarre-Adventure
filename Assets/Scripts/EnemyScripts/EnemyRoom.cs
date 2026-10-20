using UnityEngine;
using UnityEngine.Tilemaps;

public class EnemyRoom : MonoBehaviour
{
    [System.Serializable]
    public class EnemySpawn
    {
        public GameObject enemyPrefab;
        public Transform spawnPoint;
    }

    [System.Serializable]
    public class Wave
    {
        public EnemySpawn[] enemies;
    }

    [Header("Gate")]
    [SerializeField] private GameObject roomGate;
    [SerializeField] private Tilemap gateTilemap;

    [Header("Waves")]
    [SerializeField] private Wave[] waves;

    [Header("Portal")]
    [SerializeField] private GameObject portal;

    private int currentWave;
    private int aliveEnemies;
    private bool roomStarted;
    private bool roomCleared;

    private void Awake()
    {
        if (roomGate != null)
            roomGate.SetActive(false);

        if (portal != null)
            portal.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (roomStarted || roomCleared)
            return;

        if (!collision.CompareTag("Player"))
            return;

        StartRoom();
    }

    private void StartRoom()
    {
        roomStarted = true;

        if (roomGate != null)
            roomGate.SetActive(true);

        currentWave = 0;
        SpawnWave();
    }

    private void SpawnWave()
    {
        if (currentWave >= waves.Length)
        {
            ClearRoom();
            return;
        }

        aliveEnemies = 0;

        foreach (EnemySpawn spawn in waves[currentWave].enemies)
        {
            if (spawn.enemyPrefab == null || spawn.spawnPoint == null)
                continue;

            GameObject enemy = Instantiate(
                spawn.enemyPrefab,
                spawn.spawnPoint.position,
                spawn.spawnPoint.rotation
            );

            EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();

            if (enemyHealth == null)
                continue;

            aliveEnemies++;
            enemyHealth.OnDeath += OnEnemyDeath;
        }

        if (aliveEnemies == 0)
            OnWaveCleared();
    }

    private void OnEnemyDeath()
    {
        aliveEnemies--;

        if (aliveEnemies <= 0)
            OnWaveCleared();
    }

    private void OnWaveCleared()
    {
        currentWave++;

        if (currentWave >= waves.Length)
        {
            ClearRoom();
            return;
        }

        SpawnWave();
    }

    private void ClearRoom()
    {
        roomCleared = true;

        if (roomGate != null)
            roomGate.SetActive(false);

        if (portal != null)
            portal.SetActive(true);
    }
}