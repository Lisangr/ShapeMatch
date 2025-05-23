using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Collections;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }
    
    [Header("Level Completion")]
    [SerializeField] private List<LevelButton> allLevelButtons = new List<LevelButton>();
    
    [Header("Default Sprites")]
    [SerializeField] private Sprite defaultWaitingSprite;
    [SerializeField] private Sprite defaultCompletedSprite;
    
    [Header("Background Settings")]
    private Image backgroundImage;
    
    [Header("Audio")]
    [SerializeField] private AudioSource audioSFX;
    
    private int currentLevel = 1;
    
    private void Awake()
    {
        // Если это LevelManager в сцене выбора уровней
        if (Instance == null)
        {
            Instance = this;
            
            // Не уничтожаем при смене сцены только если мы в главном меню
            if (SceneManager.GetActiveScene().name != "MainLogic")
                DontDestroyOnLoad(gameObject);
        }
        else if (SceneManager.GetActiveScene().name != "MainLogic")
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // Получаем выбранный уровень
        currentLevel = PlayerPrefs.GetInt("SelectedLevel", 1);
        
        // Если мы в игровой сцене, проверяем на завершение уровня
        if (SceneManager.GetActiveScene().name == "MainLogic")
        {
            // Подписываемся на события игры
            if (GameManager.Instance != null)
            {
                // Здесь можно подписаться на событие завершения игры
            }
            
            // Ждем инициализации UIManager и получения backgroundImage
            StartCoroutine(WaitForUIManager());
        }
        else
        {
            // Если мы в меню выбора уровней, настраиваем кнопки
            SetupLevelButtons();
        }
    }

    private System.Collections.IEnumerator WaitForUIManager()
    {
        // Ждем, пока UIManager не будет инициализирован
        while (UIManager.Instance == null)
        {
            yield return null;
        }

        // Ждем еще один кадр, чтобы убедиться, что UIManager полностью инициализирован
        yield return null;

        // Получаем backgroundImage из UIManager через публичное свойство
        if (UIManager.Instance.BackgroundImage != null)
        {
            SetBackgroundImage(UIManager.Instance.BackgroundImage);
        }
    }

    public void UpdateBackgroundImage()
    {
        if (backgroundImage == null) return;

        // Получаем название спрайта из PlayerPrefs
        string spriteName = PlayerPrefs.GetString("BackgroundSprite", "forest");
        
        // Загружаем спрайт по имени
        Sprite newSprite = Resources.Load<Sprite>($"Image/{spriteName}");

        if (newSprite != null)
        {
            backgroundImage.sprite = newSprite;
            Debug.Log($"Background image updated to: {spriteName}");
        }
        else
        {
            Debug.LogWarning($"Could not load background sprite: {spriteName}");
        }
    }

    // Метод для установки фонового изображения
    public void SetBackgroundImage(Image image)
    {
        backgroundImage = image;
        UpdateBackgroundImage();
    }

    private void SetupLevelButtons()
    {
        // Автоматически находим все LevelButton компоненты
        if (allLevelButtons.Count == 0)
        {
            allLevelButtons.AddRange(FindObjectsOfType<LevelButton>());
        }
        
        // Настраиваем каждую кнопку
        for (int i = 0; i < allLevelButtons.Count; i++)
        {
            if (allLevelButtons[i] != null)
            {
                // Если кнопка не настроена, используем номер по порядку
                if (allLevelButtons[i].LevelNumber == 1 && i > 0)
                {
                    allLevelButtons[i].SetupLevel(i + 1, defaultWaitingSprite, defaultCompletedSprite);
                }
            }
        }
    }
    
    // Метод для пометки уровня как пройденного
    public void CompleteLevel(int levelNumber)
    {
        PlayerPrefs.SetInt($"Level_{levelNumber}_Completed", 1);
        PlayerPrefs.Save();
        
        // Обновляем все кнопки уровней после прохождения
        RefreshAllLevels();
    }
    
    // Метод для завершения текущего уровня
    public void CompleteCurrentLevel()
    {
        // Получаем актуальный номер уровня из PlayerPrefs
        int selectedLevel = PlayerPrefs.GetInt("SelectedLevel", 1);
        CompleteLevel(selectedLevel);
    }
    
    private void UpdateLevelButtonIfExists(int levelNumber)
    {
        // Ищем кнопку с соответствующим номером уровня
        LevelButton targetButton = allLevelButtons.Find(btn => btn.LevelNumber == levelNumber);
        
        if (targetButton != null)
        {
            targetButton.MarkAsCompleted();
        }
    }
    
    private void PlayCompletionSound()
    {
        if (audioSFX != null)
        {
            AudioClip completionClip = Resources.Load<AudioClip>("Audio/LevelComplete");
            if (completionClip != null)
            {
                audioSFX.PlayOneShot(completionClip);
            }
        }
    }
    
    // Метод для сброса всех уровней (для отладки)
    public void ResetAllLevels()
    {
        foreach (var button in allLevelButtons)
        {
            if (button != null)
            {
                button.ResetLevel();
            }
        }        
    }
    
    // Метод для получения статуса уровня
    public bool IsLevelCompleted(int levelNumber)
    {
        return PlayerPrefs.GetInt($"Level_{levelNumber}_Completed", 0) == 1;
    }
    
    // Метод для получения общего прогресса
    public float GetOverallProgress()
    {
        int completedLevels = 0;
        int totalLevels = allLevelButtons.Count;
        
        for (int i = 1; i <= totalLevels; i++)
        {
            if (IsLevelCompleted(i))
                completedLevels++;
        }
        
        return totalLevels > 0 ? (float)completedLevels / totalLevels : 0f;
    }
    
    // Свойства для доступа
    public int CurrentLevel => currentLevel;
    public List<LevelButton> AllLevelButtons => allLevelButtons;
    
    // Добавьте этот метод в LevelManager
    public void RefreshAllLevels()
    {
        LevelButton[] levelButtons = FindObjectsOfType<LevelButton>();
        foreach (LevelButton button in levelButtons)
        {
            button.RefreshLevelState();
        }
    }
} 