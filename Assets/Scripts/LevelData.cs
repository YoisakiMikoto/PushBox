using UnityEngine;

[CreateAssetMenu(fileName = "NewLevelData", menuName = "PushBox/Level Data")]
public class LevelData : ScriptableObject
{
    public int width;
    public int height;
    public TileType[] rowData;

    public TileType GetTile(int x, int y)
    {
        int index = y * width + x;
        if (index >= 0 && index < rowData.Length)
        {
            return rowData[index];
        }

        return TileType.Wall;
    }
}

public enum TileType
{
    Empty,
    Wall,
    Target,
    Box,
    BoxOnTarget,
    Player,
    PlayerOnTarget
}
