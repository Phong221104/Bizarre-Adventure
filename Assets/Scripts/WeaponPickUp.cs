using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    [SerializeField] private Weapon.WeaponID weaponID;

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        if (!Input.GetKeyDown(KeyCode.F))
            return;

        OwnedWeapon ownedWeapon = collision.GetComponent<OwnedWeapon>();
        WeaponHolder holder = collision.GetComponentInChildren<WeaponHolder>();

        if (ownedWeapon == null || holder == null)
            return;

        Weapon weapon = holder.GetWeapon(weaponID);

        if (weapon == null || weapon.Have)
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

        Destroy(gameObject);
    }
}