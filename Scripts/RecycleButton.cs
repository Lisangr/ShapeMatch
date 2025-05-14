using UnityEngine;
using UnityEngine.UI;

public class RecycleButton : MonoBehaviour
{
    [SerializeField] private Button recycleBtn;
    [SerializeField] private FigureSpawner spawner;

    private void Start()
    {
        recycleBtn.onClick.AddListener(() =>
        {
            // Очищаем ActionBar от всех фигур
            if (ActionBar.Instance != null)
            {
                ActionBar.Instance.ClearAllFigures();
            }
            
            GameFieldLogic.Instance.ClearField();
            spawner.RestartSpawning();
        });
    }
}