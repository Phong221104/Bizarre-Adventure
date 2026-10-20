using UnityEngine;

public class Gold : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        GoldManager goldManager = collision.GetComponent<GoldManager>();

        if (goldManager != null)
        {
            goldManager.AddGold(1);
            Destroy(gameObject);
        }
    }
}