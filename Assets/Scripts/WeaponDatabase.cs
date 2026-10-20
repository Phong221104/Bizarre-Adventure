using System;
using UnityEngine;

public class WeaponDatabase : MonoBehaviour
{
    [Serializable]
    public class WeaponData
    {
        public Weapon.WeaponID weaponID;
        public GameObject pickupPrefab;
        public int price;
    }

    [SerializeField] private WeaponData[] weapons;

    public WeaponData Get(Weapon.WeaponID id)
    {
        foreach (WeaponData weapon in weapons)
        {
            if (weapon.weaponID == id)
                return weapon;
        }

        return null;
    }
}