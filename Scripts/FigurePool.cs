using UnityEngine;
using System.Collections.Generic;

public class FigurePool : MonoBehaviour
{
    [SerializeField] private Figure figurePrefab;
    [SerializeField] private int initialPoolSize = 50;
    
    private Queue<Figure> availableFigures;
    private List<Figure> activeFigures;
    
    private void Awake()
    {
        availableFigures = new Queue<Figure>();
        activeFigures = new List<Figure>();
        InitializePool();
    }
    
    private void InitializePool()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewFigure();
        }
    }
    
    private void CreateNewFigure()
    {
        Figure figure = Instantiate(figurePrefab, transform);
        figure.Deactivate();
        availableFigures.Enqueue(figure);
    }
    
    public Figure GetFigure()
    {
        if (availableFigures.Count == 0)
        {
            CreateNewFigure();
        }
        
        Figure figure = availableFigures.Dequeue();
        activeFigures.Add(figure);
        return figure;
    }
    
    public void ReturnFigure(Figure figure)
    {
        if (activeFigures.Contains(figure))
        {
            activeFigures.Remove(figure);
            figure.Deactivate();
            figure.transform.SetParent(transform);
            availableFigures.Enqueue(figure);
        }
    }
    
    public void ReturnAllFigures()
    {
        foreach (Figure figure in activeFigures.ToArray())
        {
            ReturnFigure(figure);
        }
    }
} 