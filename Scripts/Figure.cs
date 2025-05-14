using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

public enum FigureShape
{
    circle,
    squad,
    traped,
    triangle
}

public enum FigureColor
{
    Red,
    Blue,
    Green,
    Yellow,
    Purple
}

public enum FigureAnimal
{
    bear,
    cheaken,
    cow,
    dog,
    elephant,
    fox,
    owl,
    panda,
    pig,
    pinguin,
    rabbit,
    sheep,
    unicorn
}

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class Figure : MonoBehaviour, IPointerClickHandler
{
    [Header("New Settings")]
    [SerializeField] private bool isClickable = true;
    [SerializeField] private string figureTag;

    [Header("References")]
    [SerializeField] private Image frameImage; // Рамка
    [SerializeField] private Image backgroundImage; // Фон
    [SerializeField] private Image animalImage;

    [Header("Animation Settings")]
    [SerializeField] private float moveDuration = 0.5f;
    [SerializeField] private float jumpPower = 2f;
    [SerializeField] private int jumpCount = 1;
    [SerializeField] private Ease moveEase = Ease.OutQuad;

    private FigureShape shape;
    private FigureColor color;
    private FigureAnimal animal;
    private Sprite animalSprite;
    public event Action<Figure> OnClicked;
    public string MatchKey => $"{shape}-{color}-{animal}";
    private Rigidbody2D rb;
    private Collider2D col2d;
    private Sequence moveSequence;
    private MoveToGrid moveToGrid;

    private static readonly Dictionary<FigureColor, Color> colorMap = new Dictionary<FigureColor, Color>
    {
        { FigureColor.Red, Color.red },
        { FigureColor.Blue, Color.blue },
        { FigureColor.Green, Color.green },
        { FigureColor.Yellow, Color.yellow },
        { FigureColor.Purple, new Color(0.5f, 0f, 0.5f) }
    };

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        moveToGrid = GetComponent<MoveToGrid>();
        ConfigureCollider();
    }

    private void OnDestroy()
    {
        // Убеждаемся, что все твины остановлены
        moveSequence?.Kill();
    }

    public void Initialize(FigureShape newShape, FigureColor newColor, FigureAnimal newAnimal, Sprite animalSp)
    {
        shape = newShape;
        color = newColor;
        animal = newAnimal;
        animalSprite = animalSp;
        isClickable = true;
        UpdateVisuals();
        ConfigureCollider();
    }

    private void UpdateVisuals()
    {
        // Загружаем спрайты для фрейма и фона
        string shapeName = shape.ToString().ToLower();
        Sprite frameSprite = Resources.Load<Sprite>($"Frames/{shapeName}");
        Sprite bgSprite = Resources.Load<Sprite>($"FrameBG/{shapeName}");

        if (frameSprite == null || bgSprite == null)
        {
            Debug.LogError($"Could not load sprites for shape {shapeName}");
            return;
        }

        // Устанавливаем спрайты
        if (frameImage != null)
        {
            frameImage.sprite = frameSprite;
            frameImage.color = Color.white; // Рамка всегда белая
        }

        if (backgroundImage != null)
        {
            backgroundImage.sprite = bgSprite;
            if (colorMap.TryGetValue(color, out var bgColor))
            {
                backgroundImage.color = bgColor;
            }
        }

        if (animalImage != null && animalSprite != null)
        {
            animalImage.sprite = animalSprite;
            animalImage.color = Color.white; // Животное тоже всегда белое
        }
    }

    private void ConfigureCollider()
    {
        // Remove existing collider
        var old = GetComponent<Collider2D>();
        if (old) Destroy(old);

        // Получаем размеры из спрайта фрейма
        Sprite sprite = frameImage.sprite;
        if (sprite == null) return;

        // Получаем реальные размеры спрайта в юнитах и увеличиваем их в 10 раз
        float pixelsPerUnit = sprite.pixelsPerUnit;
        float spriteWidth = (sprite.rect.width / pixelsPerUnit) * 10f;
        float spriteHeight = (sprite.rect.height / pixelsPerUnit) * 10f;

        // Учитываем масштаб объекта
        Vector3 scale = transform.lossyScale;
        spriteWidth *= scale.x;
        spriteHeight *= scale.y;

        switch (shape)
        {
            case FigureShape.circle:
                col2d = gameObject.AddComponent<CircleCollider2D>();
                var circ = (CircleCollider2D)col2d;
                circ.radius = spriteWidth * 0.5f;
                break;

            case FigureShape.squad:
                col2d = gameObject.AddComponent<BoxCollider2D>();
                var box = (BoxCollider2D)col2d;
                box.size = new Vector2(spriteWidth, spriteHeight);
                break;

            case FigureShape.triangle:
                col2d = gameObject.AddComponent<PolygonCollider2D>();
                var triPoly = (PolygonCollider2D)col2d;
                Vector2[] trianglePoints = new Vector2[]
                {
                    new Vector2(-spriteWidth/2, -spriteHeight/2),
                    new Vector2(spriteWidth/2, -spriteHeight/2),
                    new Vector2(0, spriteHeight/2)
                };
                triPoly.points = trianglePoints;
                break;

            case FigureShape.traped:
                col2d = gameObject.AddComponent<PolygonCollider2D>();
                var trapPoly = (PolygonCollider2D)col2d;
                float trapTopWidth = spriteWidth * 0.6f; // Верхняя часть трапеции 60% от ширины
                Vector2[] trapPoints = new Vector2[]
                {
                    new Vector2(-spriteWidth/2, -spriteHeight/2),
                    new Vector2(spriteWidth/2, -spriteHeight/2),
                    new Vector2(trapTopWidth/2, spriteHeight/2),
                    new Vector2(-trapTopWidth/2, spriteHeight/2)
                };
                trapPoly.points = trapPoints;
                break;
        }

        // Настройка физических свойств
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        
        // Настройка материала физики для лучшего взаимодействия
        if (col2d != null)
        {
            col2d.sharedMaterial = new PhysicsMaterial2D
            {
                friction = 0.4f,
                bounciness = 0.1f
            };
            
            // Устанавливаем смещение коллайдера, чтобы он точно совпадал со спрайтом
            col2d.offset = sprite.bounds.center;
        }
    }

    public void SetParent(Transform parent)
    {
        transform.SetParent(parent, true);
    }

    public void SetPosition(Vector3 worldPos)
    {
        transform.position = worldPos;
    }

    public void Activate()
    {
        gameObject.SetActive(true);
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (col2d == null) col2d = GetComponent<Collider2D>();
        rb.simulated = true;
        col2d.enabled = true;
        isClickable = true;
    }

    public void Deactivate()
    {
        moveSequence?.Kill();
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.simulated = false;
        if (col2d != null)
            col2d.enabled = false;
        gameObject.SetActive(false);
        isClickable = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isClickable) return;
        
        Debug.Log("Figure clicked!");
        
        // Проверяем, можно ли добавить фигуру в ActionBar
        if (ActionBar.Instance != null && ActionBar.Instance.CanAddFigure())
        {
            isClickable = false;
            TogglePhysics(false);
            ActionBar.Instance.AddFigure(this);
        }
        else if (moveToGrid != null)
        {
            moveToGrid.OnPointerClick();
        }
    }

    public void TogglePhysics(bool state)
    {
        if (rb != null) rb.simulated = state;
        if (col2d != null) col2d.enabled = state;
    }

    public void ResetState()
    {
        isClickable = true;
        TogglePhysics(true);
        transform.localScale = Vector3.one;
    }
}