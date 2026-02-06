using UnityEngine;

public class PlayerMine : MonoBehaviour
{
    public float reachDistance = 3f;
    public ProceduralBlockManager blockManager;

    void Awake()
    {
        if (GetComponent<PlayerGridController>() != null)
        {
            // Grid controller already handles mining input.
            enabled = false;
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
            MineBlock();
    }

    void MineBlock()
    {
        if (blockManager == null) return;

        Vector3 rayOrigin = transform.position + Vector3.up;
        Vector3 rayDir = transform.forward;

        if (!Physics.Raycast(rayOrigin, rayDir, out RaycastHit hit, reachDistance))
            return;

        Block block = hit.collider.GetComponent<Block>() ?? hit.collider.GetComponentInParent<Block>();
        if (block == null) return;

        Vector3 toBlock = block.transform.position - transform.position;
        if (Vector3.Dot(toBlock, transform.forward) <= 0) return;

        block.MineNext();
    }
}
