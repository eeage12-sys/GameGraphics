using UnityEngine;

public class DAY08_DemoRotator : MonoBehaviour
{
    public float degreesPerSecond = 18f;

    void Update()
    {
        transform.Rotate(0f, degreesPerSecond * Time.deltaTime, 0f, Space.World);
    }
}
