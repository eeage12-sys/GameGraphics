using UnityEngine;

public class DAY09_HitPointDemo : MonoBehaviour
{
    public Camera demoCamera;
    public Transform target;
    public ParticleSystem hitEffect;

    [Tooltip("Play Mode에서 일정 간격으로 실제 Raycast Hit Point에 1회 재생합니다.")]
    public bool autoDemo = true;

    [Min(0.5f)]
    public float interval = 2.0f;

    float timer;

    void Start()
    {
        timer = 0.3f;
        if (hitEffect != null)
            hitEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    void Update()
    {
        if (!autoDemo)
            return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            PlayHitOnce();
            timer = interval;
        }
    }

    [ContextMenu("Play Hit Once")]
    public void PlayHitOnce()
    {
        if (demoCamera == null || target == null || hitEffect == null)
            return;

        Vector3 origin = demoCamera.transform.position;
        Vector3 direction = (target.position - origin).normalized;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, 100f))
        {
            hitEffect.transform.position = hit.point;

            // Particle Shape의 로컬 +Z가 표면 바깥쪽을 향하도록 맞춤.
            hitEffect.transform.rotation = Quaternion.LookRotation(hit.normal);

            hitEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            hitEffect.Play(true);

            Debug.Log($"DAY09 Hit confirmed once: {hit.collider.name} / Point = {hit.point}");
        }
    }
}
