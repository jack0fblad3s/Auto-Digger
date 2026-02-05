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
        // Snap player to edge grid
        Vector3 pos = transform.position;
        float size = BlockSize();
        effectiveStandingCenterY = Mathf.Approximately(standingCenterY, 1f) ? size : standingCenterY;

        transform.position = new Vector3(
            Mathf.Floor(pos.x / size) * size + (0.5f * size),
            effectiveStandingCenterY,
            Mathf.Floor(pos.z / size) * size + (0.5f * size)
        );

        targetPosition = transform.position;
        targetRotation = transform.rotation;
    }

    void Update()
    {
        HandleInput();
        SmoothMove();
        SmoothRotate();
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


    float BlockSize()
    {
        if (blockManager == null) return 1f;
        return Mathf.Max(0.0001f, blockManager.blockSize);
    }

    float StepDistance()
    {
        return gridStep * BlockSize();
    }

    void TryMove(Vector3 dir, Quaternion newRot)
    {
        if (blockManager == null) return;

        Vector3 desired = targetPosition + dir * StepDistance();
        desired.y = effectiveStandingCenterY;

        BoxCollider col = GetComponent<BoxCollider>();
        Vector3 halfExtents = col.size * 0.5f;
        Vector3 scaledCenter = Vector3.Scale(col.center, transform.lossyScale);
        Vector3[] checkPoints = new Vector3[]
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

    void FixedUpdate()
    {
        if (Input.GetMouseButtonDown(0))
            MineBlock();
    }

    void MineBlock()
    {
        if (blockManager == null) return;

        Vector3Int forward = new Vector3Int(
            Mathf.RoundToInt(transform.forward.x),
            0,
            Mathf.RoundToInt(transform.forward.z)
        );

        if (forward == Vector3Int.zero) return;

        float size = BlockSize();
        Vector3 stepOffset = new Vector3(forward.x, 0f, forward.z) * StepDistance();
        Vector3 topProbe = transform.position + stepOffset + new Vector3(0f, 0.5f * size, 0f);
        Vector3 bottomProbe = transform.position + stepOffset + new Vector3(0f, -0.5f * size, 0f);

        Vector3Int[] mineOrder =
        {
            blockManager.WorldToGrid(topProbe),
            blockManager.WorldToGrid(bottomProbe)
        };

        foreach (var targetPos in mineOrder)
        {
            Block block = blockManager.GetBlockAt(targetPos);
            if (block == null) continue;

            if (Vector3.Distance(transform.position, block.transform.position) > reachDistance) continue;

            block.MineNext();
            return;
        }
    }
}
