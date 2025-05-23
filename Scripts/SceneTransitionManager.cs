using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    // Событие, которое вызывается при завершении перехода
    public static event Action OnTransitionComplete;

    [Header("Stripe Transition Settings")]
    [SerializeField] private float transitionDuration = 0.8f;
    [SerializeField] private int stripeCount = 20;
    [SerializeField] private AnimationCurve easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private Color stripeColor = Color.black;

    // Убираем SerializeField - Canvas создается автоматически
    private Canvas transitionCanvas;
    private RectTransform stripeContainer;
    private List<RectTransform> stripes = new List<RectTransform>();
    private bool isTransitioning = false;

    // Публичное свойство для доступа к состоянию перехода
    public bool IsTransitioning => isTransitioning;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SetupTransitionCanvas();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void SetupTransitionCanvas()
    {
        // Всегда создаем новый Canvas для переходов
        GameObject canvasObj = new GameObject("TransitionCanvas");
        DontDestroyOnLoad(canvasObj);
        transitionCanvas = canvasObj.AddComponent<Canvas>();
        transitionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        transitionCanvas.sortingOrder = 1000; // Поверх всего
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        GraphicRaycaster raycaster = canvasObj.AddComponent<GraphicRaycaster>();

        // Создаем контейнер для полосок
        GameObject container = new GameObject("StripeContainer");
        container.transform.SetParent(transitionCanvas.transform, false);
        stripeContainer = container.AddComponent<RectTransform>();
        stripeContainer.anchorMin = Vector2.zero;
        stripeContainer.anchorMax = Vector2.one;
        stripeContainer.offsetMin = Vector2.zero;
        stripeContainer.offsetMax = Vector2.zero;
    }

    public void LoadScene(string sceneName)
    {
        if (isTransitioning) return;
        StartCoroutine(StripeTransition(sceneName));
    }

    private IEnumerator StripeTransition(string sceneName)
    {
        isTransitioning = true;

        // Начинаем загрузку сцены сразу, но не активируем
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        // Создаем полоски и анимируем их въезд
        CreateStripes();
        yield return AnimateStripes(true);

        // Ждем пока сцена полностью загрузится
        while (asyncLoad.progress < 0.9f)
        {
            yield return null;
        }

        // Активируем новую сцену
        asyncLoad.allowSceneActivation = true;
        
        // Ждем пока сцена активируется
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // Небольшая пауза для стабилизации
        yield return new WaitForSeconds(0.1f);

        // Анимируем выезд полосок
        yield return AnimateStripes(false);

        // Очищаем полоски
        ClearStripes();
        isTransitioning = false;
        
        // Уведомляем о завершении перехода
        OnTransitionComplete?.Invoke();
    }

    private void CreateStripes()
    {
        // Убеждаемся, что контейнер существует
        if (stripeContainer == null)
        {
            SetupTransitionCanvas();
        }

        ClearStripes();

        for (int i = 0; i < stripeCount; i++)
        {
            GameObject stripe = new GameObject($"Stripe_{i}");
            stripe.transform.SetParent(stripeContainer, false);

            Image stripeImage = stripe.AddComponent<Image>();
            stripeImage.color = stripeColor;

            RectTransform rect = stripe.GetComponent<RectTransform>();
            
            float yMin = i / (float)stripeCount;
            float yMax = (i + 1) / (float)stripeCount;
            
            rect.anchorMin = new Vector2(0, yMin);
            rect.anchorMax = new Vector2(1, yMax);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Используем ReferenceResolution для расчета позиций
            rect.anchoredPosition = new Vector2(1920f, 0); // Начальная позиция за правым краем

            stripes.Add(rect);
        }
    }

    private IEnumerator AnimateStripes(bool isClosing)
    {
        float halfDuration = transitionDuration * 0.5f; // Половина времени на каждую фазу
        float delayBetweenStripes = 0.02f; // Уменьшенная задержка
        
        Sequence sequence = DOTween.Sequence();

        for (int i = 0; i < stripes.Count; i++)
        {
            RectTransform stripe = stripes[i];
            Vector2 startPos, endPos;

            if (isClosing)
            {
                // Въезжают справа
                startPos = new Vector2(1920f, 0);
                endPos = Vector2.zero;
            }
            else
            {
                // Уезжают влево
                startPos = Vector2.zero;
                endPos = new Vector2(-1920f, 0);
            }

            stripe.anchoredPosition = startPos;

            // Важно: все полоски должны закончиться одновременно
            float stripeDelay = i * delayBetweenStripes;
            float stripeDuration = halfDuration + stripeDelay; // Увеличиваем длительность на размер задержки

            Tween tween = stripe.DOAnchorPos(endPos, stripeDuration)
                .SetEase(easeCurve)
                .SetDelay(stripeDelay);

            sequence.Join(tween);
        }

        yield return sequence.WaitForCompletion();
    }

    private void ClearStripes()
    {
        foreach (var stripe in stripes)
        {
            if (stripe != null)
                Destroy(stripe.gameObject);
        }
        stripes.Clear();
    }

    private void OnDestroy()
    {
        // Останавливаем все анимации при уничтожении
        DOTween.Kill(this);
    }
} 