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
    //triangle
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
    unicorn,
    stink,
    bomb
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
    [SerializeField] private AudioSource audioSFX; // Добавляем ссылку на AudioSource

    [Header("Animation Settings")]
    [SerializeField] private float moveDuration = 0.5f;
    [SerializeField] private float jumpPower = 2f;
    [SerializeField] private int jumpCount = 1;
    [SerializeField] private Ease moveEase = Ease.OutQuad;

    [Header("Falling Variability")]
    [SerializeField] private float minGravityScale = 0.8f;
    [SerializeField] private float maxGravityScale = 1.2f;
    [SerializeField] private float rotationChance = 0.3f;
    [SerializeField] private float maxRotationSpeed = 180f;
    [SerializeField] private float turbulenceChance = 0.4f;
    [SerializeField] private float maxTurbulenceForce = 2f;
    [SerializeField] private float turbulenceInterval = 0.5f;

    private FigureShape shape;
    private FigureColor color;
    private FigureAnimal animal;
    private Sprite animalSprite;
    public event Action<Figure> OnClicked;
    public string MatchKey => $"{shape}-{color}-{animal}";
    private Rigidbody2D rb;
    private Collider2D col2d;
    private Sequence currentSequence;
    private MoveToGrid moveToGrid;
    private float lastMovementTime;
    private Vector3 lastPosition;
    private AudioSource cachedAudioSFX;
    private float nextTurbulenceTime;
    private bool isFalling = false;

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
        InitializeFallingVariability();
    }

    private void InitializeFallingVariability()
    {
        if (rb == null) return;

        // Случайная гравитация
        float randomGravityScale = UnityEngine.Random.Range(minGravityScale, maxGravityScale);
        rb.gravityScale = randomGravityScale;

        // Случайная ротация
        if (UnityEngine.Random.value < rotationChance)
        {
            rb.constraints = RigidbodyConstraints2D.None; // Разблокируем ротацию
            float randomRotationSpeed = UnityEngine.Random.Range(-maxRotationSpeed, maxRotationSpeed);
            rb.angularVelocity = randomRotationSpeed;
        }

        // Инициализация времени следующей турбулентности
        nextTurbulenceTime = Time.time + UnityEngine.Random.Range(0f, turbulenceInterval);
    }

    private void OnDestroy()
    {
        // Убеждаемся, что все твины остановлены
        currentSequence?.Kill();
        StopAllCoroutines();
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

        // Используем ТОЛЬКО CircleCollider2D для ВСЕХ фигур
        col2d = gameObject.AddComponent<CircleCollider2D>();
        var circleCollider = (CircleCollider2D)col2d;

        // Настройка радиуса и позиции в зависимости от формы
        if (shape == FigureShape.circle)
        {
            circleCollider.radius = 41f;
            circleCollider.offset = Vector2.zero;
        }
        else
        {
            circleCollider.radius = 41f;
            circleCollider.offset = Vector2.zero;
        }

        // Настройка физических свойств
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // Важно! Настраиваем физику для избежания пирамид
        rb.drag = 1.0f;        // Увеличиваем сопротивление воздуха
        rb.angularDrag = 5.0f; // Сильное сопротивление вращению
        rb.mass = 1.0f;        // Стандартная масса

        // Создаем специальный материал для предотвращения пирамид
        if (col2d != null)
        {
            PhysicsMaterial2D antiStackMaterial = new PhysicsMaterial2D("AntiStack");
            antiStackMaterial.friction = 0.1f;     // Очень низкое трение
            antiStackMaterial.bounciness = 0.0f;   // Никакой упругости
            col2d.sharedMaterial = antiStackMaterial;
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
        currentSequence?.Kill();
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

        // Воспроизводим звук Action при клике
        GameObject audioSFXObj = GameObject.Find("AudioSFX");
        if (audioSFXObj != null)
        {
            Debug.Log("Found AudioSFX object");
            AudioSource audioSource = audioSFXObj.GetComponent<AudioSource>();
            if (audioSource != null)
            {
                Debug.Log("Found AudioSource component");
                AudioClip actionClip = Resources.Load<AudioClip>("Audio/Action");
                if (actionClip != null)
                {
                    Debug.Log("Playing Action sound");
                    audioSource.PlayOneShot(actionClip);
                }
                else
                {
                    Debug.LogError("Action clip not found at Audio/Action");
                    // Запасной вариант - воспроизвести клип по умолчанию
                    if (audioSource.clip != null)
                    {
                        audioSource.PlayOneShot(audioSource.clip);
                    }
                }
            }
            else
            {
                Debug.LogError("AudioSource component not found");
            }
        }
        else
        {
            Debug.LogError("AudioSFX object not found");
        }

        // Проверяем, можно ли добавить фигуру в ActionBar
        if (ActionBar.Instance != null)
        {
            if (ActionBar.Instance.CanAddFigure())
            {
                isClickable = false;
                TogglePhysics(false);
                AnimateToActionBar();
            }
            else
            {
                // Если места нет - сразу показываем экран поражения
                GameManager.Instance.OnGameOver(false);
            }
        }
        else if (moveToGrid != null)
        {
            moveToGrid.OnPointerClick();
        }
    }

    public void AnimateToActionBar()
    {
        if (ActionBar.Instance == null) return;

        Vector3 targetPosition = ActionBar.Instance.transform.position;

        // Убиваем предыдущую анимацию, если она есть
        currentSequence?.Kill();

        // Создаем новую анимацию
        currentSequence = DOTween.Sequence();

        // Анимация прыжка и перемещения
        currentSequence.Append(transform.DOJump(targetPosition, jumpPower, jumpCount, moveDuration)
            .SetEase(moveEase))
            .OnComplete(() => {
                if (this != null && gameObject != null && ActionBar.Instance != null)
                {
                    if (Animal == FigureAnimal.bomb)
                    {
                        ExplodeInActionBar();
                    }
                    ActionBar.Instance.AddFigure(this);
                }
            });
    }

    private void ExplodeInActionBar()
    {
        // Проверяем, что ActionBar все еще существует
        if (ActionBar.Instance == null) return;
        StartCoroutine(ExplodeNextFrame());
    }

    private IEnumerator ExplodeNextFrame()
    {
        yield return null;  // Ждем следующий кадр

        // Проверяем, что объекты все еще существуют
        if (this == null || ActionBar.Instance == null) yield break;

        // Получаем список фигур из ActionBar
        var actionBarFigures = ActionBar.Instance.GetFigures();
        if (actionBarFigures == null) yield break;

        int index = actionBarFigures.IndexOf(this);

        if (index >= 0)
        {
            List<Figure> figuresToDestroy = new List<Figure>();

            // Добавляем соседние фигуры в список на удаление
            if (index > 0 && index - 1 < actionBarFigures.Count)
                figuresToDestroy.Add(actionBarFigures[index - 1]);
            if (index < actionBarFigures.Count - 1)
                figuresToDestroy.Add(actionBarFigures[index + 1]);

            // Анимируем и удаляем фигуры
            foreach (var fig in figuresToDestroy)
            {
                if (fig != null)
                    ActionBar.Instance.RemoveFigure(fig);
            }

            // Удаляем саму бомбу
            if (this != null && ActionBar.Instance != null)
                ActionBar.Instance.RemoveFigure(this);

            // Добавляем задержку и проверяем победу
            yield return new WaitForSeconds(0.1f);
            if (ActionBar.Instance != null)
            {
                ActionBar.Instance.CheckWinCondition();
            }
        }
    }

    public void OnFall()
    {
        if (animal == FigureAnimal.stink)
        {
            PullNeighboringFigure();
        }
    }

    private void PullNeighboringFigure()
    {
        // Логика для притягивания соседней фигуры
        var allFigures = FindObjectsOfType<Figure>();
        foreach (var fig in allFigures)
        {
            if (fig != this && Vector3.Distance(fig.transform.position, transform.position) < 1.5f)
            {
                fig.transform.DOMove(transform.position, 0.5f).SetEase(Ease.InOutQuad);
                break;
            }
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

    private void Update()
    {
        // Проверяем, двигается ли фигура
        if (Vector3.Distance(transform.position, lastPosition) > 0.01f)
        {
            lastMovementTime = Time.time;
            lastPosition = transform.position;
            isFalling = rb.velocity.y < -0.1f;
        }

        // Применяем турбулентность во время падения
        if (isFalling && Time.time >= nextTurbulenceTime)
        {
            ApplyTurbulence();
            nextTurbulenceTime = Time.time + turbulenceInterval;
        }

        // Если фигура не двигалась 2 секунды и она "висит" в воздухе
        if (Time.time - lastMovementTime > 2.0f && transform.position.y > 0 && rb != null)
        {
            // Проверяем, есть ли что-то под нами
            RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 2.0f);
            if (hit.collider == null || hit.distance > 1.5f)
            {
                // Если ничего нет, добавляем силу вниз
                rb.AddForce(Vector2.down * 2.0f, ForceMode2D.Impulse);
                lastMovementTime = Time.time;
            }
        }
    }

    private void ApplyTurbulence()
    {
        if (rb == null || !isFalling) return;

        if (UnityEngine.Random.value < turbulenceChance)
        {
            // Создаем случайную силу в горизонтальном направлении
            float randomForce = UnityEngine.Random.Range(-maxTurbulenceForce, maxTurbulenceForce);
            Vector2 turbulenceForce = new Vector2(randomForce, 0);
            
            // Применяем силу с небольшим случайным отклонением вверх
            turbulenceForce.y = UnityEngine.Random.Range(-0.5f, 0.5f);
            rb.AddForce(turbulenceForce, ForceMode2D.Impulse);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Проверяем, если фигура коснулась другой фигуры
        Figure otherFigure = collision.gameObject.GetComponent<Figure>();
        if (otherFigure != null)
        {
            // Добавляем небольшую случайную силу для предотвращения стакинга
            Vector2 separationForce = (transform.position - collision.transform.position).normalized;
            separationForce += new Vector2(UnityEngine.Random.Range(-0.1f, 0.1f), 0);

            if (rb != null)
            {
                rb.AddForce(separationForce * 0.5f, ForceMode2D.Impulse);
            }

            // Останавливаем ротацию при столкновении
            if (rb != null && rb.constraints == RigidbodyConstraints2D.None)
            {
                rb.angularVelocity *= 0.5f;
            }
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        // Если фигура долго находится в контакте с другой
        Figure otherFigure = collision.gameObject.GetComponent<Figure>();
        if (otherFigure != null)
        {
            // Проверяем, не застряли ли мы
            if (rb != null && rb.velocity.magnitude < 0.1f)
            {
                // Добавляем силу разделения
                Vector2 separation = (transform.position - collision.transform.position).normalized;
                separation.x += UnityEngine.Random.Range(-0.2f, 0.2f); // Добавляем случайность по X
                rb.AddForce(separation * 0.3f, ForceMode2D.Impulse);
            }
        }
    }

    public FigureAnimal Animal => animal;
}