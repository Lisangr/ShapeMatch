using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class FigureSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FigurePool figurePool;
    [SerializeField] private RectTransform spawnArea;
    [SerializeField] private Transform spawnParent;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnDelay = 0.5f;
    [SerializeField] private float dropDelay = 0.1f;
    [SerializeField] private float topOffset = 0.2f;
    [SerializeField] private int totalFigures = 30;
    [SerializeField] private float safeEdgeOffset = 1f; // Отступ от краев для безопасного спавна

    private Queue<Figure> spawnQueue;
    private Array shapeValues, colorValues, animalValues;
    private Dictionary<FigureAnimal, Sprite> animalSprites;
    private float leftBoundary;
    private float rightBoundary;
    private float spawnWidth;

    private void Awake()
    {
        shapeValues = Enum.GetValues(typeof(FigureShape));
        colorValues = Enum.GetValues(typeof(FigureColor));
        animalValues = Enum.GetValues(typeof(FigureAnimal));

        animalSprites = Resources.LoadAll<Sprite>("Animals")
            .Where(s => Enum.TryParse(s.name, out FigureAnimal _))
            .ToDictionary(s => (FigureAnimal)Enum.Parse(typeof(FigureAnimal), s.name), s => s);

        // Вычисляем границы спавна
        var corners = new Vector3[4];
        spawnArea.GetWorldCorners(corners);
        leftBoundary = corners[0].x + safeEdgeOffset;
        rightBoundary = corners[3].x - safeEdgeOffset;
        spawnWidth = rightBoundary - leftBoundary;
    }

    private void Start()
    {
        BuildSpawnQueue();
        StartCoroutine(SpawnRoutine());
    }

    public void RestartSpawning()
    {
        StopAllCoroutines();
        foreach (var fig in spawnQueue) Destroy(fig.gameObject);
        BuildSpawnQueue();
        StartCoroutine(SpawnRoutine());
    }

    private void BuildSpawnQueue()
    {
        spawnQueue = new Queue<Figure>();
        var template = new List<(FigureShape, FigureColor, FigureAnimal)>();
        var used = new HashSet<string>();
        var rnd = new System.Random();

        // Создаем тройки одинаковых фигур
        for (int i = 0; i < totalFigures / 3; i++)
        {
            FigureShape sh; FigureColor c; FigureAnimal a; string key;
            do
            {
                sh = (FigureShape)shapeValues.GetValue(rnd.Next(shapeValues.Length));
                c = (FigureColor)colorValues.GetValue(rnd.Next(colorValues.Length));
                a = (FigureAnimal)animalValues.GetValue(rnd.Next(animalValues.Length));
                key = $"{sh}-{c}-{a}";
            } while (!used.Add(key));
            for (int j = 0; j < 3; j++) template.Add((sh, c, a));
        }
        template = template.OrderBy(_ => rnd.Next()).ToList();

        var corners = new Vector3[4];
        spawnArea.GetWorldCorners(corners);
        float top = corners[1].y;

        // Распределяем фигуры по ширине равномерно
        float stepWidth = spawnWidth / (template.Count - 1);
        for (int i = 0; i < template.Count; i++)
        {
            var (sh, c, a) = template[i];
            var fig = figurePool.GetFigure();

            fig.Deactivate();
            fig.transform.SetParent(spawnParent, true);
            fig.Initialize(sh, c, a, animalSprites[a]);

            // Вычисляем позицию спавна с небольшой случайностью
            float baseX = leftBoundary + (i * stepWidth);
            float randomOffset = UnityEngine.Random.Range(-stepWidth * 0.3f, stepWidth * 0.3f);
            float spawnX = Mathf.Clamp(baseX + randomOffset, leftBoundary, rightBoundary);
            
            fig.transform.position = new Vector3(spawnX, top + topOffset, 0);
            spawnQueue.Enqueue(fig);
        }
    }

    private IEnumerator SpawnRoutine()
    {
        yield return new WaitForSeconds(spawnDelay);
        while (spawnQueue.Count > 0)
        {
            var fig = spawnQueue.Dequeue();
            fig.Activate();
            yield return new WaitForSeconds(dropDelay);
        }
    }
}