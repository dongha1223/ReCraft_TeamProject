using UnityEngine;

public class BoxAccelerate : MonoBehaviour
{
    public float acceleration = 5f;
    private float velocity = 0f;

    void Update()
    {
        velocity += acceleration * Time.deltaTime;
        transform.position += Vector3.right * velocity * Time.deltaTime;
    }
}
