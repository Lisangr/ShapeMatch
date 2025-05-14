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

    // Метод для кнопки "Играть"
    public void PlayGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    // Метод для кнопки "Настройки"
    public void ToggleSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(!settingsPanel.activeSelf);
        }
    }

    // Метод для кнопки "Выйти"
    public void QuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #elif UNITY_ANDROID
            Application.Quit();
        #endif
    }

    // Метод для кнопки "Закрыть настройки" или "Назад"
    public void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    // Метод для возврата в главное меню
    public void ReturnToMenu()
    {
        SceneManager.LoadScene(menuSceneName);
    }

    // Метод для закрытия любой активной панели
    public void CloseActivePanel(GameObject panel)
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }
} 