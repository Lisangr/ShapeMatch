using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.UI;

public class ActionBar : MonoBehaviour
{
    public static ActionBar Instance { get; private set; }

    [System.Serializable]
    public class TagPoints
    {
        public string tag;
        public int points;
    }

    [Header("References")]
    [SerializeField] private RectTransform slotsParent;
    [SerializeField] private Text scoreText;
    [SerializeField] private GameObject defeatPanel;

    [Header("Settings")]
    [SerializeField] private int maxSlots = 7;
    [SerializeField] private float slotSpacing = 100f;
    [SerializeField] private Vector2 slotOffset = new Vector2(0, 0);
    [SerializeField] private List<TagPoints> matchRules = new List<TagPoints>();

    [Header("Animation")]
    [SerializeField] private float moveDuration = 0.5f;
    [SerializeField] private Ease moveEase = Ease.OutQuad;

    private List<Figure> slots = new List<Figure>();
    private List<RectTransform> slotTransforms = new List<RectTransform>();
    private Dictionary<string, int> scoreMap = new Dictionary<string, int>();
    private int totalScore;

    private void Awake()
    {
        Instance = this;
        InitializeScoreSystem();
        CreateSlots();
    }

    private void InitializeScoreSystem()
    {
        scoreMap = new Dictionary<string, int>();
        foreach (var rule in matchRules)
        {
            scoreMap[rule.tag] = rule.points;
        }
    }

    private void CreateSlots()
    {
        Debug.Log("Creating slots...");
        float startX = -(slotSpacing * (maxSlots - 1)) / 2f;

        for (int i = 0; i < maxSlots; i++)
        {
            // Создаем пустой GameObject для слота
            GameObject slotObj = new GameObject($"Slot_{i}");
            RectTransform rectTransform = slotObj.AddComponent<RectTransform>();
            rectTransform.SetParent(slotsParent, false);
            
            // Настраиваем RectTransform слота
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(100f, 100f); // Размер слота
            
            Vector2 position = new Vector2(startX + (slotSpacing * i), 0) + slotOffset;
            rectTransform.anchoredPosition = position;
            
            slotTransforms.Add(rectTransform);
            Debug.Log($"Created slot {i} at position {position}");
        }
    }

    public bool CanAddFigure()
    {
        return slots.Count < maxSlots;
    }

    public void AddFigure(Figure fig)
    {
        if (slots.Count >= maxSlots)
        {
            Debug.Log("ActionBar is full!");
            GameManager.Instance.OnGameOver(false);
            return;
        }

        Debug.Log($"Adding figure to slot {slots.Count}");
        slots.Add(fig);
        int slotIndex = slots.Count - 1;

        // Получаем трансформ слота
        RectTransform slotTransform = slotTransforms[slotIndex];
        
        // Делаем фигуру дочерним объектом слота
        fig.transform.SetParent(slotTransform, true);
        
        // Настраиваем RectTransform фигуры
        RectTransform figureRect = fig.transform as RectTransform;
        if (figureRect != null)
        {
            figureRect.anchorMin = new Vector2(0.5f, 0.5f);
            figureRect.anchorMax = new Vector2(0.5f, 0.5f);
            figureRect.pivot = new Vector2(0.5f, 0.5f);
            figureRect.anchoredPosition = Vector2.zero; // Центрируем фигуру в слоте
        }

        // Создаем анимацию
        Sequence moveSequence = DOTween.Sequence();

        // Анимация перемещения
        moveSequence.Append(fig.transform.DOMove(slotTransform.position, moveDuration)
            .SetEase(moveEase));

        // Добавляем вращение
        moveSequence.Join(fig.transform.DORotate(new Vector3(0, 0, 360), moveDuration, RotateMode.FastBeyond360)
            .SetEase(moveEase));

        // Добавляем масштабирование
        moveSequence.Join(fig.transform.DOScale(Vector3.one * 0.8f, moveDuration * 0.5f)
            .SetLoops(2, LoopType.Yoyo));

        // После завершения анимации проверяем совпадения
        moveSequence.OnComplete(() => {
            CheckMatches();
        });
    }

    private void CheckMatches()
    {
        if (slots.Count < 3) return;

        int lastIndex = slots.Count - 1;
        if (lastIndex >= 2)
        {
            var last3 = slots.GetRange(lastIndex - 2, 3);
            if (AreMatching(last3))
            {
                foreach (var fig in last3)
                {
                    AnimateAndDestroyFigure(fig);
                }
                slots.RemoveRange(lastIndex - 2, 3);
                CheckWinCondition();
            }
        }
    }

    private void AnimateAndDestroyFigure(Figure figure)
    {
        Sequence destroySequence = DOTween.Sequence();

        destroySequence.Append(figure.transform.DOScale(0f, moveDuration * 0.5f))
            .Join(figure.transform.DORotate(new Vector3(0, 360, 0), moveDuration * 0.5f, RotateMode.FastBeyond360))
            .OnComplete(() => {
                // Очищаем слот перед уничтожением фигуры
                RectTransform slotTransform = figure.transform.parent as RectTransform;
                Destroy(figure.gameObject);
            });
    }

    private bool AreMatching(List<Figure> figures)
    {
        if (figures.Count != 3) return false;
        return figures[0].MatchKey == figures[1].MatchKey && 
               figures[1].MatchKey == figures[2].MatchKey;
    }

    private void CheckWinCondition()
    {
        var remainingFigures = FindObjectsOfType<Figure>();
        int figuresInPlay = 0;
        
        foreach (var fig in remainingFigures)
        {
            if (!slots.Contains(fig))
            {
                figuresInPlay++;
            }
        }

        if (figuresInPlay == 0)
        {
            GameManager.Instance.OnGameOver(true);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (slotsParent == null)
        {
            slotsParent = transform as RectTransform;
        }
    }
#endif
}