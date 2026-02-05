using UnityEngine;

public class PlayerMine : MonoBehaviour
{
    public float reachDistance = 3f;
    public ProceduralBlockManager blockManager;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            MineBlock();
        }
    }

    void MineBlock()
    {
        if (blockManager == null) return;

        // Player midpoint
        Vector3 rayOrigin = transform.position + Vector3.up; // middle of 2-high player
        Vector3 rayDir = transform.forward;

        // Raycast against blocks in front
        if (Physics.Raycast(rayOrigin, rayDir, out RaycastHit hit, reachDistance))
        {
            // Find the nearest Block in the hit object or parent
            Block block = hit.collider.GetComponent<Block>();
            if (block == null) block = hit.collider.GetComponentInParent<Block>();

            if (block == null) return;

            // Ensure block is in front of player
            Vector3 toBlock = block.transform.position - transform.position;
            if (Vector3.Dot(toBlock, transform.forward) <= 0) return;

            // Snap hit point to grid
            Vector3Int gridPos = new Vector3Int(
                Mathf.FloorToInt(hit.point.x),
                Mathf.FloorToInt(hit.point.y),
                Mathf.FloorToInt(hit.point.z)
            );

            // Get BlockUnit to mine
            BlockUnit targetUnit = block.blockUnits.Length > 0 ? block.blockUnits[0] : null;
            if (targetUnit != null)
            {
                targetUnit.Mine(); // Mine that cube
            }

            // After mining, check if block has any children left
            bool hasUnitsLeft = false;
            foreach (var u in block.blockUnits)
            {
                if (u != null)
                {
                    hasUnitsLeft = true;
                    break;
                }
            }

            if (!hasUnitsLeft)
            {
                // Destroy block and spawn neighbors
                blockManager.SpawnNeighbors(block);
                Destroy(block.gameObject);
            }
        }
    }
}
