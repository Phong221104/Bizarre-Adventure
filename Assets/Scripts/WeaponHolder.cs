using UnityEngine;

public class WeaponHolder : MonoBehaviour
{
    Weapon[] weapons;

    private void Awake()
    {
        weapons = GetComponentsInChildren<Weapon>(true);
    }

    public Weapon GetWeapon(Weapon.WeaponID id)
    {
        foreach (Weapon weapon in weapons)
        {
            if (weapon.WeaponIDGet == id)
                return weapon;
        }
        return null;
    }
}
