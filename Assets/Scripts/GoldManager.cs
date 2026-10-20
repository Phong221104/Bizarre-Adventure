using UnityEngine;

public class GoldManager : MonoBehaviour
{
    [SerializeField] private int gold;

    public int Gold => gold;

    public bool CanAfford(int amount)
    {
        return gold >= amount;
    }

    public bool Spend(int amount)
    {
        if (amount < 0)
            return false;

        if (gold < amount)
            return false;

        gold -= amount;
        return true;
    }

    public void AddGold(int amount)
    {
        if (amount <= 0)
            return;

        gold += amount;
    }
}