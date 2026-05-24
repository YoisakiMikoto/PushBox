using System;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }
    public static event Action OnLevelComplete;
    public static event Action OnLevelRestored;

    public int Width { get; private set; }
    public int Height { get; private set; }

    private TileType[,] gridData;
    private bool isGameWon;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void InitializeGridSize(int width, int height)
    {
        Width = width;
        Height = height;
        gridData = new TileType[width, height];
        isGameWon = false;
    }

    public TileType GetTileType(int x, int y)
    {
        if (x >= 0 && x < Width && y >= 0 && y < Height)
        {
            return gridData[x, y];
        }

        return TileType.Wall;
    }

    public void SetTileType(int x, int y, TileType type)
    {
        if (x >= 0 && x < Width && y >= 0 && y < Height)
        {
            gridData[x, y] = type;
        }
    }

    public TileType[,] CreateSnapshot()
    {
        TileType[,] snapshot = new TileType[Width, Height];
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                snapshot[x, y] = gridData[x, y];
            }
        }

        return snapshot;
    }

    public void RestoreSnapshot(TileType[,] snapshot)
    {
        if (snapshot == null ||
            snapshot.GetLength(0) != Width ||
            snapshot.GetLength(1) != Height)
        {
            return;
        }

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                gridData[x, y] = snapshot[x, y];
            }
        }

        isGameWon = false;
        OnLevelRestored?.Invoke();
    }

    public void CheckVictory()
    {
        if (isGameWon) return;

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (gridData[x, y] == TileType.Box ||
                    gridData[x, y] == TileType.Target ||
                    gridData[x, y] == TileType.PlayerOnTarget)
                {
                    return;
                }
            }
        }

        Debug.Log("Level complete.");
        isGameWon = true;
        OnLevelComplete?.Invoke();
    }
}
