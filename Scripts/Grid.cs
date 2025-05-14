using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DG.Tweening;

[System.Serializable]
public class TagPoints
{
    public string tag;
    public int points;
}

public class Grid : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private List<TagPoints> pointsPerTagList = new List<TagPoints>();
    [SerializeField] private GameObject defeatPanel;

    [Header("Animation")]
    [SerializeField] private float matchAnimationDuration = 0.5f;

    private Dictionary<string, int> pointsPerTag;

    private void Start()
    {
        InitializePointsSystem();
        if (defeatPanel != null)
        {
            defeatPanel.SetActive(false);
        }
    }

    private void InitializePointsSystem()
    {
        pointsPerTag = new Dictionary<string, int>();
        foreach (var tagPoints in pointsPerTagList)
        {
            pointsPerTag[tagPoints.tag] = tagPoints.points;
        }
    }

    private void Update()
    {
        CheckForMatches();
        CheckDefeatCondition();
    }

    private void CheckForMatches()
    {
        var allFigures = GetComponentsInChildren<Figure>().ToList();
        
        foreach (var tag in pointsPerTag.Keys)
        {
            var figuresWithTag = allFigures.Where(f => f.CompareTag(tag)).ToList();

            while (figuresWithTag.Count >= 3)
            {
                var matchedFigures = figuresWithTag.Take(3).ToList();
                HandleMatchedFigures(matchedFigures);
                figuresWithTag = figuresWithTag.Skip(3).ToList();
            }
        }
    }

    private void HandleMatchedFigures(List<Figure> matchedFigures)
    {
        if (matchedFigures.Count != 3) return;

        string tag = matchedFigures[0].tag;

        foreach (var figure in matchedFigures)
        {
            AnimateAndDestroyFigure(figure);
        }
    }

    private void AnimateAndDestroyFigure(Figure figure)
    {
        Sequence destroySequence = DOTween.Sequence();

        destroySequence.Append(figure.transform.DOScale(0f, matchAnimationDuration))
            .Join(figure.transform.DORotate(new Vector3(0, 360, 0), matchAnimationDuration, RotateMode.FastBeyond360))
            .OnComplete(() => Destroy(figure.gameObject));
    }
    private void CheckDefeatCondition()
    {
        int figuresCount = transform.childCount;
        if (figuresCount > 7 && defeatPanel != null)
        {
            defeatPanel.SetActive(true);
            Time.timeScale = 0f;
        }
    }
}