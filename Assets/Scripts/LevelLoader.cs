using UnityEngine;

public class LevelLoader : MonoBehaviour
{
    [Header("Level Data")]
    public LevelData currentLevelData;

    [Header("Prefabs")]
    public GameObject wallPrefab;
    public GameObject targetPrefab;
    public GameObject boxPrefab;
    public GameObject playerPrefab;
    public GameObject groundPrefab;

    [Header("Generation")]
    public float gridSpacing = 1.0f;

    private void Start()
    {
        if (currentLevelData == null)
        {
            Debug.LogError("LevelLoader requires a LevelData asset.");
            return;
        }

        GenerateLevel();
    }

    public void GenerateLevel()
    {
        int width = currentLevelData.width;
        int height = currentLevelData.height;

        GridManager.Instance.InitializeGridSize(width, height);

        Transform levelParent = new GameObject("Generated_Level_2D").transform;
        levelParent.parent = transform;

        float offsetX = (width - 1) * gridSpacing / 2f;
        float offsetY = (height - 1) * gridSpacing / 2f;
        Vector3 centerOffset = new Vector3(offsetX, offsetY, 0f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                TileType type = currentLevelData.GetTile(x, y);
                Vector3 spawnPosition = new Vector3(x * gridSpacing, y * gridSpacing, 0f) - centerOffset;

                if (groundPrefab != null && type != TileType.Wall)
                {
                    Instantiate(groundPrefab, spawnPosition, Quaternion.identity, levelParent);
                }

                SpawnTileObjects(type, spawnPosition, x, y, levelParent);
            }
        }
    }

    private void SpawnTileObjects(TileType type, Vector3 position, int x, int y, Transform parent)
    {
        switch (type)
        {
            case TileType.Wall:
                InstantiateRequired(wallPrefab, position, parent, TileType.Wall);
                GridManager.Instance.SetTileType(x, y, TileType.Wall);
                break;
            case TileType.Target:
                InstantiateRequired(targetPrefab, position, parent, TileType.Target);
                GridManager.Instance.SetTileType(x, y, TileType.Target);
                break;
            case TileType.Box:
                InstantiateRequired(boxPrefab, position, parent, TileType.Box);
                GridManager.Instance.SetTileType(x, y, TileType.Box);
                break;
            case TileType.BoxOnTarget:
                InstantiateRequired(targetPrefab, position, parent, TileType.Target);
                InstantiateRequired(boxPrefab, position, parent, TileType.Box);
                GridManager.Instance.SetTileType(x, y, TileType.BoxOnTarget);
                break;
            case TileType.Player:
                InstantiateRequired(playerPrefab, position, parent, TileType.Player);
                GridManager.Instance.SetTileType(x, y, TileType.Player);
                break;
            case TileType.PlayerOnTarget:
                InstantiateRequired(targetPrefab, position, parent, TileType.Target);
                InstantiateRequired(playerPrefab, position, parent, TileType.Player);
                GridManager.Instance.SetTileType(x, y, TileType.PlayerOnTarget);
                break;
            case TileType.Empty:
            default:
                GridManager.Instance.SetTileType(x, y, TileType.Empty);
                break;
        }
    }

    private void InstantiateRequired(GameObject prefab, Vector3 position, Transform parent, TileType tileType)
    {
        if (prefab == null)
        {
            Debug.LogError($"Missing prefab for {tileType}.");
            return;
        }

        Instantiate(prefab, position, Quaternion.identity, parent);
    }
}
