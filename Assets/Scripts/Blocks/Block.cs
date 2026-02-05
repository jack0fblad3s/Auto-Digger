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
        // Auto-collect BlockUnits from children in strict visual order:
        // top-left, top-right, bottom-left, bottom-right.
        blockUnits = GetComponentsInChildren<BlockUnit>()
            .OrderByDescending(u => u.transform.localPosition.y)
            .ThenBy(u => u.transform.localPosition.x)
            .ThenBy(u => u.slotIndex)
            .ToArray();
    }

    public void NotifyUnitDestroyed(BlockUnit destroyedUnit)
    {
        for (int i = 0; i < blockUnits.Length; i++)
        {
            if (blockUnits[i] == destroyedUnit)
            {
                blockUnits[i] = null;
                break;
            }
        }
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
            if (manager == null)
            {
                Debug.LogWarning("Block has no manager assigned during removal.", this);
                return;
            }

            manager.SpawnNeighbors(this);
            manager.RemoveBlock(this);
        }
    }
}
