using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class PlayerGridController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float rotationSpeed = 720f;
    public float gridStep = 1f;

    [Header("References")]
    public ProceduralBlockManager blockManager;

    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private bool isMoving = false;

    [Header("Mining")]
    public float reachDistance = 3f;

    [Header("Player Height")]
    public float standingCenterY = 1f;
    private float effectiveStandingCenterY;

    void Start()
    {
        float size = BlockSize();
        effectiveStandingCenterY = Mathf.Approximately(standingCenterY, 1f) ? size : standingCenterY;

        // Snap player to grid
        targetPosition = new Vector3(
            Mathf.Floor(transform.position.x / size) * size + 0.5f * size,
            effectiveStandingCenterY,
            Mathf.Floor(transform.position.z / size) * size + 0.5f * size
        );

        transform.position = targetPosition;
        targetRotation = transform.rotation;
    }

    void Update()
    {
        HandleInput();
        SmoothMove();
        SmoothRotate();

        if (Input.GetMouseButtonDown(0))
            MineBlock();
    }

    void HandleInput()
    {
        if (isMoving) return;

        Vector3 inputDir = Vector3.zero;

        if (Input.GetKeyDown(KeyCode.W)) inputDir = Vector3.forward;
        if (Input.GetKeyDown(KeyCode.S)) inputDir = Vector3.back;
        if (Input.GetKeyDown(KeyCode.A)) inputDir = Vector3.left;
        if (Input.GetKeyDown(KeyCode.D)) inputDir = Vector3.right;

        if (inputDir != Vector3.zero)
        {
            Vector3 relativeDir = transform.TransformDirection(inputDir);
            relativeDir.y = 0;

            Quaternion newRot = targetRotation;
            if (inputDir == Vector3.left) newRot *= Quaternion.Euler(0, -90f, 0);
            if (inputDir == Vector3.right) newRot *= Quaternion.Euler(0, 90f, 0);

            TryMove(relativeDir.normalized, newRot);
        }
    }

    float BlockSize() => blockManager != null ? Mathf.Max(0.0001f, blockManager.blockSize) : 1f;

    float StepDistance() => gridStep * BlockSize();

    void TryMove(Vector3 dir, Quaternion newRot)
    {
        if (blockManager == null) return;

        Vector3 desired = targetPosition + dir * StepDistance();
        desired.y = effectiveStandingCenterY;

        BoxCollider col = GetComponent<BoxCollider>();
        Vector3 halfExtents = col.size * 0.5f;
        Vector3 scaledCenter = Vector3.Scale(col.center, transform.lossyScale);

        Vector3[] checkPoints =
        {
            new Vector3(halfExtents.x, 0, halfExtents.z),
            new Vector3(-halfExtents.x, 0, halfExtents.z),
            new Vector3(halfExtents.x, 0, -halfExtents.z),
            new Vector3(-halfExtents.x, 0, -halfExtents.z)
        };

        foreach (var pt in checkPoints)
        {
            Vector3 probe = desired + scaledCenter + Vector3.Scale(pt, transform.lossyScale);
            Vector3Int check = blockManager.WorldToGrid(probe);

            if (blockManager.IsOccupied(check))
                return; // blocked
        }

        targetPosition = desired;
        targetRotation = newRot;
        isMoving = true;
    }

    void SmoothMove()
    {
        if (!isMoving) return;
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
        if (Vector3.Distance(transform.position, targetPosition) < 0.001f)
        {
            transform.position = targetPosition;
            isMoving = false;
        }
    }

    void SmoothRotate()
    {
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    void MineBlock()
    {
        if (blockManager == null) return;

        float size = BlockSize();

        // Forward snapped to grid
        Vector3Int forwardGrid = new Vector3Int(
            Mathf.RoundToInt(transform.forward.x),
            0,
            Mathf.RoundToInt(transform.forward.z)
        );
        if (forwardGrid == Vector3Int.zero) return;

        Vector3Int playerGrid = blockManager.WorldToGrid(targetPosition);
        int maxDepth = Mathf.CeilToInt(reachDistance / StepDistance());

        // Iterate depth until we find a block to mine
        for (int depth = 1; depth <= maxDepth; depth++)
        {
            Vector3 blockCenter = targetPosition + new Vector3(forwardGrid.x, 0, forwardGrid.z) * StepDistance() * depth;

            // Sub-unit offsets for **TL → TR → BL → BR**
            Vector3 right = transform.right * 0.25f * size;
            Vector3 up = Vector3.up * 0.25f * size;

            Vector3[] subUnitProbes = new Vector3[]
            {
                blockCenter + up - right, // TL
                blockCenter + up + right, // TR
                blockCenter - up - right, // BL
                blockCenter - up + right  // BR
            };

            // Mine **only the first available sub-unit**
            foreach (var probe in subUnitProbes)
            {
                Vector3Int gridPos = blockManager.WorldToGrid(probe);
                Block block = blockManager.GetBlockAt(gridPos);
                if (block == null) continue;
                if (Vector3.Distance(transform.position, block.transform.position) > reachDistance) continue;

                block.MineNext(); // Mine 1 sub-block per click
                return;        // STOP after mining one
            }

            // Only move deeper **if all 4 sub-blocks were gone**
            bool layerEmpty = true;
            foreach (var probe in subUnitProbes)
            {
                Vector3Int gridPos = blockManager.WorldToGrid(probe);
                Block block = blockManager.GetBlockAt(gridPos);
                if (block != null && block.blockUnits != null && block.blockUnits.Length > 0)
                {
                    layerEmpty = false;
                    break;
                }
            }

            if (!layerEmpty)
                break; // stop if current layer still has sub-blocks
        }
    }
}
