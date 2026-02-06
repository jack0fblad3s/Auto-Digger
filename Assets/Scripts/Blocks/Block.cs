using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Block : MonoBehaviour
{
    [HideInInspector]
    public ProceduralBlockManager manager;

    public BlockUnit[] blockUnits;

    void Awake()
    {
        RefreshBlockUnitsIfNeeded();
    }

    public void RefreshBlockUnitsIfNeeded()
    {
        BlockUnit[] units = GetComponentsInChildren<BlockUnit>(true);
        if (units == null || units.Length == 0)
            return;

        // Order by slotIndex if available, otherwise by local position
        if (units.All(u => u != null))
        {
            HashSet<int> indices = units.Select(u => u.slotIndex).ToHashSet();
            if (indices.SetEquals(new[] { 0, 1, 2, 3 }))
                blockUnits = units.OrderBy(u => u.slotIndex).ToArray();
            else
                blockUnits = units.OrderByDescending(u => u.transform.localPosition.y)
                                  .ThenBy(u => u.transform.localPosition.x)
                                  .ToArray();
        }
        else
        {
            blockUnits = units.OrderByDescending(u => u.transform.localPosition.y)
                              .ThenBy(u => u.transform.localPosition.x)
                              .ToArray();
        }
    }

    public void NotifyUnitDestroyed(BlockUnit destroyedUnit)
    {
        for (int i = 0; i < blockUnits.Length; i++)
            if (blockUnits[i] == destroyedUnit)
                blockUnits[i] = null;
    }

    // Mine the next available sub-unit in the predefined order
    public void MineNext()
    {
        RefreshBlockUnitsIfNeeded();

        if (blockUnits == null || blockUnits.Length == 0) return;

        BlockUnit nextUnit = blockUnits.FirstOrDefault(u => u != null && u.IsMineable());
        if (nextUnit == null) return;

        nextUnit.Mine();

        // If all units are gone, remove the block and spawn neighbors
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
