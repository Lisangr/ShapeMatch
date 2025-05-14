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
            GameFieldLogic.Instance.ClearField();
            spawner.RestartSpawning();
        });
    }
}