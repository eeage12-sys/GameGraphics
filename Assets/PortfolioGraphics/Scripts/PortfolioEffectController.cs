using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.VFX;

/// <summary>
/// Connects a confirmed player input event to one HitSpark instance and
/// controls the exposed SpawnRate property on the portfolio VFX Graph.
/// </summary>
public sealed class PortfolioEffectController : MonoBehaviour
{
    [Header("Hit effect")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private LayerMask impactMask;
    [SerializeField] private ParticleSystem hitEffectPrefab;
    [SerializeField, Min(0f)] private float minimumSpawnInterval = 0.08f;

    [Header("VFX Graph")]
    [SerializeField] private VisualEffect manaVfx;
    [SerializeField] private string spawnRateProperty = "SpawnRate";
    [SerializeField, Min(0f)] private float lowSpawnRate = 20f;
    [SerializeField, Min(0f)] private float defaultSpawnRate = 80f;
    [SerializeField, Min(0f)] private float highSpawnRate = 200f;

    private int lastSpawnFrame = -1;
    private float nextAllowedSpawnTime;
    private float currentSpawnRate;

    public float CurrentSpawnRate => currentSpawnRate;
    public bool SpawnRatePropertyReady =>
        manaVfx != null && manaVfx.HasFloat(spawnRateProperty);

    public void Configure(
        Camera camera,
        LayerMask mask,
        ParticleSystem hitPrefab,
        VisualEffect visualEffect)
    {
        targetCamera = camera;
        impactMask = mask;
        hitEffectPrefab = hitPrefab;
        manaVfx = visualEffect;
    }

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        ApplySpawnRate(defaultSpawnRate);
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard != null)
        {
            if (keyboard.digit1Key.wasPressedThisFrame)
                ApplySpawnRate(lowSpawnRate);

            if (keyboard.digit2Key.wasPressedThisFrame)
                ApplySpawnRate(highSpawnRate);

            if (keyboard.digit3Key.wasPressedThisFrame)
                ApplySpawnRate(defaultSpawnRate);
        }

        Mouse mouse = Mouse.current;

        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            TrySpawnHitEffect(mouse.position.ReadValue());
    }

    private void TrySpawnHitEffect(Vector2 screenPosition)
    {
        // One physical press is accepted once. The frame and short time guards
        // also prevent duplicate creation if input is delivered more than once.
        if (Time.frameCount == lastSpawnFrame ||
            Time.unscaledTime < nextAllowedSpawnTime ||
            targetCamera == null ||
            hitEffectPrefab == null)
        {
            return;
        }

        Ray ray = targetCamera.ScreenPointToRay(screenPosition);

        if (!Physics.Raycast(
                ray,
                out RaycastHit hit,
                100f,
                impactMask,
                QueryTriggerInteraction.Ignore))
        {
            return;
        }

        lastSpawnFrame = Time.frameCount;
        nextAllowedSpawnTime = Time.unscaledTime + minimumSpawnInterval;

        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
        ParticleSystem instance = Instantiate(
            hitEffectPrefab,
            hit.point + hit.normal * 0.02f,
            rotation);

        instance.Play(true);

        ParticleSystem.MainModule main = instance.main;
        float cleanupDelay = main.duration + main.startLifetime.constantMax + 0.25f;
        Destroy(instance.gameObject, cleanupDelay);
    }

    private void ApplySpawnRate(float value)
    {
        currentSpawnRate = value;

        if (manaVfx == null)
            return;

        if (manaVfx.HasFloat(spawnRateProperty))
        {
            manaVfx.SetFloat(spawnRateProperty, value);
            return;
        }

        // This fallback keeps the comparison visible before the Blackboard
        // property is connected. Final validation still requires SpawnRate.
        manaVfx.playRate = Mathf.Max(0.05f, value / defaultSpawnRate);
    }
}
