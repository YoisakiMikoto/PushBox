using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;

    private int gridX;
    private int gridY;
    private bool isMoving;
    private bool movementDisabled;
    private float offsetX;
    private float offsetY;
    private float spacing;
    private readonly Stack<MoveSnapshot> undoStack = new Stack<MoveSnapshot>();

    private void OnEnable()
    {
        GridManager.OnLevelComplete += DisableMovement;
    }

    private void OnDisable()
    {
        GridManager.OnLevelComplete -= DisableMovement;
    }

    private void Start()
    {
        LevelLoader loader = FindFirstObjectByType<LevelLoader>();
        if (loader == null || loader.currentLevelData == null)
        {
            Debug.LogError("PlayerController requires a LevelLoader with LevelData.");
            enabled = false;
            return;
        }

        spacing = loader.gridSpacing;
        offsetX = (loader.currentLevelData.width - 1) * spacing / 2f;
        offsetY = (loader.currentLevelData.height - 1) * spacing / 2f;

        gridX = Mathf.RoundToInt((transform.position.x + offsetX) / spacing);
        gridY = Mathf.RoundToInt((transform.position.y + offsetY) / spacing);
    }

    private void Update()
    {
        if (isMoving) return;

        if (Input.GetKeyDown(KeyCode.Z))
        {
            UndoLastMove();
            return;
        }

        if (movementDisabled) return;

        int inputX = 0;
        int inputY = 0;

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) inputY = 1;
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) inputY = -1;
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) inputX = -1;
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) inputX = 1;

        if (inputX != 0 || inputY != 0)
        {
            TryMove(inputX, inputY);
        }
    }

    private void DisableMovement()
    {
        movementDisabled = true;
    }

    private Vector3 GetWorldPosition(int x, int y)
    {
        return new Vector3(x * spacing - offsetX, y * spacing - offsetY, 0);
    }

    private void TryMove(int dirX, int dirY)
    {
        int nextX = gridX + dirX;
        int nextY = gridY + dirY;

        TileType nextTile = GridManager.Instance.GetTileType(nextX, nextY);
        if (nextTile == TileType.Wall) return;

        if (nextTile == TileType.Empty || nextTile == TileType.Target)
        {
            RecordUndoSnapshot(null);
            MovePlayerGridData(nextX, nextY);
            StartCoroutine(SmoothMoveTo(GetWorldPosition(nextX, nextY)));
            return;
        }

        if (nextTile == TileType.Box || nextTile == TileType.BoxOnTarget)
        {
            int boxNextX = nextX + dirX;
            int boxNextY = nextY + dirY;
            TileType boxNextTile = GridManager.Instance.GetTileType(boxNextX, boxNextY);

            if ((boxNextTile == TileType.Empty || boxNextTile == TileType.Target) &&
                TryGetBoxTransform(nextX, nextY, out Transform boxTransform))
            {
                RecordUndoSnapshot(boxTransform);
                StartCoroutine(SmoothMoveObject(boxTransform, GetWorldPosition(boxNextX, boxNextY)));
                MoveBoxGridData(nextX, nextY, boxNextX, boxNextY);
                MovePlayerGridData(nextX, nextY);
                StartCoroutine(SmoothMoveTo(GetWorldPosition(nextX, nextY)));
            }
        }
    }

    private void MovePlayerGridData(int targetX, int targetY)
    {
        TileType currentTile = GridManager.Instance.GetTileType(gridX, gridY);
        GridManager.Instance.SetTileType(gridX, gridY, currentTile == TileType.PlayerOnTarget ? TileType.Target : TileType.Empty);

        TileType targetTile = GridManager.Instance.GetTileType(targetX, targetY);
        GridManager.Instance.SetTileType(targetX, targetY, targetTile == TileType.Target ? TileType.PlayerOnTarget : TileType.Player);

        gridX = targetX;
        gridY = targetY;
    }

    private void MoveBoxGridData(int fromX, int fromY, int toX, int toY)
    {
        TileType currentBoxTile = GridManager.Instance.GetTileType(fromX, fromY);
        GridManager.Instance.SetTileType(fromX, fromY, currentBoxTile == TileType.BoxOnTarget ? TileType.Target : TileType.Empty);

        TileType targetBoxTile = GridManager.Instance.GetTileType(toX, toY);
        GridManager.Instance.SetTileType(toX, toY, targetBoxTile == TileType.Target ? TileType.BoxOnTarget : TileType.Box);

        GridManager.Instance.CheckVictory();
    }

    private bool TryGetBoxTransform(int x, int y, out Transform boxTransform)
    {
        boxTransform = null;
        Vector2 worldPosition = GetWorldPosition(x, y);
        Collider2D hitCollider = Physics2D.OverlapCircle(worldPosition, 0.2f);

        if (hitCollider != null && hitCollider.CompareTag("Box"))
        {
            boxTransform = hitCollider.transform;
            return true;
        }

        return false;
    }

    private void RecordUndoSnapshot(Transform movedBox)
    {
        undoStack.Push(new MoveSnapshot
        {
            gridSnapshot = GridManager.Instance.CreateSnapshot(),
            playerX = gridX,
            playerY = gridY,
            playerPosition = transform.position,
            movedBox = movedBox,
            boxPosition = movedBox != null ? movedBox.position : Vector3.zero
        });
    }

    private void UndoLastMove()
    {
        if (undoStack.Count == 0) return;

        MoveSnapshot snapshot = undoStack.Pop();
        GridManager.Instance.RestoreSnapshot(snapshot.gridSnapshot);

        gridX = snapshot.playerX;
        gridY = snapshot.playerY;
        transform.position = snapshot.playerPosition;

        if (snapshot.movedBox != null)
        {
            snapshot.movedBox.position = snapshot.boxPosition;
        }

        movementDisabled = false;
    }

    private IEnumerator SmoothMoveTo(Vector3 targetPosition)
    {
        isMoving = true;
        while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = targetPosition;
        isMoving = false;
    }

    private IEnumerator SmoothMoveObject(Transform objTransform, Vector3 targetPosition)
    {
        while (Vector3.Distance(objTransform.position, targetPosition) > 0.01f)
        {
            objTransform.position = Vector3.MoveTowards(objTransform.position, targetPosition, moveSpeed * Time.deltaTime);
            yield return null;
        }

        objTransform.position = targetPosition;
    }

    private struct MoveSnapshot
    {
        public TileType[,] gridSnapshot;
        public int playerX;
        public int playerY;
        public Vector3 playerPosition;
        public Transform movedBox;
        public Vector3 boxPosition;
    }
}
