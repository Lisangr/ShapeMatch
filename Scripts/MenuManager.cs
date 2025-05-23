using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;

public class MenuManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject settingsPanel; // Панель настроек
    [SerializeField] private string gameSceneName = "MainLogic"; // Имя сцены с игрой
    [SerializeField] private string menuSceneName = "Menu"; // Имя сцены с меню
    [SerializeField] private string levelChangerSceneName = "LevelChanger"; // Имя сцены выбора уровней
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button closeSettingsButton;
    [SerializeField] private Image backgroundImage; // Reference to the background image component

    [Header("Active Panels")]
    [SerializeField] private GameObject[] gamePanels; // Массив всех панелей в игре

    [Header("Audio")]
    [SerializeField] private AudioSource audioSFX; // AudioSource для воспроизведения звуков

    [Header("Animation Settings")]
    [SerializeField] private float panelAnimationDuration = 0.6f;
    [SerializeField] private Ease openEase = Ease.OutBack;
    [SerializeField] private Ease closeEase = Ease.InBack;

    [Header("Background Settings")]
    [SerializeField] private Sprite forestSprite;
    [SerializeField] private Sprite bolotoSprite;
    [SerializeField] private Sprite mountainSprite;

    private void Start()
    {
        // Убеждаемся, что панель настроек скрыта при старте
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(OpenSettings);
        }

        if (closeSettingsButton != null)
        {
            closeSettingsButton.onClick.AddListener(CloseSettings);
        }

        // Load background sprites from Resources
        if (backgroundImage != null)
        {
            forestSprite = Resources.Load<Sprite>("Images/forest");
            bolotoSprite = Resources.Load<Sprite>("Images/boloto");
            mountainSprite = Resources.Load<Sprite>("Images/mountain");
            
            UpdateBackgroundImage();
        }
    }

    private void Update()
    {
        // Обработка кнопки "Назад" на Android
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Если мы в игровой сцене
            if (SceneManager.GetActiveScene().name == gameSceneName)
            {
                // Проверяем, есть ли активные панели
                bool anyPanelActive = false;
                if (gamePanels != null)
                {
                    foreach (var panel in gamePanels)
                    {
                        if (panel != null && panel.activeSelf)
                        {
                            panel.SetActive(false);
                            anyPanelActive = true;
                            break;
                        }
                    }
                }

                // Если нет активных панелей, возвращаемся в меню
                if (!anyPanelActive)
                {
                    ReturnToMenu();
                }
            }
            // Если мы в меню
            else if (SceneManager.GetActiveScene().name == menuSceneName)
            {
                // Если открыта панель настроек, закрываем её
                if (settingsPanel != null && settingsPanel.activeSelf)
                {
                    CloseSettings();
                }
                // Иначе выходим из игры
                else
                {
                    QuitGame();
                }
            }
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

    // Метод для кнопки "Играть"
    public void PlayGame()
    {
        PlayClickSound();
        // Останавливаем все звуки перед переходом на новую сцену
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StopAllSounds();
        }
        
        // Используем новую систему переходов с полосками
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadScene(gameSceneName);
        }
        else
        {
            SceneManager.LoadScene(gameSceneName);
        }
    }

    // Метод для кнопки "Настройки"
    public void ToggleSettings()
    {
        PlayClickSound();
        if (settingsPanel != null)
        {
            if (settingsPanel.activeSelf)
            {
                CloseSettings();
            }
            else
            {
                OpenSettings();
            }
        }
    }

    // Метод для кнопки "Выйти"
    public void QuitGame()
    {
        PlayClickSound();
#if UNITY_WEBGL && !UNITY_EDITOR
            Application.OpenURL("https://yandex." + YandexGame.EnvironmentData.domain + "/games/");
#elif UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #elif UNITY_ANDROID
            Application.Quit();
        #endif
    }

    // Метод для кнопки "Закрыть настройки" или "Назад"
    public void CloseSettings()
    {
        PlayClickSound();
        if (settingsPanel != null)
        {
            AnimateSettingsPanel(false);
        }
    }

    // Метод для возврата в главное меню
    public void ReturnToMenu()
    {
        PlayClickSound();
        // Останавливаем все звуки перед переходом на новую сцену
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StopAllSounds();
        }
        
        // Используем новую систему переходов с полосками
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadScene(menuSceneName);
        }
        else
        {
            SceneManager.LoadScene(menuSceneName);
        }
    }

    // Метод для закрытия любой активной панели
    public void CloseActivePanel(GameObject panel)
    {
        PlayClickSound();
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    public void OpenSettings()
    {
        PlayClickSound();
        if (settingsPanel != null)
        {
            AnimateSettingsPanel(true);
        }
    }

    private void AnimateSettingsPanel(bool isOpening)
    {
        if (settingsPanel == null) return;
        
        RectTransform panelRect = settingsPanel.GetComponent<RectTransform>();
        if (panelRect == null) return;
        
        if (isOpening)
        {
            // Активируем панель
            settingsPanel.SetActive(true);
            
            // Устанавливаем начальные значения для открытия
            panelRect.localScale = Vector3.zero;
            panelRect.rotation = Quaternion.Euler(0, 0, 180f);
            
            // Создаем анимационную последовательность открытия
            Sequence openSequence = DOTween.Sequence();
            
            openSequence.Append(panelRect.DOScale(Vector3.one, panelAnimationDuration)
                .SetEase(openEase))
                .Join(panelRect.DORotate(Vector3.zero, panelAnimationDuration)
                .SetEase(Ease.OutCubic))
                // Добавляем небольшую пульсацию
                .Append(panelRect.DOScale(Vector3.one * 1.05f, 0.1f)
                .SetEase(Ease.OutQuad))
                .Append(panelRect.DOScale(Vector3.one, 0.1f)
                .SetEase(Ease.InQuad));
        }
        else
        {
            // Создаем анимационную последовательность закрытия
            Sequence closeSequence = DOTween.Sequence();
            
            closeSequence.Append(panelRect.DOScale(Vector3.zero, panelAnimationDuration)
                .SetEase(closeEase))
                .Join(panelRect.DORotate(new Vector3(0, 0, -180f), panelAnimationDuration)
                .SetEase(Ease.InCubic))
                .OnComplete(() => {
                    // Деактивируем панель после завершения анимации
                    settingsPanel.SetActive(false);
                });
        }
    }

    // Метод для кнопки "Выбор уровня"
    public void LoadLevelChanger()
    {
        PlayClickSound();
        // Останавливаем все звуки перед переходом на новую сцену
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StopAllSounds();
        }
        
        // Используем систему переходов с полосками
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadScene(levelChangerSceneName);
        }
        else
        {
            SceneManager.LoadScene(levelChangerSceneName);
        }
    }

    private void UpdateBackgroundImage()
    {
        if (backgroundImage == null) return;

        // Получаем название спрайта из PlayerPrefs
        string spriteName = PlayerPrefs.GetString("BackgroundSprite", "forest");
        Sprite newSprite = null;

        // Загружаем спрайт по имени
        newSprite = Resources.Load<Sprite>($"Images/{spriteName}");

        if (newSprite != null)
        {
            backgroundImage.sprite = newSprite;
        }
        else
        {
            Debug.LogWarning($"Could not load background sprite: {spriteName}");
        }
    }

    // Метод для установки текущего уровня
    public void SetCurrentLevel(int level)
    {
        PlayerPrefs.SetInt("CurrentLevel", level);
        PlayerPrefs.Save();
        UpdateBackgroundImage();
    }
} 