using UnityEngine;
using DG.Tweening;

public class MoveToGrid : MonoBehaviour
{
    private Transform gridTransform;
    [SerializeField] private float moveDuration = 1f;
    [SerializeField] private float jumpPower = 2f;
    [SerializeField] private int jumpCount = 1;
    [SerializeField] private Ease moveEase = Ease.OutQuad;
    [SerializeField] private int newLayer;
    private bool isActive = true;
    private Sequence moveSequence;
    private Figure figure;

    private void Start()
    {
        gridTransform = FindObjectOfType<Grid>().GetComponent<Transform>();
        figure = GetComponent<Figure>();
    }

    private void OnDestroy()
    {
        moveSequence?.Kill();
    }

    public void OnPointerClick()
    {
        if (!isActive || figure == null) return;

        Debug.Log("MoveToGrid: Starting movement");
        figure.TogglePhysics(false);
        StartMoveToGrid();
    }

    private void StartMoveToGrid()
    {
        if (moveSequence != null)
        {
            moveSequence.Kill();
        }

        Vector3 startPosition = transform.position;
        Vector3 endPosition = gridTransform.position;

        Debug.Log($"Moving from {startPosition} to {endPosition}");

        moveSequence = DOTween.Sequence();

        // Основное движение с прыжком
        moveSequence.Append(transform.DOJump(endPosition, jumpPower, jumpCount, moveDuration)
            .SetEase(moveEase));

        // Добавляем вращение
        moveSequence.Join(transform.DORotate(new Vector3(0, 0, 360), moveDuration, RotateMode.FastBeyond360)
            .SetEase(moveEase));

        // Добавляем эффект масштабирования
        moveSequence.Join(transform.DOScale(Vector3.one * 0.8f, moveDuration * 0.5f)
            .SetLoops(2, LoopType.Yoyo));

        moveSequence.OnComplete(() => {
            Debug.Log("Movement completed");
            transform.SetParent(gridTransform);
            gameObject.layer = newLayer;
            CheckAndDeactivateLayers();
        });
    }

    public void SetActiveState(bool state)
    {
        isActive = state;
        if (figure != null)
        {
            figure.TogglePhysics(state);
        }
    }

    private void CheckAndDeactivateLayers()
    {
        int currentLayer = gameObject.layer;
        bool tilesOnCurrentLayer = false;

        GameObject[] allTiles = FindObjectsOfType<GameObject>();
        foreach (GameObject tile in allTiles)
        {
            if (tile.layer == currentLayer && tile != this.gameObject)
            {
                tilesOnCurrentLayer = true;
                break;
            }
        }

        if (!tilesOnCurrentLayer)
        {
            foreach (GameObject tile in allTiles)
            {
                if (tile.layer != currentLayer && tile.CompareTag("Tile"))
                {
                    MoveToGrid tileScript = tile.GetComponent<MoveToGrid>();
                    if (tileScript != null)
                    {
                        tileScript.SetActiveState(false);
                    }
                }
            }
        }
    }
}
