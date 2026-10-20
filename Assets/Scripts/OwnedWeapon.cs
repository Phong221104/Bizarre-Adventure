using UnityEngine;

public class OwnedWeapon : MonoBehaviour
{
    [SerializeField] private WeaponHolder holder;
    [SerializeField] private WeaponManager weaponManager;

    [SerializeField] private Weapon slot1;
    [SerializeField] private Weapon slot2;

    [Header("Switch Weapon")]
    [SerializeField] private float switchCooldown;
    private float currentSwitchCooldown;

    public Weapon Slot1 => slot1;
    public Weapon Slot2 => slot2;

    private void Start()
    {
        slot1 = holder.GetWeapon(Weapon.WeaponID.Pistol);

        if (slot1 != null)
        {
            slot1.Have = true;
            weaponManager.Equip(slot1);
        }
    }

    private void Update()
    {
        if (currentSwitchCooldown > 0)
            currentSwitchCooldown -= Time.deltaTime;

        if (currentSwitchCooldown > 0)
            return;

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            EquipSlot(1);
            currentSwitchCooldown = switchCooldown;
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            EquipSlot(2);
            currentSwitchCooldown = switchCooldown;
        }
    }

    public Weapon AddWeapon(Weapon weapon)
    {
        if (weapon == null || weapon.Have)
            return null;

        if (slot1 == null)
        {
            slot1 = weapon;
            weapon.Have = true;
            weaponManager.Equip(weapon);
            return null;
        }

        if (slot2 == null)
        {
            slot2 = weapon;
            weapon.Have = true;
            weaponManager.Equip(weapon);
            return null;
        }

        Weapon current = weaponManager.CurrentWeapon;

        if (current == slot1)
        {
            slot1 = weapon;
        }
        else if (current == slot2)
        {
            slot2 = weapon;
        }
        else
        {
            return null;
        }

        current.Have = false;
        weapon.Have = true;

        weaponManager.Equip(weapon);

        return current;
    }

    public Weapon ReplaceCurrentWeapon(Weapon newWeapon)
    {
        Weapon current = weaponManager.CurrentWeapon;

        if (current == null)
        {
            if (slot1 == null)
            {
                slot1 = newWeapon;
                newWeapon.Have = true;
                weaponManager.Equip(newWeapon);
                return null;
            }

            if (slot2 == null)
            {
                slot2 = newWeapon;
                newWeapon.Have = true;
                weaponManager.Equip(newWeapon);
                return null;
            }

            current = slot1;
        }

        if (slot1 == current)
        {
            Weapon dropped = slot1;

            slot1 = newWeapon;
            newWeapon.Have = true;
            dropped.Have = false;

            weaponManager.Equip(newWeapon);

            return dropped;
        }

        if (slot2 == current)
        {
            Weapon dropped = slot2;

            slot2 = newWeapon;
            newWeapon.Have = true;
            dropped.Have = false;

            weaponManager.Equip(newWeapon);

            return dropped;
        }

        return null;
    }

    private void EquipSlot(int slot)
    {
        Weapon weapon = slot == 1 ? slot1 : slot2;

        if (weapon == null)
            return;

        if (weapon == weaponManager.CurrentWeapon)
            return;

        weaponManager.Equip(weapon);
    }
}