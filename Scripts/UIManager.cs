using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    [SerializeField] private GameObject winScreen, loseScreen;
    private void Awake() => Instance = this;
    public void ShowWin() => winScreen.SetActive(true);
    public void ShowLose() => loseScreen.SetActive(true);
}