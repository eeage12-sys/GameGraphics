using UnityEngine;

public class DAY09_LocalWorldMover : MonoBehaviour
{
    public float distance = 1.5f;
    public float speed = 1.25f;

    Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        float x = Mathf.Sin(Time.time * speed) * distance;
        transform.position = startPosition + Vector3.right * x;
    }
}
