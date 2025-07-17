using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get;private set; }

    [SerializeField] private AudioSource _soundSource;
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    public void PlaySound()
    {
        _soundSource.Play();
    }
}
