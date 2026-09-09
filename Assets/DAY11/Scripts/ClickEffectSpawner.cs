using UnityEngine;
using UnityEngine.InputSystem;

public class ClickEffectSpawner : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private ParticleSystem effectPrefab;
    [SerializeField] private LayerMask groundMask;

    private Vector2 pointerPosition;

    public void OnPoint(InputValue value)
    {
        pointerPosition = value.Get<Vector2>();
    }

    public void OnClick(InputValue value)
    {
        if (!value.isPressed)
        {
            return;
        }

        Ray ray = targetCamera.ScreenPointToRay(pointerPosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundMask))
        {
            ParticleSystem effect = Instantiate(
                effectPrefab,
                hit.point,
                Quaternion.identity
            );

            effect.Play();

            Destroy(
                effect.gameObject,
                effect.main.duration + effect.main.startLifetime.constantMax
            );
        }
    }
}
