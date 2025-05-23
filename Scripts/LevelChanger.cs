using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;

public class LevelChanger : MonoBehaviour
{
    [Header("Level Groups")]
    [SerializeField] private List<GameObject> levelGroups = new List<GameObject>();
    
    [Header("Navigation Buttons")]
    [SerializeField] private Button leftArrow;
    [SerializeField] private Button rightArrow;
    
    [Header("Animation Settings")]
    [SerializeField] private float transitionDuration = 0.8f;
    [SerializeField] private TransitionType transitionType = TransitionType.SlideWithFade;
    [SerializeField] private Ease transitionEase = Ease.InOutQuart;
    
    [Header("Audio")]
    [SerializeField] private AudioSource audioSFX;
    
    [Header("Audio Clips")]
    [SerializeField] private AudioClip pageFlipClip;
    [SerializeField] private AudioClip softTransitionClip;
    
    public enum TransitionType
    {
        SlideWithFade,      // Плавное скольжение с fade
        CrossFade,          // Перекрестное исчезновение
        ScaleAndFade,       // Масштабирование с fade
        SlideAndPush        // Скольжение с выталкиванием
    }
    
    private int currentGroupIndex = 0;
    private bool isTransitioning = false;
    
    // Позиции для анимации
    private Vector2 centerPosition;
    private Vector2 leftPosition;
    private Vector2 rightPosition;
    
    private void Start()
    {
        SetupPositions();
        InitializeGroups();
        
        if (leftArrow != null)
            leftArrow.onClick.AddListener(() => ChangePage(-1));
        if (rightArrow != null)
            rightArrow.onClick.AddListener(() => ChangePage(1));
        
        UpdateArrowButtons();
    }
    
    private void SetupPositions()
    {
        RectTransform rectTransform = GetComponent<RectTransform>();
        centerPosition = Vector2.zero;
        
        // Используем размер контейнера для плавного перехода
        float containerWidth = rectTransform.rect.width;
        leftPosition = new Vector2(-containerWidth * 1.2f, 0);
        rightPosition = new Vector2(containerWidth * 1.2f, 0);
    }
    
    private void InitializeGroups()
    {
        for (int i = 0; i < levelGroups.Count; i++)
        {
            if (levelGroups[i] != null)
            {
                RectTransform groupRect = levelGroups[i].GetComponent<RectTransform>();
                if (groupRect != null)
                {
                    groupRect.localScale = Vector3.one;
                    groupRect.anchoredPosition = centerPosition;
                    
                    // Настраиваем прозрачность для всех дочерних компонентов
                    CanvasGroup canvasGroup = levelGroups[i].GetComponent<CanvasGroup>();
                    if (canvasGroup == null)
                        canvasGroup = levelGroups[i].AddComponent<CanvasGroup>();
                    
                    canvasGroup.alpha = i == 0 ? 1f : 0f;
                }
                
                levelGroups[i].SetActive(i == 0);
            }
        }
    }
    
    public void ChangePage(int direction)
    {
        if (isTransitioning || levelGroups.Count <= 1) return;
        
        int newIndex = currentGroupIndex + direction;
        if (newIndex < 0 || newIndex >= levelGroups.Count) return;
        
        StartPageTransition(newIndex, direction);
    }
    
    private void StartPageTransition(int newIndex, int direction)
    {
        isTransitioning = true;
        
        GameObject currentGroup = levelGroups[currentGroupIndex];
        GameObject nextGroup = levelGroups[newIndex];
        
        PlayPageTurnSound();
        
        // Активируем следующую группу
        nextGroup.SetActive(true);
        
        switch (transitionType)
        {
            case TransitionType.SlideWithFade:
                StartSlideWithFadeTransition(currentGroup, nextGroup, direction);
                break;
            case TransitionType.CrossFade:
                StartCrossFadeTransition(currentGroup, nextGroup);
                break;
            case TransitionType.ScaleAndFade:
                StartScaleAndFadeTransition(currentGroup, nextGroup, direction);
                break;
            case TransitionType.SlideAndPush:
                StartSlideAndPushTransition(currentGroup, nextGroup, direction);
                break;
        }
        
        currentGroupIndex = newIndex;
        UpdateArrowButtons();
    }
    
    private void StartSlideWithFadeTransition(GameObject current, GameObject next, int direction)
    {
        RectTransform currentRect = current.GetComponent<RectTransform>();
        RectTransform nextRect = next.GetComponent<RectTransform>();
        CanvasGroup currentGroup = current.GetComponent<CanvasGroup>();
        CanvasGroup nextGroup = next.GetComponent<CanvasGroup>();
        
        // Настройка начальных позиций
        nextRect.anchoredPosition = direction > 0 ? rightPosition : leftPosition;
        nextGroup.alpha = 0f;
        
        Sequence transition = DOTween.Sequence();
        
        // Параллельная анимация
        transition.Append(currentRect.DOAnchorPos(direction > 0 ? leftPosition : rightPosition, transitionDuration)
            .SetEase(transitionEase))
            .Join(currentGroup.DOFade(0f, transitionDuration * 0.8f)
            .SetEase(transitionEase))
            .Join(nextRect.DOAnchorPos(centerPosition, transitionDuration)
            .SetEase(transitionEase))
            .Join(nextGroup.DOFade(1f, transitionDuration * 0.8f)
            .SetEase(transitionEase).SetDelay(transitionDuration * 0.2f));
        
        transition.OnComplete(() => FinishTransition(current, next, currentRect, nextRect));
    }
    
    private void StartCrossFadeTransition(GameObject current, GameObject next)
    {
        CanvasGroup currentGroup = current.GetComponent<CanvasGroup>();
        CanvasGroup nextGroup = next.GetComponent<CanvasGroup>();
        
        // Настройка начальных состояний
        nextGroup.alpha = 0f;
        next.GetComponent<RectTransform>().anchoredPosition = centerPosition;
        
        Sequence transition = DOTween.Sequence();
        
        // Ключевое изменение: делаем overlapping fade
        // Новая группа начинает появляться раньше, чем старая полностью исчезает
        transition.Append(nextGroup.DOFade(1f, transitionDuration * 0.7f)
            .SetEase(transitionEase))
            .Join(currentGroup.DOFade(0f, transitionDuration * 0.7f)
            .SetEase(transitionEase)
            .SetDelay(transitionDuration * 0.3f)); // Задержка для старой группы
        
        transition.OnComplete(() => FinishTransition(current, next, null, null));
    }
    
    private void StartScaleAndFadeTransition(GameObject current, GameObject next, int direction)
    {
        RectTransform currentRect = current.GetComponent<RectTransform>();
        RectTransform nextRect = next.GetComponent<RectTransform>();
        CanvasGroup currentGroup = current.GetComponent<CanvasGroup>();
        CanvasGroup nextGroup = next.GetComponent<CanvasGroup>();
        
        // Настройка начальных состояний
        nextRect.localScale = Vector3.one * 0.9f;
        nextRect.anchoredPosition = centerPosition;
        nextGroup.alpha = 0f;
        
        Sequence transition = DOTween.Sequence();
        
        // Убираем промежуток между анимациями
        transition.Append(currentRect.DOScale(0.9f, transitionDuration * 0.6f)
            .SetEase(transitionEase))
            .Join(currentGroup.DOFade(0f, transitionDuration * 0.6f)
            .SetEase(transitionEase))
            .Join(nextRect.DOScale(Vector3.one, transitionDuration * 0.6f)
            .SetEase(transitionEase).SetDelay(transitionDuration * 0.4f))
            .Join(nextGroup.DOFade(1f, transitionDuration * 0.6f)
            .SetEase(transitionEase).SetDelay(transitionDuration * 0.4f));
        
        transition.OnComplete(() => FinishTransition(current, next, currentRect, nextRect));
    }
    
    private void StartSlideAndPushTransition(GameObject current, GameObject next, int direction)
    {
        RectTransform currentRect = current.GetComponent<RectTransform>();
        RectTransform nextRect = next.GetComponent<RectTransform>();
        
        // Настройка начальных позиций
        nextRect.anchoredPosition = direction > 0 ? rightPosition : leftPosition;
        
        Sequence transition = DOTween.Sequence();
        
        // Оба элемента движутся одновременно
        transition.Append(currentRect.DOAnchorPos(direction > 0 ? leftPosition : rightPosition, transitionDuration)
            .SetEase(transitionEase))
            .Join(nextRect.DOAnchorPos(centerPosition, transitionDuration)
            .SetEase(transitionEase));
        
        transition.OnComplete(() => FinishTransition(current, next, currentRect, nextRect));
    }
    
    private void FinishTransition(GameObject current, GameObject next, RectTransform currentRect, RectTransform nextRect)
    {
        // Деактивируем предыдущую группу
        current.SetActive(false);
        
        // Сбрасываем трансформы
        if (currentRect != null)
        {
            currentRect.anchoredPosition = centerPosition;
            currentRect.localScale = Vector3.one;
        }
        
        if (nextRect != null)
        {
            nextRect.anchoredPosition = centerPosition;
            nextRect.localScale = Vector3.one;
        }
        
        // Сбрасываем alpha
        CanvasGroup currentGroup = current.GetComponent<CanvasGroup>();
        CanvasGroup nextGroup = next.GetComponent<CanvasGroup>();
        
        if (currentGroup != null) currentGroup.alpha = 1f;
        if (nextGroup != null) nextGroup.alpha = 1f;
        
        isTransitioning = false;
    }
    
    private void UpdateArrowButtons()
    {
        if (leftArrow != null)
        {
            leftArrow.interactable = currentGroupIndex > 0;
            
            // Плавная анимация прозрачности кнопок
            Image leftImage = leftArrow.GetComponent<Image>();
            if (leftImage != null)
            {
                float targetAlpha = leftArrow.interactable ? 1f : 0.3f;
                leftImage.DOFade(targetAlpha, 0.3f);
            }
        }
        
        if (rightArrow != null)
        {
            rightArrow.interactable = currentGroupIndex < levelGroups.Count - 1;
            
            Image rightImage = rightArrow.GetComponent<Image>();
            if (rightImage != null)
            {
                float targetAlpha = rightArrow.interactable ? 1f : 0.3f;
                rightImage.DOFade(targetAlpha, 0.3f);
            }
        }
    }
    
    private void PlayPageTurnSound()
    {
        if (audioSFX != null)
        {
            AudioClip clipToPlay = softTransitionClip != null ? softTransitionClip : pageFlipClip;
            
            if (clipToPlay != null)
            {
                audioSFX.PlayOneShot(clipToPlay);
            }
        }
    }
    
    // Публичные методы для управления
    public void SetTransitionType(TransitionType newType)
    {
        transitionType = newType;
    }
    
    public void GoToGroup(int groupIndex)
    {
        if (groupIndex < 0 || groupIndex >= levelGroups.Count || groupIndex == currentGroupIndex)
            return;
        
        int direction = groupIndex > currentGroupIndex ? 1 : -1;
        StartPageTransition(groupIndex, direction);
    }
    
    // Геттеры
    public int CurrentGroupIndex => currentGroupIndex;
    public int TotalGroups => levelGroups.Count;
    public bool IsTransitioning => isTransitioning;
    
    private void OnValidate()
    {
        if (levelGroups.Count > 0)
        {
            levelGroups.RemoveAll(item => item == null);
        }
    }
    
    private void ConfigureAudioSource()
    {
        if (audioSFX != null)
        {
            audioSFX.volume = Mathf.Clamp01(audioSFX.volume); // Убеждаемся что volume в пределах 0-1
            audioSFX.spatialBlend = 0f; // 2D звук
            audioSFX.loop = false;
            audioSFX.playOnAwake = false;
        }
    }
    
    private void Awake()
    {
        ConfigureAudioSource();
    }
} 