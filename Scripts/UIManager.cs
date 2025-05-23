using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    
    [Header("UI Panels")]
    [SerializeField] private GameObject winScreen;
    [SerializeField] private GameObject loseScreen;
    [SerializeField] private Image backgroundImage; // Делаем приватным, но оставляем публичным свойство
    
    [Header("Animation Settings")]
    [SerializeField] private float panelAnimationDuration = 0.6f;
    [SerializeField] private Ease openEase = Ease.OutBack;

    // Публичное свойство для доступа к backgroundImage
    public Image BackgroundImage => backgroundImage;
    
    private void Awake() 
    { 
        Instance = this;
        
        // Убеждаемся, что панели неактивны в начале
        if (winScreen != null) winScreen.SetActive(false);
        if (loseScreen != null) loseScreen.SetActive(false);

        // Проверяем наличие backgroundImage
        if (backgroundImage == null)
        {
            Debug.LogWarning("Background Image не назначен в UIManager!");
        }
    }
    
    private void Start()
    {
        UpdateBackgroundImage();
    }

    private void UpdateBackgroundImage()
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
    
    public void ShowWinPanel()
    {
        if (winScreen != null)
        {
            // Получаем правильный номер уровня
            int levelNumber = PlayerPrefs.GetInt("SelectedLevel", 1);
            
            // Анимация появления панели победы
            winScreen.SetActive(true);
            RectTransform winRect = winScreen.GetComponent<RectTransform>();
            
            if (winRect != null)
            {
                winRect.localScale = Vector3.zero;
                winRect.rotation = Quaternion.Euler(0, 0, 180f);
                
                winRect.DOScale(Vector3.one, 0.6f).SetEase(Ease.OutBack);
                winRect.DORotate(Vector3.zero, 0.6f).SetEase(Ease.OutCubic);
            }
            
            // Завершаем правильный уровень
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.CompleteLevel(levelNumber);
            }
        }
    }
    
    public void ShowLose() 
    { 
        AnimatePanel(loseScreen);
    }
    
    private void AnimatePanel(GameObject panel)
    {
        if (panel == null) return;
        
        // Активируем панель
        panel.SetActive(true);
        
        // Получаем RectTransform панели
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        if (panelRect == null) return;
        
        // Устанавливаем начальные значения (маленький размер, повернутый)
        panelRect.localScale = Vector3.zero;
        panelRect.rotation = Quaternion.Euler(0, 0, 180f);
        
        // Создаем анимационную последовательность
        Sequence panelSequence = DOTween.Sequence();
        
        // Анимация увеличения с одновременным поворотом
        panelSequence.Append(panelRect.DOScale(Vector3.one, panelAnimationDuration)
            .SetEase(openEase))
            .Join(panelRect.DORotate(Vector3.zero, panelAnimationDuration)
            .SetEase(Ease.OutCubic))
            // Добавляем небольшую пульсацию в конце
            .Append(panelRect.DOScale(Vector3.one * 1.05f, 0.1f)
            .SetEase(Ease.OutQuad))
            .Append(panelRect.DOScale(Vector3.one, 0.1f)
            .SetEase(Ease.InQuad));
    }
}