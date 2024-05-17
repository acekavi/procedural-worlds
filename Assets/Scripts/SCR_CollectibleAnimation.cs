using UnityEngine;

public class CollectibleAnimation : MonoBehaviour
{
    public float jumpHeight = 1.0f;
    public float rotationSpeed = 90.0f; // Degrees per second
    private float baseY;

    private void Start()
    {
        baseY = transform.position.y;
    }

    private void Update()
    {
        // Jumping motion
        float newY = baseY + Mathf.PingPong(Time.time, jumpHeight);
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);

        // Rotating motion
        float rotationAmount = rotationSpeed * Time.deltaTime;
        transform.Rotate(Vector3.up, rotationAmount);
    }
}