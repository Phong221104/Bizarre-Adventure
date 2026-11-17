using Unity.VisualScripting;
using UnityEngine;

public class Gold : MonoBehaviour
{
    [SerializeField] private GameObject coinDisappearEffect;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        GoldManager goldManager = collision.GetComponent<GoldManager>();

        if (goldManager != null)
        {
            goldManager.AddGold(1);
            Instantiate(coinDisappearEffect, gameObject.transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
    }
}