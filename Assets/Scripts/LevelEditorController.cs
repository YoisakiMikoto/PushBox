using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelEditorController : MonoBehaviour
{
    public int width = 10;
    public int height = 10;
    public float gridSpacing = 1f;

    public GameObject wallPrefab;
    public GameObject targetPrefab;
    public GameObject boxPrefab;
    public GameObject playerPrefab;

    public Button wallButton;
    public Button emptyButton;
    public Button targetButton;
    public Button boxButton;
    public Button playerButton;
    public Button playtestButton;
    public Button backButton;
    public Text warningText;

    private TileType currentMode = TileType.Wall;
    private TileType[,] tiles;
    private TileType[,] editorSnapshot;
    private GameObject[,] placedObjects;
    private GameObject previewObject;
    private Transform tileParent;
    private Transform previewParent;
    private float offsetX;
    private float offsetY;
    private int playerX = -1;
    private int playerY = -1;
    private int emptyPreviewX = -1;
    private int emptyPreviewY = -1;
    private bool isPlaytestMode;
    private Coroutine warningCoroutine;
    private LevelEditorPlaytestController playtestController;

    private void Start()
    {
        offsetX = (width - 1) * gridSpacing / 2f;
        offsetY = (height - 1) * gridSpacing / 2f;
        tiles = new TileType[width, height];
        placedObjects = new GameObject[width, height];
        tileParent = new GameObject("LevelEditor_Tiles").transform;
        previewParent = new GameObject("LevelEditor_Preview").transform;

        BindToolbarButtons();
        SetBackButtonVisible(false);
        SetWarningVisible(false);
        InitializeMap();
        UpdatePreviewObject();
    }

    private void Update()
    {
        if (isPlaytestMode) return;

        if (!TryGetMouseGridPosition(out int gridX, out int gridY) || EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            RestoreEmptyPreviewTile();
            if (previewObject != null) previewObject.SetActive(false);
            return;
        }

        if (currentMode == TileType.Empty)
        {
            if (previewObject != null) previewObject.SetActive(false);
            UpdateEmptyPreviewTile(gridX, gridY);
            if (Input.GetMouseButtonDown(0))
            {
                RestoreEmptyPreviewTile();
                PlaceTile(gridX, gridY, currentMode);
            }

            return;
        }

        RestoreEmptyPreviewTile();
        if (previewObject != null)
        {
            previewObject.SetActive(true);
            previewObject.transform.position = GetWorldPosition(gridX, gridY);
        }

        if (Input.GetMouseButtonDown(0))
        {
            PlaceTile(gridX, gridY, currentMode);
        }
    }

    public void SelectWallMode() => SelectMode(TileType.Wall);
    public void SelectEmptyMode() => SelectMode(TileType.Empty);
    public void SelectTargetMode() => SelectMode(TileType.Target);
    public void SelectBoxMode() => SelectMode(TileType.Box);
    public void SelectPlayerMode() => SelectMode(TileType.Player);

    private void BindToolbarButtons()
    {
        BindButton(wallButton, TileType.Wall);
        BindButton(emptyButton, TileType.Empty);
        BindButton(targetButton, TileType.Target);
        BindButton(boxButton, TileType.Box);
        BindButton(playerButton, TileType.Player);

        if (playtestButton != null)
        {
            playtestButton.onClick.RemoveAllListeners();
            playtestButton.onClick.AddListener(EnterPlaytestMode);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(ExitPlaytestMode);
        }
    }

    private void BindButton(Button button, TileType mode)
    {
        if (button == null) return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => SelectMode(mode));
    }

    private void SelectMode(TileType mode)
    {
        RestoreEmptyPreviewTile();
        currentMode = mode;
        UpdatePreviewObject();
    }

    private void InitializeMap()
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                PlaceTile(x, y, TileType.Wall);
            }
        }
    }

    private void PlaceTile(int x, int y, TileType type)
    {
        if (placedObjects[x, y] != null)
        {
            Destroy(placedObjects[x, y]);
            placedObjects[x, y] = null;
        }

        if (playerX == x && playerY == y && type != TileType.Player)
        {
            playerX = -1;
            playerY = -1;
        }

        if (type == TileType.Player)
        {
            ClearExistingPlayer();
            playerX = x;
            playerY = y;
        }

        tiles[x, y] = type;
        SpawnTileObject(x, y, type);
    }

    private void EnterPlaytestMode()
    {
        if (!HasMatchingTargetAndBoxCount())
        {
            ShowWarning("目标点数量和箱子数量不一致！");
            return;
        }

        if (playerX < 0 || playerY < 0)
        {
            ShowWarning("进入试玩模式前需要先放置玩家。");
            return;
        }

        RestoreEmptyPreviewTile();
        if (previewObject != null) previewObject.SetActive(false);

        editorSnapshot = CloneTiles(tiles);
        playtestController = gameObject.AddComponent<LevelEditorPlaytestController>();
        playtestController.Initialize(width, height, gridSpacing, tiles, placedObjects, playerX, playerY);

        isPlaytestMode = true;
        SetEditButtonsInteractable(false);
        SetBackButtonVisible(true);
        SetButtonInteractable(playtestButton, false);
    }

    private void ExitPlaytestMode()
    {
        if (!isPlaytestMode) return;

        if (playtestController != null)
        {
            Destroy(playtestController);
            playtestController = null;
        }

        if (editorSnapshot != null)
        {
            RebuildMap(editorSnapshot);
            editorSnapshot = null;
        }

        isPlaytestMode = false;
        SetEditButtonsInteractable(true);
        SetBackButtonVisible(false);
        SetButtonInteractable(playtestButton, true);
        UpdatePreviewObject();
    }

    private bool HasMatchingTargetAndBoxCount()
    {
        int targets = 0;
        int boxes = 0;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                TileType tile = tiles[x, y];
                if (tile == TileType.Target || tile == TileType.BoxOnTarget || tile == TileType.PlayerOnTarget) targets++;
                if (tile == TileType.Box || tile == TileType.BoxOnTarget) boxes++;
            }
        }

        return targets == boxes;
    }

    private void ShowWarning(string message)
    {
        if (warningText == null) return;
        warningText.text = message;
        SetWarningVisible(true);
        if (warningCoroutine != null) StopCoroutine(warningCoroutine);
        warningCoroutine = StartCoroutine(HideWarningAfterDelay(2f));
    }

    private IEnumerator HideWarningAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SetWarningVisible(false);
        warningCoroutine = null;
    }

    private void SetEditButtonsInteractable(bool interactable)
    {
        SetButtonInteractable(wallButton, interactable);
        SetButtonInteractable(emptyButton, interactable);
        SetButtonInteractable(targetButton, interactable);
        SetButtonInteractable(boxButton, interactable);
        SetButtonInteractable(playerButton, interactable);
    }

    private void SetButtonInteractable(Button button, bool interactable)
    {
        if (button != null) button.interactable = interactable;
    }

    private void SetBackButtonVisible(bool visible)
    {
        if (backButton != null) backButton.gameObject.SetActive(visible);
    }

    private void SetWarningVisible(bool visible)
    {
        if (warningText != null) warningText.gameObject.SetActive(visible);
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

    private void RebuildMap(TileType[,] source)
    {
        ClearMapObjects();
        tiles = CloneTiles(source);
        placedObjects = new GameObject[width, height];
        playerX = -1;
        playerY = -1;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                SpawnTileObject(x, y, tiles[x, y]);
            }
        }
    }

    private void ClearMapObjects()
    {
        for (int i = tileParent.childCount - 1; i >= 0; i--)
        {
            Destroy(tileParent.GetChild(i).gameObject);
        }
    }

    private void SpawnTileObject(int x, int y, TileType type)
    {
        if (type == TileType.Empty) return;
        if (type == TileType.BoxOnTarget)
        {
            InstantiateEditorObject(TileType.Target, x, y);
            placedObjects[x, y] = InstantiateEditorObject(TileType.Box, x, y);
            return;
        }

        if (type == TileType.PlayerOnTarget)
        {
            InstantiateEditorObject(TileType.Target, x, y);
            placedObjects[x, y] = InstantiateEditorObject(TileType.Player, x, y);
            playerX = x;
            playerY = y;
            return;
        }

        placedObjects[x, y] = InstantiateEditorObject(type, x, y);
        if (type == TileType.Player)
        {
            playerX = x;
            playerY = y;
        }
    }

    private GameObject InstantiateEditorObject(TileType type, int x, int y)
    {
        GameObject prefab = GetPrefab(type);
        GameObject obj = Instantiate(prefab, GetWorldPosition(x, y), Quaternion.identity, tileParent);
        PrepareEditorObject(obj);
        return obj;
    }

    private void PrepareEditorObject(GameObject editorObject)
    {
        foreach (MonoBehaviour behaviour in editorObject.GetComponentsInChildren<MonoBehaviour>()) behaviour.enabled = false;
        foreach (Collider2D collider in editorObject.GetComponentsInChildren<Collider2D>()) collider.enabled = false;
        foreach (Rigidbody2D body in editorObject.GetComponentsInChildren<Rigidbody2D>()) body.simulated = false;
    }

    private void ClearExistingPlayer()
    {
        if (playerX < 0 || playerY < 0) return;
        if (placedObjects[playerX, playerY] != null)
        {
            Destroy(placedObjects[playerX, playerY]);
            placedObjects[playerX, playerY] = null;
        }

        tiles[playerX, playerY] = TileType.Empty;
    }

    private void UpdateEmptyPreviewTile(int x, int y)
    {
        if (emptyPreviewX == x && emptyPreviewY == y) return;
        RestoreEmptyPreviewTile();
        GameObject placedObject = placedObjects[x, y];
        if (placedObject == null) return;
        emptyPreviewX = x;
        emptyPreviewY = y;
        SetObjectAlpha(placedObject, 0.35f);
    }

    private void RestoreEmptyPreviewTile()
    {
        if (emptyPreviewX < 0 || emptyPreviewY < 0) return;
        GameObject placedObject = placedObjects[emptyPreviewX, emptyPreviewY];
        if (placedObject != null) SetObjectAlpha(placedObject, 1f);
        emptyPreviewX = -1;
        emptyPreviewY = -1;
    }

    private void SetObjectAlpha(GameObject target, float alpha)
    {
        foreach (SpriteRenderer spriteRenderer in target.GetComponentsInChildren<SpriteRenderer>())
        {
            Color color = spriteRenderer.color;
            color.a = alpha;
            spriteRenderer.color = color;
        }
    }

    private void UpdatePreviewObject()
    {
        if (previewObject != null) Destroy(previewObject);
        TileType previewType = currentMode == TileType.Empty ? TileType.Wall : currentMode;
        previewObject = InstantiateEditorObject(previewType, 0, 0);
        previewObject.name = "PlacementPreview";
        SetObjectAlpha(previewObject, currentMode == TileType.Empty ? 0.25f : 0.5f);
        previewObject.transform.SetParent(previewParent);
        previewObject.SetActive(false);
    }

    private bool TryGetMouseGridPosition(out int gridX, out int gridY)
    {
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        gridX = Mathf.RoundToInt((mouseWorld.x + offsetX) / gridSpacing);
        gridY = Mathf.RoundToInt((mouseWorld.y + offsetY) / gridSpacing);
        return gridX >= 0 && gridX < width && gridY >= 0 && gridY < height;
    }

    private Vector3 GetWorldPosition(int x, int y)
    {
        return new Vector3(x * gridSpacing - offsetX, y * gridSpacing - offsetY, 0f);
    }

    private GameObject GetPrefab(TileType type)
    {
        switch (type)
        {
            case TileType.Wall:
                return wallPrefab;
            case TileType.Target:
                return targetPrefab;
            case TileType.Box:
                return boxPrefab;
            case TileType.Player:
                return playerPrefab;
            default:
                return null;
        }
    }

    public void BackToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
