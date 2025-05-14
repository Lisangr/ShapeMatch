using UnityEngine;
using UnityEngine.UI;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource audioBG;
    [SerializeField] private AudioSource audioSFX;

    [Header("Toggle References")]
    [SerializeField] private Toggle masterToggle;  // Переключатель Sounds
    [SerializeField] private Toggle bgMusicToggle; // Переключатель BGMusic
    [SerializeField] private Toggle sfxToggle;     // Переключатель SFX

    [Header("Sprite References")]
    [SerializeField] private Image masterBackground;  // Background для Sounds
    [SerializeField] private Image bgMusicBackground; // Background для BGMusic
    [SerializeField] private Image sfxBackground;     // Background для SFX
    
    [Header("Checkmark References")]
    [SerializeField] private Image masterCheckmark;  // Checkmark для Sounds
    [SerializeField] private Image bgMusicCheckmark; // Checkmark для BGMusic
    [SerializeField] private Image sfxCheckmark;     // Checkmark для SFX
    
    [Header("Sprites")]
    [SerializeField] private Sprite soundOnSprite;
    [SerializeField] private Sprite soundOffSprite;
    [SerializeField] private Sprite checkOnSprite;
    [SerializeField] private Sprite checkOffSprite;

    // Ключи для PlayerPrefs
    private const string MASTER_KEY = "MasterSound";
    private const string BG_MUSIC_KEY = "BGMusic";
    private const string SFX_KEY = "SFX";

    // Сохранение предыдущего состояния переключателей
    private bool previousBGMusicState;
    private bool previousSFXState;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SetupToggles();
            LoadSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void SetupToggles()
    {
        // Отключаем стандартное поведение Toggle для Checkmark
        if (masterToggle != null)
        {
            masterToggle.graphic = null; // Отключаем стандартный графический элемент
            masterCheckmark.gameObject.SetActive(true); // Делаем Checkmark всегда видимым
        }
        
        if (bgMusicToggle != null)
        {
            bgMusicToggle.graphic = null;
            bgMusicCheckmark.gameObject.SetActive(true);
        }
        
        if (sfxToggle != null)
        {
            sfxToggle.graphic = null;
            sfxCheckmark.gameObject.SetActive(true);
        }
    }

    private void Start()
    {
        // Подписываемся на события изменения переключателей
        if (masterToggle != null) masterToggle.onValueChanged.AddListener(OnMasterToggleChanged);
        if (bgMusicToggle != null) bgMusicToggle.onValueChanged.AddListener(OnBGMusicToggleChanged);
        if (sfxToggle != null) sfxToggle.onValueChanged.AddListener(OnSFXToggleChanged);

        // Сохраняем начальное состояние
        previousBGMusicState = bgMusicToggle != null && bgMusicToggle.isOn;
        previousSFXState = sfxToggle != null && sfxToggle.isOn;

        // Применяем начальные настройки
        ApplySettings();
    }

    private void LoadSettings()
    {
        // Загружаем сохраненные настройки
        bool masterEnabled = PlayerPrefs.GetInt(MASTER_KEY, 1) == 1;
        bool bgMusicEnabled = PlayerPrefs.GetInt(BG_MUSIC_KEY, 1) == 1;
        bool sfxEnabled = PlayerPrefs.GetInt(SFX_KEY, 1) == 1;

        // Устанавливаем значения переключателей
        if (masterToggle != null) masterToggle.isOn = masterEnabled;
        if (bgMusicToggle != null)
        {
            bgMusicToggle.isOn = bgMusicEnabled;
            previousBGMusicState = bgMusicEnabled;
        }
        if (sfxToggle != null)
        {
            sfxToggle.isOn = sfxEnabled;
            previousSFXState = sfxEnabled;
        }
    }

    private void SaveSettings()
    {
        PlayerPrefs.SetInt(MASTER_KEY, masterToggle != null && masterToggle.isOn ? 1 : 0);
        // Сохраняем предыдущее состояние для дочерних переключателей, если мастер выключен
        bool masterOn = masterToggle != null && masterToggle.isOn;
        PlayerPrefs.SetInt(BG_MUSIC_KEY, masterOn ? (bgMusicToggle != null && bgMusicToggle.isOn ? 1 : 0) : (previousBGMusicState ? 1 : 0));
        PlayerPrefs.SetInt(SFX_KEY, masterOn ? (sfxToggle != null && sfxToggle.isOn ? 1 : 0) : (previousSFXState ? 1 : 0));
        PlayerPrefs.Save();
    }

    private void OnMasterToggleChanged(bool isOn)
    {
        // Обновляем спрайт
        if (masterBackground != null)
        {
            masterBackground.sprite = isOn ? soundOnSprite : soundOffSprite;
        }

        if (!isOn)
        {
            // Сохраняем текущее состояние перед выключением
            previousBGMusicState = bgMusicToggle != null && bgMusicToggle.isOn;
            previousSFXState = sfxToggle != null && sfxToggle.isOn;

            // Выключаем переключатели
            if (bgMusicToggle != null) bgMusicToggle.isOn = false;
            if (sfxToggle != null) sfxToggle.isOn = false;
        }
        else
        {
            // Восстанавливаем предыдущее состояние
            if (bgMusicToggle != null) bgMusicToggle.isOn = previousBGMusicState;
            if (sfxToggle != null) sfxToggle.isOn = previousSFXState;
        }

        ApplySettings();
        SaveSettings();
    }

    private void OnBGMusicToggleChanged(bool isOn)
    {
        // Обновляем спрайт
        if (bgMusicBackground != null)
        {
            bgMusicBackground.sprite = isOn ? soundOnSprite : soundOffSprite;
        }

        // Если включаем музыку, убеждаемся что главный переключатель включен
        if (isOn && masterToggle != null)
        {
            masterToggle.isOn = true;
        }

        ApplySettings();
        SaveSettings();
    }

    private void OnSFXToggleChanged(bool isOn)
    {
        // Обновляем спрайт
        if (sfxBackground != null)
        {
            sfxBackground.sprite = isOn ? soundOnSprite : soundOffSprite;
        }

        // Если включаем SFX, убеждаемся что главный переключатель включен
        if (isOn && masterToggle != null)
        {
            masterToggle.isOn = true;
        }

        ApplySettings();
        SaveSettings();
    }

    private void ApplySettings()
    {
        bool masterEnabled = masterToggle != null && masterToggle.isOn;
        bool bgMusicEnabled = bgMusicToggle != null && bgMusicToggle.isOn && masterEnabled;
        bool sfxEnabled = sfxToggle != null && sfxToggle.isOn && masterEnabled;

        // Применяем настройки к аудиосорсам
        if (audioBG != null) audioBG.mute = !bgMusicEnabled;
        if (audioSFX != null) audioSFX.mute = !sfxEnabled;

        // Обновляем все спрайты
        UpdateSprites();
    }

    private void UpdateSprites()
    {
        bool masterEnabled = masterToggle != null && masterToggle.isOn;
        bool bgMusicEnabled = bgMusicToggle != null && bgMusicToggle.isOn && masterEnabled;
        bool sfxEnabled = sfxToggle != null && sfxToggle.isOn && masterEnabled;

        // Обновляем спрайты фона
        if (masterBackground != null)
            masterBackground.sprite = masterEnabled ? soundOnSprite : soundOffSprite;
        
        if (bgMusicBackground != null)
            bgMusicBackground.sprite = bgMusicEnabled ? soundOnSprite : soundOffSprite;
        
        if (sfxBackground != null)
            sfxBackground.sprite = sfxEnabled ? soundOnSprite : soundOffSprite;

        // Обновляем спрайты чекмарков (теперь они всегда видимы)
        if (masterCheckmark != null)
            masterCheckmark.sprite = masterEnabled ? checkOnSprite : checkOffSprite;
        
        if (bgMusicCheckmark != null)
            bgMusicCheckmark.sprite = bgMusicEnabled ? checkOnSprite : checkOffSprite;
        
        if (sfxCheckmark != null)
            sfxCheckmark.sprite = sfxEnabled ? checkOnSprite : checkOffSprite;
    }

    // Публичный метод для воспроизведения SFX
    public void PlaySFX(AudioClip clip)
    {
        if (audioSFX != null && sfxToggle != null && sfxToggle.isOn && masterToggle != null && masterToggle.isOn)
        {
            audioSFX.PlayOneShot(clip);
        }
    }
} 