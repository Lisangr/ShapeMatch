using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("Audio")]
    [SerializeField] private AudioSource audioSFX;
    
    private void Awake() => Instance = this;
    
    public void OnGameOver(bool isWin)
    {
        // Воспроизводим соответствующий звук
        if (audioSFX != null)
        {
            AudioClip clip = null;
            if (isWin)
            {
                clip = Resources.Load<AudioClip>("Audio/Win");

            }
            else
            {
                clip = Resources.Load<AudioClip>("Audio/GameOver");

            }
            
            if (clip != null)
            {
                audioSFX.PlayOneShot(clip);
            }
        }
        
        // Показываем соответствующую панель
        if (isWin)
        {
            // Помечаем уровень как пройденный
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.CompleteCurrentLevel();
            }
            
            UIManager.Instance.ShowWinPanel();
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
