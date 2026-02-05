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

    void Start()
    {
        // Snap player to edge grid
        Vector3 pos = transform.position;
        transform.position = new Vector3(
            Mathf.Round(pos.x) + 0.5f,
            standingCenterY,
            Mathf.Round(pos.z) + 0.5f
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

    void TryMove(Vector3 dir, Quaternion newRot)
    {
        Vector3 desired = targetPosition + dir * gridStep;
        desired.y = standingCenterY;

        BoxCollider col = GetComponent<BoxCollider>();
        Vector3 halfExtents = col.size * 0.5f;
        Vector3[] checkPoints = new Vector3[]
        {
            new Vector3(halfExtents.x, 0, halfExtents.z),
            new Vector3(-halfExtents.x, 0, halfExtents.z),
            new Vector3(halfExtents.x, 0, -halfExtents.z),
            new Vector3(-halfExtents.x, 0, -halfExtents.z)
        };

        foreach (var pt in checkPoints)
        {
            Vector3 check = new Vector3(
                Mathf.FloorToInt(desired.x + pt.x),
                Mathf.FloorToInt(desired.y),
                Mathf.FloorToInt(desired.z + pt.z)
            );

            if (blockManager.IsOccupied(Vector3Int.RoundToInt(check)))
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

        Vector3 feetPosition = transform.position + Vector3.down;
        Vector3Int playerGrid = new Vector3Int(
            Mathf.FloorToInt(transform.position.x),
            0,
            Mathf.FloorToInt(transform.position.z)
        );

        // Mine only the two block layers at the player's body height,
        // top first (1.5y from feet), then bottom (0.5y from feet).
        int topLayerY = Mathf.FloorToInt(feetPosition.y + 1.5f);
        int bottomLayerY = Mathf.FloorToInt(feetPosition.y + 0.5f);

        Vector3Int[] mineOrder =
        {
            new Vector3Int(playerGrid.x + forward.x, topLayerY, playerGrid.z + forward.z),
            new Vector3Int(playerGrid.x + forward.x, bottomLayerY, playerGrid.z + forward.z)
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
