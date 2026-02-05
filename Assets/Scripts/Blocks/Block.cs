using System.Linq;
using UnityEngine;

public class Block : MonoBehaviour
{
    [HideInInspector]
    public ProceduralBlockManager manager;

    // The 4 mineable sub-blocks (TL, TR, BL, BR)
    public BlockUnit[] blockUnits;

    void Awake()
    {
        // Auto-collect BlockUnits from children
        blockUnits = GetComponentsInChildren<BlockUnit>()
            .OrderBy(u => u.slotIndex) // 0 TL, 1 TR, 2 BL, 3 BR
            .ToArray();
    }

    // Called by Player when mining this block
    public void MineNext()
    {
        // Find next mineable unit in order
        BlockUnit unit = blockUnits.FirstOrDefault(u => u != null && u.IsMineable());
        if (unit == null)
            return;

        unit.Mine();

        // If all units are gone, destroy block and spawn neighbors
        if (blockUnits.All(u => u == null || !u.IsMineable()))
        {
            if (manager != null)
                manager.SpawnNeighbors(this);

            manager.RemoveBlock(this);
        }
    }
}
