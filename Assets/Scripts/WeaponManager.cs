using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    [SerializeField] private Weapon currentWeapon;

    public Weapon CurrentWeapon => currentWeapon;

    public void Equip(Weapon weapon)
    {
        if (weapon == null || !weapon.Have)
            return;

        if (currentWeapon != null)
            currentWeapon.gameObject.SetActive(false);

        currentWeapon = weapon;
        currentWeapon.gameObject.SetActive(true);
    }
}