using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Tile : MonoBehaviour
{
    public TileState State { get; private set; }

    public TileCell Cell;
    
    public bool IsMerged { get; set; }

    public int Number { get; private set; }

    private Image _backgroundImage;
    private TextMeshProUGUI _numberText;
    private Sequence _mergeSequence;
    private AudioSource _audioSource;


    private void Awake()
    {
        _backgroundImage = GetComponent<Image>();
        _numberText = GetComponentInChildren<TextMeshProUGUI>();
        _audioSource = GetComponent<AudioSource>();
    }

    private void OnDestroy()
    {
        transform.DOKill();
        _mergeSequence.Kill();
    }

    public void SetState(TileState state, int number)
    {
        State = state;
        Number = number;

        _backgroundImage.color =state.backgroundColor;
        _numberText.color = state.textColor;
        _numberText.text = number.ToString();
    }

    public void Spawn(TileCell cell,UnityAction onSpawned = null)
    {
        if(Cell!= null)
        {
            Cell.Tile = null;
        }
        Cell = cell;
        Cell.Tile = this;

        //初始状态设置为很小
        transform.localScale = Vector3.zero;
        transform.position = cell.transform.position;

        //添加缩放动画
        transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack).OnComplete(() =>
        {
            onSpawned?.Invoke();
        });
    }

    public void DoMove(TileCell cell, float duration)
    {
        if (Cell != null)
        {
            Cell.Tile = null;
        }
        Cell = cell;
        Cell.Tile = this;

        _mergeSequence.Kill(); //先清除之前的动画
        _mergeSequence = DOTween.Sequence(); //重新创建
        //添加移动动画和弹性效果
        _mergeSequence.Append(transform.DOMove(cell.transform.position, duration).SetEase(Ease.OutQuad))
            .OnComplete(() =>
             {
                 transform.localScale = Vector3.one;
             });
    }

    public void PlaySound()
    {
        _audioSource.Play();
    }

    public void ReSetState()
    {
        Cell = null;
        State = null;
        Number = 0;
        _backgroundImage.color = Color.white;
        _numberText.color = Color.black;
        _numberText.text = "";

        transform.DOKill();
        _mergeSequence.Kill();
        IsMerged = false;
    }
}
