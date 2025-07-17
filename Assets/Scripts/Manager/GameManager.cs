using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;

public enum GameState
{
    Running,
    Pause
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private enum InputMoveType
    {
        None,
        Up,
        Down,
        Left,
        Right
    }

    [Header("方块预制体")]
    [SerializeField] private Tile _tilePrefab;
    [SerializeField] private TileState[] _tileStates;

    [Header("绑定组件")]
    [SerializeField] private TextMeshProUGUI _scoreText;
    [SerializeField] private TileGrid _tileGrid;
    [SerializeField] private Transform _tileParent;
    [SerializeField] private GameObject _gameOverPanel;

    [Header("游戏参数")]
    [SerializeField] private int _firstSpawnCount = 2;
    [SerializeField] private float _moveDuration = 0.3f;
    [SerializeField] private float _animTime = 0.1f;
    [SerializeField] private float _maxMoveDuration = 0.5f;

    [Header("触摸设置")]
    [SerializeField] private float _swipeThresholdPercentage = 0.05f; //5% 的屏幕宽度作为最小滑动距离


    [Header("游戏数据")]
    public GameState gameState = GameState.Pause;
    [SerializeField] private List<Tile> _tiles; //游戏中现有的所有方块
    [SerializeField] private bool _isMoving = false;

    private ObjectPool<Tile> _tilePool; //方块对象池
    public int score = 0;
    private StringBuilder _scoreBuilder = new();
    private float _maxMoveDurationTemp = 0;
    private bool _shouldIgnoreCurrentTouch = false; //是否忽略当前触摸
    private float _minSwipeDistance; //动态计算的最小滑动距离
    private Vector2 _swipeStartPos = Vector2.zero; //滑动开始时的位置


    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        _minSwipeDistance = Screen.width * _swipeThresholdPercentage; //根据屏幕宽度计算最小滑动距离

    }

    void Start()
    {
        InitTilePool();
        GameStart();
    }

    void Update()
    {
        if (gameState != GameState.Running) return;

        HandleKeyboardInput();
        HandleTouchInput();
    }

    #region 处理输入

    private void HandleKeyboardInput()
    {
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL
        switch (CheckInput())
        {
            case InputMoveType.Up:
                MoveAllTiles(new Vector2Int(-1, 0));
                break;
            case InputMoveType.Down:
                MoveAllTiles(new Vector2Int(1, 0));
                break;
            case InputMoveType.Left:
                MoveAllTiles(new Vector2Int(0, -1));
                break;
            case InputMoveType.Right:
                MoveAllTiles(new Vector2Int(0, 1));
                break;
            default:
                break;
        }
#endif
    }

    private InputMoveType CheckInput()
    {
        if (!Input.anyKeyDown)
            return InputMoveType.None;

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) return InputMoveType.Up;
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) return InputMoveType.Down;
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) return InputMoveType.Left;
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) return InputMoveType.Right;


        return InputMoveType.None;
    }

    private void HandleTouchInput()
    {
#if UNITY_IOS || UNITY_ANDROID || UNITY_WEBGL
        if (gameState != GameState.Running)
            return;

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (_shouldIgnoreCurrentTouch)
            {
                //忽略直到当前触摸结束
                if (touch.phase == TouchPhase.Ended)
                    _shouldIgnoreCurrentTouch = false;
                return;
            }

            //记录滑动开始/移动时的位置
            if (touch.phase == TouchPhase.Began)
            {
                _swipeStartPos = touch.position;
            }
            else if (touch.phase == TouchPhase.Ended)
            {
                Vector2 endPos = touch.position;
                Vector2 delta = endPos - _swipeStartPos;

                if (delta.magnitude > _minSwipeDistance)
                {
                    float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg; //计算滑动角度
                    if (Mathf.Abs(angle) < 45 || Mathf.Abs(angle) > 135)
                    {
                        //水平滑动
                        if (delta.x > 0) MoveAllTiles(new Vector2Int(0, 1));  // 右滑
                        else MoveAllTiles(new Vector2Int(0, -1)); // 左滑
                    }
                    else
                    {
                        //垂直滑动
                        if (delta.y > 0) MoveAllTiles(new Vector2Int(-1, 0)); // 上滑
                        else MoveAllTiles(new Vector2Int(1, 0));    // 下滑
                    }
                }
            }
        }
#endif
    }

    #endregion

    #region Game状态

    public void GameStart()
    {
        gameState = GameState.Running;

        for (int i = 0; i < _firstSpawnCount; i++)
        {
            CreateTile();
        }
    }

    public void GamePause()
    {
        gameState = GameState.Pause;
        _swipeStartPos = Vector2.zero;
        _shouldIgnoreCurrentTouch = true;
    }

    public void GameResume()
    {
        gameState = GameState.Running;
        _swipeStartPos = Vector2.zero;
        _shouldIgnoreCurrentTouch = true;
    }

    public void ResetGame()
    {
        ClearGame();
        GameStart();
    }
    public void ClearGame()
    {
        foreach (Tile tile in _tiles)
        {
            Destroy(tile.gameObject);
        }
        _tiles.Clear();
        score = 0;

        _tileGrid.ClearCellTiles();
    }
    private bool IsGameOver()
    {
        //TODO:游戏结束逻辑

        //如果还有空格子
        if (_tileGrid.GetRandomEmptyCell() != null)
        {
            return false;
        }

        //2. 检查是否有可以合并的相邻方块
        for (int x = 0; x < TileGrid.TILE_ROW_COUNT; x++)
        {
            for (int y = 0; y < TileGrid.TILE_COLUMN_COUNT; y++)
            {
                TileCell currentCell = _tileGrid.Cells[x, y];
                int currentNumber = currentCell.Tile.Number;

                //检查上方方块
                if (x > 0 && _tileGrid.Cells[x - 1, y].Tile.Number == currentNumber)
                    return false;

                //检查下方方块
                if (x < TileGrid.TILE_ROW_COUNT - 1 && _tileGrid.Cells[x + 1, y].Tile.Number == currentNumber)
                    return false;
                //检查左方方块
                if (y > 0 && _tileGrid.Cells[x, y - 1].Tile.Number == currentNumber)
                    return false;
                //检查右方方块
                if (y < TileGrid.TILE_COLUMN_COUNT - 1 && _tileGrid.Cells[x, y + 1].Tile.Number == currentNumber)
                    return false;
            }
        }

        //3. 如果没有空格子，且没有可合并的相邻方块，则游戏结束
        _gameOverPanel.SetActive(true);

        return true; //没有空格子且没有可合并的相邻方块
    }

    #endregion

    #region 方块对象池

    private void InitTilePool()
    {
        _tilePool = new ObjectPool<Tile>(createFunc, actionOnGet, actionOnRelease, actionOnDestroy);
    }

    private Tile createFunc()
    {
        return Instantiate(_tilePrefab, _tileParent);
    }

    private void actionOnGet(Tile tile)
    {
        TileCell emptyCell = _tileGrid.GetRandomEmptyCell(); //获取空格子

        //如果没有空格子，则不能生成
        if (emptyCell == null)
        {
            _tilePool.Release(tile); //放回对象池
            return;
        }

        tile.gameObject.SetActive(true);

        int index = UnityEngine.Random.Range(0, 2); //随机选择方块样式

        tile.Spawn(emptyCell); //移动方块到空格子
        tile.SetState(_tileStates[index], 2 * (index + 1)); //设置方块样式

        _tiles.Add(tile); //添加到游戏集合中

        //更新游戏分数
        score += tile.Number;
        UpdateScore(score);
    }

    private void actionOnRelease(Tile tile)
    {
        //确保从所有数据结构中移除
        if (_tiles.Contains(tile))
        {
            _tiles.Remove(tile);
        }
        tile.transform.SetParent(_tileParent);
        tile.gameObject.SetActive(false);
        tile.ReSetState();
    }

    private void actionOnDestroy(Tile tile)
    {
        _tiles.Remove(tile);
        Destroy(tile.gameObject);
    }

    #endregion

    #region 方块移动

    /// <summary>
    /// 移动所有方块
    /// </summary>
    /// <param name="direction"></param>
    private void MoveAllTiles(Vector2Int direction)
    {
        if (IsGameOver())
        {
            return;
        }

        if (_isMoving) return;
        _isMoving = true;
        //bool moved = false;

        //根据移动方向确定遍历顺序
        int startX, endX, stepX;
        int startY, endY, stepY;

        //设置X方向的遍历顺序
        if (direction.x > 0) //向下移动
        {
            startX = TileGrid.TILE_ROW_COUNT - 1;
            endX = -1;
            stepX = -1;
        }
        else if (direction.x < 0) //向上移动
        {
            startX = 0;
            endX = TileGrid.TILE_ROW_COUNT;
            stepX = 1;
        }
        else //水平移动
        {
            startX = 0;
            endX = TileGrid.TILE_ROW_COUNT;
            stepX = 1;
        }

        //设置Y方向的遍历顺序
        if (direction.y > 0) //向右移动
        {
            startY = TileGrid.TILE_COLUMN_COUNT - 1;
            endY = -1;
            stepY = -1;
        }
        else if (direction.y < 0) //向左移动
        {
            startY = 0;
            endY = TileGrid.TILE_COLUMN_COUNT;
            stepY = 1;
        }
        else //垂直移动
        {
            startY = 0;
            endY = TileGrid.TILE_COLUMN_COUNT;
            stepY = 1;
        }

        _maxMoveDuration = 0; //重置最大移动时间
        _maxMoveDurationTemp = 0; //重置最大移动判断时间
        //遍历所有格子
        for (int x = startX; x != endX; x += stepX)
        {
            for (int y = startY; y != endY; y += stepY)
            {
                TileCell cell = _tileGrid.Cells[x, y];

                if (cell.Occupied)
                {
                    MoveTilePosition(cell, direction, _moveDuration);
                    _maxMoveDuration = Mathf.Max(_maxMoveDuration, _maxMoveDurationTemp); //记录最大移动时间
                }
            }
        }
        //移动完成后生成新方块
        StartCoroutine(DealyOverMove(_maxMoveDuration));
    }

    /// <summary>
    /// 移动单个方块
    /// </summary>
    /// <param name="tile"></param>
    /// <param name="direction"></param>
    /// <returns></returns>
    private Vector2Int MoveTilePosition(TileCell cell, Vector2Int direction, float totalDuration)
    {
        //获取相邻的格子，只获取网格中的格子，不包括边界，避免越界
        int x_index = Mathf.Clamp(cell.Coordinates.x + direction.x, 0, TileGrid.TILE_ROW_COUNT - 1);
        int y_index = Mathf.Clamp(cell.Coordinates.y + direction.y, 0, TileGrid.TILE_COLUMN_COUNT - 1);

        TileCell nextCell = _tileGrid.Cells[x_index, y_index]; //相邻下一个位置

        //计算已用时间和剩余时间
        float remainingTime = Mathf.Max(0.05f, totalDuration); // 确保不为负

        //如果下一个位置已经有瓦片，，且不能合并，则不能移动
        if (nextCell.Occupied && !CanMerge(cell, nextCell))
            return cell.Coordinates;
        //如果下一个位置已经有瓦片，且可以合并，则合并
        else if (nextCell.Occupied && CanMerge(cell, nextCell))
        {
            _maxMoveDurationTemp = remainingTime;
            Merge(cell, nextCell, remainingTime);
            return nextCell.Coordinates;
        }
        //如果下一个位置为空，则移动到空位置
        else
        {
            remainingTime += _moveDuration;
            _maxMoveDurationTemp = remainingTime;

            cell.Tile.DoSpawn(nextCell, remainingTime);
            return MoveTilePosition(nextCell, direction, remainingTime);
        }
    }

    #endregion

    #region 合并
    private bool CanMerge(TileCell needMoveCell, TileCell targetCell)
    {
        if (needMoveCell == null || targetCell == null || needMoveCell == targetCell)
            return false;

        if (!needMoveCell.Occupied || !targetCell.Occupied || needMoveCell.Tile == targetCell.Tile)
            return false;

        return needMoveCell.Tile.Number == targetCell.Tile.Number && !needMoveCell.Tile.IsMerged && !targetCell.Tile.IsMerged;
    }

    private void Merge(TileCell needMoveCell, TileCell targetCell, float duration)
    {
        Tile needMoveTile = needMoveCell.Tile; //获取需要移动的方块
        Tile targetTile = targetCell.Tile; //获取目标方块

        //提前标记为已合并，防止重复处理
        targetTile.IsMerged = true;

        //从网格和列表中移除
        _tiles.Remove(needMoveTile);
        //Debug.Log("将要移除方块,将格子"+ needMoveCell + "设为空");
        needMoveCell.Tile = null;

        //设置父对象以便跟随移动
        needMoveTile.transform.SetParent(targetTile.transform);

        float moveTime = Mathf.Max(duration, 0.1f);

        Sequence mergeSequence = DOTween.Sequence();
        mergeSequence.Append(needMoveTile.transform.DOLocalMove(Vector3.zero, moveTime))
        .AppendCallback(() =>
        {
            //确保对象存在再释放
            if (needMoveTile != null)
            {
                score -= needMoveTile.Number * 2;//减去分数
                //Destroy(needMoveTile.gameObject);
                _tilePool.Release(needMoveTile);
            }

            targetTile.PlaySound();
            targetTile.SetState(GetNextState(targetTile.Number), targetTile.Number * 2);
            
            score += targetTile.Number;
            UpdateScore(score); //更新分数
        })
        .Append(targetTile.transform.DOPunchScale(Vector3.one * 0.1f, _animTime));
    }

    #endregion

    private void UpdateScore(int score)
    {
        _scoreBuilder.Clear();
        _scoreBuilder.Append("Socre: ");
        _scoreBuilder.Append(score);

        _scoreText.text = _scoreBuilder.ToString();
    }

    private TileState GetNextState(int number)
    {
        //查找下一个状态的瓦片样式
        for (int i = 0; i < _tileStates.Length - 1; i++)
        {
            if (_tileStates[i].number == number)
            {
                return _tileStates[i + 1];
            }
        }

        //如果没有找到更大的状态，返回最后一个
        return _tileStates[_tileStates.Length - 1];
    }

    /// <summary>
    /// 创建瓦片
    /// </summary>
    private void CreateTile()
    {
        _tilePool.Get();
    }

    private IEnumerator DealyOverMove(float delayTime)
    {
        yield return new WaitForSeconds(delayTime);
        CreateTile();
        _isMoving = false;

        //重置所有方块的合并状态
        foreach (Tile tile in _tiles)
        {
            tile.IsMerged = false;
        }
    }
}
