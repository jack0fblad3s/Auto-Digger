using System.Collections.Generic;
using UnityEngine;

public class ProceduralBlockManager : MonoBehaviour
{
    [Header("References")]
    public GameObject OreBlock;
    public GameObject CeilingFloorBlock;
    public Transform player;

    [Header("Grid Settings")]
    public int hollowX = 8;
    public int hollowY = 3;
    public int hollowZ = 8;
    public float blockSize = 1f;

    // Grid storage
    private Dictionary<Vector3Int, Block> blockGrid = new();
    private HashSet<Vector3Int> minedPositions = new();
    private Vector3Int hollowOrigin;

    void Start()
    {
        if (OreBlock == null || CeilingFloorBlock == null || player == null)
        {
            Debug.LogError("Assign OreBlock, CeilingFloorBlock, and Player!");
            return;
        }

        SpawnInitialRoom();
    }

    // ===================== INITIAL ROOM =====================
    void SpawnInitialRoom()
    {
        float size = Mathf.Max(0.0001f, blockSize);
        Vector3Int baseGrid = new Vector3Int(
            Mathf.FloorToInt(player.position.x / size),
            0,
            Mathf.FloorToInt(player.position.z / size)
        );

        int startX = baseGrid.x;
        int startZ = baseGrid.z;
        int startY = 0;

        hollowOrigin = new Vector3Int(startX, startY, startZ);

        int endX = startX + hollowX - 1;
        int endY = startY + hollowY - 1;
        int endZ = startZ + hollowZ - 1;

        // FLOOR
        for (int x = startX - 1; x <= endX + 1; x++)
            for (int z = startZ - 1; z <= endZ + 1; z++)
                SpawnBlockAt(new Vector3Int(x, -1, z), CeilingFloorBlock);

        // WALLS
        for (int y = startY; y <= endY; y++)
        {
            for (int z = startZ - 1; z <= endZ + 1; z++)
            {
                SpawnBlockAt(new Vector3Int(startX - 1, y, z), OreBlock);
                SpawnBlockAt(new Vector3Int(endX + 1, y, z), OreBlock);
            }

            for (int x = startX; x <= endX; x++)
            {
                SpawnBlockAt(new Vector3Int(x, y, startZ - 1), OreBlock);
                SpawnBlockAt(new Vector3Int(x, y, endZ + 1), OreBlock);
            }
        }

        // CEILING
        for (int x = startX - 1; x <= endX + 1; x++)
            for (int z = startZ - 1; z <= endZ + 1; z++)
                SpawnBlockAt(new Vector3Int(x, endY + 1, z), CeilingFloorBlock);

        // PLACE PLAYER (1×1×2)
        player.position = new Vector3(
            (startX + 0.5f) * size,
            size,
            (startZ + 0.5f) * size
        );
    }

    // ===================== BLOCK SPAWN =====================
    public void SpawnBlockAt(Vector3Int gridPos, GameObject prefab)
    {
        if (blockGrid.ContainsKey(gridPos)) return;

        float size = Mathf.Max(0.0001f, blockSize);
        float worldY = (gridPos.y + 0.5f) * size;
        Vector3 worldPos = new Vector3(
            gridPos.x * size,
            worldY,
            gridPos.z * size
        );

        GameObject go = Instantiate(prefab, worldPos, Quaternion.identity);
        Block block = go.GetComponent<Block>();

        if (block != null)
        {
            block.manager = this;
            blockGrid[gridPos] = block;
        }
    }

    // ===================== GRID QUERIES =====================
    public bool IsOccupied(Vector3Int gridPos)
    {
        return blockGrid.ContainsKey(gridPos);
    }

    public Block GetBlockAt(Vector3Int gridPos)
    {
        blockGrid.TryGetValue(gridPos, out Block block);
        return block;
    }

    // ===================== REMOVAL =====================
    public void RemoveBlock(Block block)
    {
        if (block == null) return;

        Vector3Int gridPos = WorldToGrid(block.transform.position);
        blockGrid.Remove(gridPos);
        minedPositions.Add(gridPos);
        Destroy(block.gameObject);
    }

    // ===================== NEIGHBOR SPAWN =====================
    public void SpawnNeighbors(Block minedBlock)
    {
        if (minedBlock == null) return;

        Vector3Int gridPos = WorldToGrid(minedBlock.transform.position);

        Vector3Int[] dirs =
        {
            Vector3Int.left,
            Vector3Int.right,
            Vector3Int.forward,
            Vector3Int.back,
            Vector3Int.up,
            Vector3Int.down
        };

        foreach (var dir in dirs)
        {
            Vector3Int n = gridPos + dir;
            if (blockGrid.ContainsKey(n)) continue;
            if (minedPositions.Contains(n)) continue;
            if (IsInsideHollow(n)) continue;

            GameObject prefab =
                (n.y == hollowOrigin.y - 1 || n.y == hollowOrigin.y + hollowY) ? CeilingFloorBlock : OreBlock;

            SpawnBlockAt(n, prefab);
        }
    }

    // ===================== HELPERS =====================
    public Vector3Int WorldToGrid(Vector3 world)
    {
        float size = Mathf.Max(0.0001f, blockSize);
        return new Vector3Int(
            Mathf.RoundToInt(world.x / size),
            Mathf.RoundToInt(world.y / size - 0.5f),
            Mathf.RoundToInt(world.z / size)
        );
    }

    bool IsInsideHollow(Vector3Int pos)
    {
        return pos.x >= hollowOrigin.x && pos.x < hollowOrigin.x + hollowX &&
               pos.y >= hollowOrigin.y && pos.y < hollowOrigin.y + hollowY &&
               pos.z >= hollowOrigin.z && pos.z < hollowOrigin.z + hollowZ;
    }
}
