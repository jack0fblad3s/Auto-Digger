using UnityEngine;

public class BlockUnit : MonoBehaviour
{
    public int clicksRequired = 3;
    public int slotIndex;     // 0 TL, 1 TR, 2 BL, 3 BR
    public int oreRarity = 1;

    private int hits;
    private Block parent;

    void Awake()
    {
        parent = GetComponentInParent<Block>();
    }

    public bool IsMineable()
    {
        return hits < clicksRequired;
    }

    public void Mine()
    {
        hits++;

        if (hits >= clicksRequired)
        {
            Destroy(gameObject);
            parent.NotifyUnitDestroyed(this);
        }
    }
}
