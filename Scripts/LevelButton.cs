using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class LevelButton : MonoBehaviour
{
    [Header("Level Settings")]
    [SerializeField] private int levelNumber = 1;
    [SerializeField] private string levelSceneName = "MainLogic";
    
    [Header("Visual States")]
    [SerializeField] private Sprite waitingSprite; // Спрайт ожидания (нолик)
    [SerializeField] private Sprite completedSprite; // Спрайт выполнения (галочка)
    [SerializeField] private Image statusImage; // Image компонент для отображения состояния
    
    [Header("Level Info")]
    [SerializeField] private Text levelNumberText; // Текст с номером уровня (опционально)
    
    [Header("Audio")]
    [SerializeField] private AudioSource audioSFX;
    
    private Button levelButton;
    private bool isCompleted = false;
    private bool isLocked = false;
    
    private void Awake()
    {
        levelButton = GetComponent<Button>();
        if (levelButton == null)
            levelButton = gameObject.AddComponent<Button>();
    }
    
    private void Start()
    {
        // Настройка кнопки
        levelButton.onClick.AddListener(OnLevelButtonClick);
        
        // Загрузка состояния уровня
        LoadLevelState();
        
        // Проверка блокировки уровня
        CheckLevelLock();
        
        // Обновление визуального состояния
        UpdateVisualState();
        
        // Обновление текста номера уровня
        if (levelNumberText != null)
            levelNumberText.text = levelNumber.ToString();
    }
    
    private void OnLevelButtonClick()
    {
        // Воспроизводим звук клика
        PlayClickSound();
        
        // Сохраняем номер выбранного уровня
        PlayerPrefs.SetInt("SelectedLevel", levelNumber);
        
        // Определяем и сохраняем название спрайта фона в зависимости от номера уровня
        string backgroundSpriteName;
        if (levelNumber >= 1 && levelNumber <= 12)
        {
            backgroundSpriteName = "forest";
        }
        else if (levelNumber >= 13 && levelNumber <= 23)
        {
            backgroundSpriteName = "boloto";
        }
        else if (levelNumber >= 24 && levelNumber <= 33)
        {
            backgroundSpriteName = "mountain";
        }
        else
        {
            backgroundSpriteName = "forest"; // По умолчанию
        }
        
        // Сохраняем название спрайта
        PlayerPrefs.SetString("BackgroundSprite", backgroundSpriteName);
        PlayerPrefs.Save();
        
        // Загружаем сцену с игрой
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadScene(levelSceneName);
        }
        else
        {
            SceneManager.LoadScene(levelSceneName);
        }
    }
    
    private void LoadLevelState()
    {
        // Загружаем состояние уровня из PlayerPrefs
        isCompleted = PlayerPrefs.GetInt($"Level_{levelNumber}_Completed", 0) == 1;
    }
    
    public void MarkAsCompleted()
    {
        isCompleted = true;
        
        // Сохраняем состояние в PlayerPrefs
        PlayerPrefs.SetInt($"Level_{levelNumber}_Completed", 1);
        PlayerPrefs.Save();
        
        // Обновляем визуальное состояние
        UpdateVisualState();
    }
    
    public void ResetLevel()
    {
        isCompleted = false;
        
        // Удаляем состояние из PlayerPrefs
        PlayerPrefs.DeleteKey($"Level_{levelNumber}_Completed");
        PlayerPrefs.Save();
        
        // Обновляем визуальное состояние
        UpdateVisualState();
    }
    
    private void CheckLevelLock()
    {
        // Первый уровень всегда доступен
        if (levelNumber <= 1)
        {
            isLocked = false;
            return;
        }
        
        // Проверяем, пройден ли предыдущий уровень
        bool previousLevelCompleted = PlayerPrefs.GetInt($"Level_{levelNumber - 1}_Completed", 0) == 1;
        isLocked = !previousLevelCompleted;
    }
    
    private void UpdateVisualState()
    {
        if (statusImage != null)
        {
            if (isLocked)
            {
                // Для заблокированного уровня показываем замок или затемненный спрайт
                statusImage.sprite = waitingSprite;
                statusImage.color = new Color(0.3f, 0.3f, 0.3f, 0.5f); // Затемняем
            }
            else
            {
                statusImage.sprite = isCompleted ? completedSprite : waitingSprite;
                statusImage.color = Color.white; // Возвращаем нормальный цвет
                
                // Анимация появления галочки
                if (isCompleted && completedSprite != null)
                {
                    statusImage.transform.localScale = Vector3.zero;
                    statusImage.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
                }
            }
        }
        
        // Настройка состояния кнопки
        if (levelButton != null)
        {
            levelButton.interactable = !isLocked;
            
            // Изменяем цвет кнопки в зависимости от состояния
            ColorBlock colors = levelButton.colors;
            if (isLocked)
            {
                colors.normalColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                colors.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.3f);
            }
            else if (isCompleted)
            {
                colors.normalColor = new Color(0.8f, 1f, 0.8f);
            }
            else
            {
                colors.normalColor = Color.white;
            }
            levelButton.colors = colors;
        }
        
        // Затемняем текст номера уровня для заблокированных уровней
        if (levelNumberText != null)
        {
            levelNumberText.color = isLocked ? new Color(0.3f, 0.3f, 0.3f, 0.5f) : Color.white;
        }
    }
    
    private void PlayClickSound()
    {
        if (audioSFX != null)
        {
            AudioClip clickClip = Resources.Load<AudioClip>("Audio/Click");
            if (clickClip != null)
            {
                audioSFX.PlayOneShot(clickClip);
            }
        }
    }
    
    // Публичные свойства для доступа
    public int LevelNumber => levelNumber;
    public bool IsCompleted => isCompleted;
    public bool IsLocked => isLocked;
    
    // Метод для программной настройки уровня
    public void SetupLevel(int number, Sprite waiting, Sprite completed)
    {
        levelNumber = number;
        waitingSprite = waiting;
        completedSprite = completed;
        
        LoadLevelState();
        UpdateVisualState();
        
        if (levelNumberText != null)
            levelNumberText.text = levelNumber.ToString();
    }
    
    public void RefreshLevelState()
    {
        LoadLevelState();
        CheckLevelLock();
        UpdateVisualState();
    }
} 