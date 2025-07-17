using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SettingPanel : MonoBehaviour
{
    [Header("音乐组件")]
    [SerializeField] private AudioMixerGroup _musicGroup;
    [SerializeField] private Slider _musicSlider;
    [SerializeField] private GameObject _openMusicImage;
    [SerializeField] private GameObject _closeMusicImage;
    private const string MUSIC_VOLUME_PARAM = "MusicVolume";

    [Header("音效组件")]
    [SerializeField] private AudioMixerGroup _soundGroup;
    [SerializeField] private Slider _soundSlider;
    [SerializeField] private GameObject _openSoundImage;
    [SerializeField] private GameObject _closeSoundImage;
    private const string SOUND_VOLUME_PARAM = "SoundVolume";

    [Header("音量设置")]
    [SerializeField] private float _minVolumeDB = -80f;
    [SerializeField] private float _maxVolumeDB = 0f;
    [SerializeField] private float _volumeMultiplier = 20f; //线性值到对数分贝值的倍率

    private void Awake()
    {
        InitializeVolumeControl(_musicGroup, _musicSlider, MUSIC_VOLUME_PARAM, _openMusicImage, _closeMusicImage);
        InitializeVolumeControl(_soundGroup, _soundSlider, SOUND_VOLUME_PARAM, _openSoundImage, _closeSoundImage);
    }

    private void OnEnable() => GameManager.Instance.GamePause();

    private void OnDisable() => GameManager.Instance.GameResume();

    private void InitializeVolumeControl(AudioMixerGroup group, Slider slider, string paramName,
                                      GameObject openIcon, GameObject closeIcon)
    {
        //添加音量改变监听
        slider.onValueChanged.AddListener(value => {
            SetVolume(group.audioMixer, paramName, value);
            UpdateVolumeIcon(value > 0, openIcon, closeIcon);
        });
    }

    private void SetVolume(AudioMixer mixer, string paramName, float linearValue)
    {
        //将线性值转换为对数分贝值
        float volumeDB;
        if (linearValue <= 0.0001f) //接近0的值视为静音
        {
            volumeDB = _minVolumeDB;
        }
        else
        {
            volumeDB = Mathf.Log10(linearValue) * _volumeMultiplier;
            volumeDB = Mathf.Clamp(volumeDB, _minVolumeDB, _maxVolumeDB);
        }

        mixer.SetFloat(paramName, volumeDB);
    }

    private void UpdateVolumeIcon(bool isOn, GameObject openIcon, GameObject closeIcon)
    {
        openIcon.SetActive(isOn);
        closeIcon.SetActive(!isOn);
    }
}
