using UnityEngine;
using System.Collections.Generic;

public class GameFieldLogic : MonoBehaviour
{
    public static GameFieldLogic Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        foreach (var fig in FindObjectsOfType<Figure>())
            fig.OnClicked += OnFigureClicked;
    }

    private void OnFigureClicked(Figure fig)
    {
        fig.OnClicked -= OnFigureClicked;
        ActionBar.Instance.AddFigure(fig);
    }

    public void ClearField()
    {
        foreach (var fig in FindObjectsOfType<Figure>())
            Destroy(fig.gameObject);
    }
}
