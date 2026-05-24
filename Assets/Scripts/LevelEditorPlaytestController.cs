using System.Collections.Generic;
using UnityEngine;

public class LevelEditorPlaytestController : MonoBehaviour
{
    private int width;
    private int height;
    private float offsetX;
    private float offsetY;
    private float gridSpacing;
    private TileType[,] tiles;
    private GameObject[,] placedObjects;
    private int playerX;
    private int playerY;
    private readonly Stack<PlaytestSnapshot> undoStack = new Stack<PlaytestSnapshot>();

    public void Initialize(int width, int height, float gridSpacing, TileType[,] tiles, GameObject[,] placedObjects, int playerX, int playerY)
    {
        this.width = width;
        this.height = height;
        this.gridSpacing = gridSpacing;
        this.tiles = tiles;
        this.placedObjects = placedObjects;
        this.playerX = playerX;
        this.playerY = playerY;
        offsetX = (width - 1) * gridSpacing / 2f;
        offsetY = (height - 1) * gridSpacing / 2f;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z))
        {
            UndoLastMove();
            return;
        }

        int inputX = 0;
        int inputY = 0;

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) inputY = 1;
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) inputY = -1;
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) inputX = -1;
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) inputX = 1;

        if (inputX != 0 || inputY != 0)
        {
            TryMovePlayer(inputX, inputY);
        }
    }

    private void TryMovePlayer(int dirX, int dirY)
    {
        int nextX = playerX + dirX;
        int nextY = playerY + dirY;
        TileType nextTile = GetTile(nextX, nextY);

        if (nextTile == TileType.Wall) return;

        if (nextTile == TileType.Box || nextTile == TileType.BoxOnTarget)
        {
            int boxNextX = nextX + dirX;
            int boxNextY = nextY + dirY;
            TileType boxNextTile = GetTile(boxNextX, boxNextY);
            if (boxNextTile != TileType.Empty && boxNextTile != TileType.Target) return;

            RecordUndoSnapshot();
            MoveObject(nextX, nextY, boxNextX, boxNextY);
            tiles[boxNextX, boxNextY] = boxNextTile == TileType.Target ? TileType.BoxOnTarget : TileType.Box;
            tiles[nextX, nextY] = nextTile == TileType.BoxOnTarget ? TileType.Target : TileType.Empty;
        }
        else
        {
            RecordUndoSnapshot();
        }

        MoveObject(playerX, playerY, nextX, nextY);
        tiles[playerX, playerY] = tiles[playerX, playerY] == TileType.PlayerOnTarget ? TileType.Target : TileType.Empty;
        tiles[nextX, nextY] = tiles[nextX, nextY] == TileType.Target ? TileType.PlayerOnTarget : TileType.Player;
        playerX = nextX;
        playerY = nextY;
    }

    private void RecordUndoSnapshot()
    {
        undoStack.Push(new PlaytestSnapshot
        {
            tiles = CloneTiles(tiles),
            placedObjects = CloneObjects(placedObjects),
            playerX = playerX,
            playerY = playerY
        });
    }

    private void UndoLastMove()
    {
        if (undoStack.Count == 0) return;

        PlaytestSnapshot snapshot = undoStack.Pop();
        tiles = snapshot.tiles;
        playerX = snapshot.playerX;
        playerY = snapshot.playerY;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                placedObjects[x, y] = snapshot.placedObjects[x, y];
                if (placedObjects[x, y] != null)
                {
                    placedObjects[x, y].transform.position = GetWorldPosition(x, y);
                }
            }
        }
    }

    private TileType[,] CloneTiles(TileType[,] source)
    {
        TileType[,] clone = new TileType[width, height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                clone[x, y] = source[x, y];
            }
        }

        return clone;
    }

    private GameObject[,] CloneObjects(GameObject[,] source)
    {
        GameObject[,] clone = new GameObject[width, height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                clone[x, y] = source[x, y];
            }
        }

        return clone;
    }

    private TileType GetTile(int x, int y)
    {
        if (x >= 0 && x < width && y >= 0 && y < height)
        {
            return tiles[x, y];
        }

        return TileType.Wall;
    }

    private void MoveObject(int fromX, int fromY, int toX, int toY)
    {
        GameObject movedObject = placedObjects[fromX, fromY];
        placedObjects[fromX, fromY] = null;
        placedObjects[toX, toY] = movedObject;
        if (movedObject != null)
        {
            movedObject.transform.position = GetWorldPosition(toX, toY);
        }
    }

    private Vector3 GetWorldPosition(int x, int y)
    {
        return new Vector3(x * gridSpacing - offsetX, y * gridSpacing - offsetY, 0f);
    }

    private struct PlaytestSnapshot
    {
        public TileType[,] tiles;
        public GameObject[,] placedObjects;
        public int playerX;
        public int playerY;
    }
}
