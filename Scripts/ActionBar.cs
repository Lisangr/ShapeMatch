
using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.UI;
using System.Collections;

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
    [SerializeField] private AudioSource audioSFX;

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
        // Воспроизводим звук FigureClick при попадании в ActionBar
        if (audioSFX != null)
        {
            AudioClip figureClickClip = Resources.Load<AudioClip>("Audio/FigureClick");
            if (figureClickClip != null)
            {
                audioSFX.PlayOneShot(figureClickClip);
            }
        }

        // Проверка на максимальное количество слотов
        if (slots.Count >= maxSlots)
        {
            Debug.Log("ActionBar is full!");
            GameManager.Instance.OnGameOver(false);
            // Уничтожаем фигуру, чтобы она не осталась висеть в пространстве
            Destroy(fig.gameObject);
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
            if (this != null && fig != null)
            {
                CheckMatches();
                // Добавляем небольшую задержку и потом проверяем победу
                StartCoroutine(DelayedWinCheck());
            }
        });
    }

    private IEnumerator DelayedWinCheck()
    {
        yield return new WaitForSeconds(0.1f);
        CheckWinCondition();
    }

    private void CheckMatches()
    {
        if (slots.Count < 3) return;

        // Подсчитываем количество каждого типа фигур
        Dictionary<string, List<Figure>> figureGroups = new Dictionary<string, List<Figure>>();

        foreach (var fig in slots)
        {
            if (fig != null)
            {
                string key = fig.MatchKey;
                if (!figureGroups.ContainsKey(key))
                    figureGroups[key] = new List<Figure>();
                figureGroups[key].Add(fig);
            }
        }

        // Ищем группы из 3 или более одинаковых фигур
        foreach (var group in figureGroups)
        {
            if (group.Value.Count >= 3)
            {
                // Удаляем 3 фигуры этого типа
                for (int i = 0; i < 3; i++)
                {
                    var fig = group.Value[i];
                    AnimateAndDestroyFigure(fig);
                    slots.Remove(fig);
                }

                // Перестраиваем ActionBar после удаления
                RearrangeFigures();

                // Проверяем условие победы
                CheckWinCondition();

                // Проверяем еще раз, возможно есть другие совпадения
                CheckMatches();
                return;
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

    public void CheckWinCondition()
    {
        var remainingFigures = FindObjectsOfType<Figure>();
        int figuresInPlay = 0;

        Debug.Log($"Checking win condition. Total figures found: {remainingFigures.Length}");

        foreach (var fig in remainingFigures)
        {
            // Проверяем, является ли родитель фигуры частью ActionBar
            bool isInActionBar = false;
            Transform parent = fig.transform.parent;
            while (parent != null)
            {
                if (parent == transform)
                {
                    isInActionBar = true;
                    break;
                }
                parent = parent.parent;
            }

            // Считаем только фигуры, которые не в ActionBar
            if (!isInActionBar)
            {
                figuresInPlay++;
                Debug.Log($"Figure in play: {fig.name}, animal: {fig.Animal}");
            }
            else
            {
                Debug.Log($"Figure in ActionBar: {fig.name}, animal: {fig.Animal}");
            }
        }

        Debug.Log($"Figures in play: {figuresInPlay}");

        if (figuresInPlay == 0)
        {
            Debug.Log("Win condition met!");
            GameManager.Instance.OnGameOver(true);
        }
    }

    public List<Figure> GetFigures()
    {
        return slots;
    }

    public void RemoveFigure(Figure figure)
    {
        if (!slots.Contains(figure)) return;

        // Анимируем уничтожение фигуры
        AnimateAndDestroyFigure(figure);

        // Удаляем фигуру из списка
        slots.Remove(figure);

        // Сдвигаем оставшиеся фигуры
        RearrangeFigures();
    }

    private void RearrangeFigures()
    {
        // Перемещаем все фигуры на свои новые позиции
        for (int i = 0; i < slots.Count; i++)
        {
            Figure fig = slots[i];
            RectTransform slotTransform = slotTransforms[i];

            // Анимируем перемещение фигуры на новую позицию
            Sequence moveSequence = DOTween.Sequence();

            moveSequence.Append(fig.transform.DOMove(slotTransform.position, moveDuration)
                .SetEase(moveEase));

            // Делаем фигуру дочерним объектом нового слота
            fig.transform.SetParent(slotTransform, true);
        }
    }

    public void ClearAllFigures()
    {
        // Очищаем все фигуры из ActionBar
        foreach (var figure in slots)
        {
            if (figure != null)
            {
                Destroy(figure.gameObject);
            }
        }
        slots.Clear();

        // Очищаем слоты от дочерних объектов
        foreach (var slotTransform in slotTransforms)
        {
            foreach (Transform child in slotTransform)
            {
                Destroy(child.gameObject);
            }
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
