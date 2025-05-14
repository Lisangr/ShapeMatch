using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject settingsPanel; // Панель настроек
    [SerializeField] private string gameSceneName = "MainLogic"; // Имя сцены с игрой
    [SerializeField] private string menuSceneName = "Menu"; // Имя сцены с меню

    [Header("Active Panels")]
    [SerializeField] private GameObject[] gamePanels; // Массив всех панелей в игре

    [Header("Audio")]
    [SerializeField] private AudioSource audioSFX; // AudioSource для воспроизведения звуков

    private void Start()
    {
        // Убеждаемся, что панель настроек скрыта при старте
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
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
        SceneManager.LoadScene(gameSceneName);
    }

    // Метод для кнопки "Настройки"
    public void ToggleSettings()
    {
        PlayClickSound();
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(!settingsPanel.activeSelf);
        }
    }

    // Метод для кнопки "Выйти"
    public void QuitGame()
    {
        PlayClickSound();
        #if UNITY_EDITOR
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
            settingsPanel.SetActive(false);
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
        SceneManager.LoadScene(menuSceneName);
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
} 