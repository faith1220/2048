using System.Collections.Generic;
using UnityEngine;

public class TileGrid : MonoBehaviour
{
    public TileCell[,] Cells { get; set; }

    public const int TILE_ROW_COUNT = 4; //行数
    public const int TILE_COLUMN_COUNT = 4; //列数

    public int Size => TILE_ROW_COUNT * TILE_COLUMN_COUNT;

    private void Awake()
    {
        Cells = new TileCell[TILE_ROW_COUNT, TILE_COLUMN_COUNT]; //初始化cells数组

        //遍历所有子物体，并将其添加到cells数组中
        for (int x = 0; x < TILE_ROW_COUNT; x++)
        {
            for (int y = 0; y < TILE_COLUMN_COUNT; y++)
            {
                //获取子物体
                Cells[x, y] = transform.GetChild(x * TILE_ROW_COUNT + y).GetComponent<TileCell>();
                Cells[x, y].Coordinates = new Vector2Int(x, y);//设置坐标
            }
        }
    }

    /// <summary>
    /// 获取随机空白格子
    /// </summary>
    /// <returns></returns>
    public TileCell GetRandomEmptyCell()
    {
        //首先收集所有空白格子的列表
        List<TileCell> emptyCells = new List<TileCell>();

        for (int x = 0; x < TILE_ROW_COUNT; x++)
        {
            for (int y = 0; y < TILE_COLUMN_COUNT; y++)
            {
                if (!Cells[x, y].Occupied)
                {
                    emptyCells.Add(Cells[x, y]);
                }
            }
        }

        //如果没有空白格子，返回null
        if (emptyCells.Count == 0)
        {
            return null;
        }

        //从空白格子中随机选择一个
        int randomIndex = Random.Range(0, emptyCells.Count);
        return emptyCells[randomIndex];
    }

    /// <summary>
    /// 清理网格中的所有格子
    /// </summary>
    public void ClearCellTiles()
    {
        foreach(TileCell cell in Cells)
        {
            cell.Tile = null;
        }
    }
}
