using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("Audio")]
    [SerializeField] private AudioSource audioSFX;
    
    private void Awake() => Instance = this;
    
    public void OnGameOver(bool win)
    {
        // Воспроизводим соответствующий звук
        if (audioSFX != null)
        {
            AudioClip clip = null;
            if (win)
            {
                clip = Resources.Load<AudioClip>("Audio/Win");
                Debug.Log("Playing Win sound");
            }
            else
            {
                clip = Resources.Load<AudioClip>("Audio/GameOver");
                Debug.Log("Playing GameOver sound");
            }
            
            if (clip != null)
            {
                audioSFX.PlayOneShot(clip);
            }
        }
        
        // Показываем соответствующую панель
        if (win)
        {
            UIManager.Instance.ShowWin();
        }
        else
        {
            UIManager.Instance.ShowLose();
        }
    }
    
    public void StopAllSounds()
    {
        if (audioSFX != null)
        {
            audioSFX.Stop();
        }
    }
}
