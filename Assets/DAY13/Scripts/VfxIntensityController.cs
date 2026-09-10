using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.VFX;

public class VfxIntensityController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private VisualEffect visualEffect;

    [Header("Exposed Property")]
    [SerializeField] private string spawnRateName = "SpawnRate";

    [Header("Intensity")]
    [SerializeField] private float defaultRate = 80f;
    [SerializeField] private float lowRate = 20f;
    [SerializeField] private float highRate = 200f;

    [Header("Quality Option")]
    [Tooltip("체크하면 시작할 때 현재 Quality Level에 따라 기본 강도를 선택합니다.")]
    [SerializeField] private bool applyQualityPresetOnStart = false;

    [Tooltip("이 값 이하의 Quality Level이면 Low Rate를 사용합니다.")]
    [SerializeField] private int lowQualityMaxLevel = 1;

    [Header("Debug")]
    [SerializeField] private bool showDebugOverlay = true;

    private float currentRate;
    private string currentMode = "DEFAULT";
    private bool propertyReady;

    public VisualEffect TargetVisualEffect => visualEffect;
    public string SpawnRateName => spawnRateName;
    public float LowRate => lowRate;
    public float HighRate => highRate;

    private void Start()
    {
        RefreshPropertyState();

        if (!propertyReady)
        {
            Debug.LogError(
                "DAY13: VFX Graph에 Float Exposed Property 'SpawnRate'가 없습니다. " +
                "Blackboard에서 SpawnRate를 Exposed로 만들고 Constant Spawn Rate 입력에 연결하세요.");
            return;
        }

        if (applyQualityPresetOnStart)
        {
            ApplyCurrentQualityPreset();
        }
        else
        {
            ApplyRate(defaultRate, "DEFAULT");
        }
    }

    public void OnLowIntensity(InputValue value)
    {
        if (value.isPressed)
        {
            ApplyRate(lowRate, "LOW");
        }
    }

    public void OnHighIntensity(InputValue value)
    {
        if (value.isPressed)
        {
            ApplyRate(highRate, "HIGH");
        }
    }

    [ContextMenu("Apply Default Rate (80)")]
    public void ApplyDefaultRate()
    {
        ApplyRate(defaultRate, "DEFAULT");
    }

    [ContextMenu("Apply Low Rate (20)")]
    public void ApplyLowRate()
    {
        ApplyRate(lowRate, "LOW");
    }

    [ContextMenu("Apply High Rate (200)")]
    public void ApplyHighRate()
    {
        ApplyRate(highRate, "HIGH");
    }

    [ContextMenu("Apply Current Quality Preset")]
    public void ApplyCurrentQualityPreset()
    {
        int qualityLevel = QualitySettings.GetQualityLevel();

        if (qualityLevel <= lowQualityMaxLevel)
        {
            ApplyRate(lowRate, "QUALITY LOW");
        }
        else
        {
            ApplyRate(defaultRate, "QUALITY NORMAL");
        }
    }

    [ContextMenu("Disable VFX (Quality Toggle OFF)")]
    public void DisableVfx()
    {
        if (visualEffect != null)
        {
            visualEffect.enabled = false;
            currentMode = "DISABLED";
        }
    }

    [ContextMenu("Enable VFX (Quality Toggle ON)")]
    public void EnableVfx()
    {
        if (visualEffect != null)
        {
            visualEffect.enabled = true;
            currentMode = "ENABLED";
        }
    }

    private void ApplyRate(float value, string mode)
    {
        RefreshPropertyState();

        if (!propertyReady)
        {
            Debug.LogError(
                $"DAY13: '{spawnRateName}' Float Exposed Property를 찾지 못했습니다.");
            return;
        }

        visualEffect.SetFloat(spawnRateName, value);
        currentRate = value;
        currentMode = mode;

        Debug.Log(
            $"DAY13 VFX Intensity -> {currentMode} / " +
            $"{spawnRateName} = {currentRate}");
    }

    private void RefreshPropertyState()
    {
        propertyReady =
            visualEffect != null &&
            !string.IsNullOrEmpty(spawnRateName) &&
            visualEffect.HasFloat(spawnRateName);
    }

    private void OnGUI()
    {
        if (!showDebugOverlay)
            return;

        GUI.Box(new Rect(12, 12, 340, 112), "DAY13 VFX Control / Performance");
        GUI.Label(new Rect(24, 38, 310, 22), "1 = Low Intensity (20)");
        GUI.Label(new Rect(24, 58, 310, 22), "2 = High Intensity (200)");
        GUI.Label(new Rect(24, 78, 310, 22),
            $"Mode: {currentMode}  |  SpawnRate: {currentRate:0}");
        GUI.Label(new Rect(24, 98, 310, 22),
            visualEffect != null
                ? $"Alive Particles: {visualEffect.aliveParticleCount}"
                : "VisualEffect: Missing");
    }
}
