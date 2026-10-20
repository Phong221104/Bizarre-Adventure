using UnityEngine;

public class ShopWeapon : MonoBehaviour
{
    [SerializeField] private Weapon.WeaponID weaponID;
    [SerializeField] private int price;

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        if (!Input.GetKeyDown(KeyCode.F))
            return;

        OwnedWeapon ownedWeapon = collision.GetComponent<OwnedWeapon>();
        GoldManager goldManager = collision.GetComponent<GoldManager>();
        WeaponHolder holder = collision.GetComponentInChildren<WeaponHolder>();

        if (ownedWeapon == null || goldManager == null || holder == null)
            return;

        Weapon weapon = holder.GetWeapon(weaponID);

        if (weapon == null || weapon.Have)
            return;

        if (!goldManager.CanAfford(price))
            return;

        Weapon dropped = ownedWeapon.AddWeapon(weapon);

        if (dropped != null && dropped.PickupPrefab != null)
        {
            Instantiate(
                dropped.PickupPrefab,
                collision.transform.position,
                Quaternion.identity
            );
        }

        goldManager.Spend(price);

        Destroy(gameObject);
    }
}