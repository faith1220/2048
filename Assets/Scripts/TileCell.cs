using UnityEngine;

public class TileCell : MonoBehaviour
{
    public Vector2Int Coordinates; //格子的坐标，行数和列数
    public Tile Tile;

    public bool Occupied => Tile != null; //是否被占用
}
