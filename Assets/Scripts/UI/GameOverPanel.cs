using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOverPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _scoreText;
    [SerializeField] private Button _retryButton;
    [SerializeField] private AudioSource _audioSource;

    [SerializeField] private Button _settingButton;
    [SerializeField] private Color _settingDisableColor;

    private Color _settingOriginalColor;
    void Awake()
    {
        _retryButton.onClick.AddListener(() =>
        {
            gameObject.SetActive(!gameObject.activeSelf);
        });
        _audioSource = GetComponent<AudioSource>();
        _settingOriginalColor = _settingButton.GetComponent<Image>().color;
    }

    private void OnEnable()
    {
        GameManager.Instance.GamePause();

        _settingButton.interactable = false;
        _settingButton.GetComponent<Image>().color = _settingDisableColor;

        _audioSource.Play();
        transform.localScale = Vector3.zero;
        transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack);
        _scoreText.text = "Your total score is: " + GameManager.Instance.score.ToString();
    }

    private void OnDisable()
    {
        transform.DOScale(Vector3.zero, 0.3f);
        _settingButton.interactable = true;
        _settingButton.GetComponent<Image>().color = _settingOriginalColor;
    }

    public void UpdateScore(int score)
    {
        _scoreText.text = "Your total score is: " + score;
    }

}
