using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class PlayerManager : MonoBehaviour
{
    [Header("Player Settings")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private GameObject playerCapsule;
    [SerializeField] private float speed = 5.0f;
    [SerializeField] private float jumpForce = 2.0f;
    [SerializeField] private float rotationSpeed = 2f;
    [SerializeField] private float verticalLookSpeed = 2f;
    [SerializeField] private float verticalRotationLimit = 80f; // Adjust as needed

    [Header("Player UI")]
    [SerializeField] Image healthBar;
    [SerializeField] float healthPoints = 100f;
    [SerializeField] readonly float maxHealthPoints = 100f;
    [SerializeField] TextMeshProUGUI feedbackText;
    [SerializeField] TextMeshProUGUI PointCounter;

    [Header("Player Shooting")]
    [SerializeField] private Bullet Bullet;
    [SerializeField] private Transform bulletSpawnPoint;

    #region Player Settings
    private float verticalRotation = 0f;
    private Rigidbody rb;
    float currentSpeed;
    float currentJumpForce;
    private int points = 0;
    #endregion

    void Start()
    {
        // Get the CharacterController component
        rb = GetComponent<Rigidbody>();
        healthPoints = maxHealthPoints;
        currentSpeed = speed;
        currentJumpForce = jumpForce;

        // Lock and hide the cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Player movement
        Move();
        if (Input.GetButtonDown("Jump"))
        {
            Jump();
        }
        // Shoot bullet
        if (Input.GetMouseButtonDown(0))
        {
            ShootBullet();
        }

        // Player rotation (horizontal)
        RotatePlayer();

        // Camera rotation (vertical)
        RotateCamera();
    }

    #region Player Navgiation
    private void Move()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 movement = new(horizontal, 0f, vertical);
        movement = transform.TransformDirection(movement);
        movement.Normalize();

        rb.MovePosition(rb.position + movement * speed * Time.deltaTime);
    }

    private void Jump()
    {
        if (IsGrounded())
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    private bool IsGrounded()
    {
        float distanceToGround = playerCapsule.GetComponent<Collider>().bounds.extents.y;
        return Physics.Raycast(transform.position, -Vector3.up, distanceToGround + 0.1f);
    }

    private void RotatePlayer()
    {
        // Horizontal rotation
        float horizontalRotation = Input.GetAxis("Mouse X") * rotationSpeed;
        transform.Rotate(0f, horizontalRotation, 0f);
    }

    private void RotateCamera()
    {
        // Calculate the new rotation
        float rotationX = Input.GetAxis("Mouse Y") * verticalLookSpeed;
        verticalRotation -= rotationX;

        // Clamp the vertical rotation
        verticalRotation = Mathf.Clamp(verticalRotation, -verticalRotationLimit, verticalRotationLimit);

        // Apply the rotation to the camera
        playerCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0, 0);
    }
    #endregion

    #region Player Powerups
    public void DoubleSpeed()
    {
        if (currentSpeed > speed * 2)
        {
            return;
        }
        else
        {
            StartCoroutine(DoubleSpeedForSeconds(10));
        }
    }

    private IEnumerator DoubleSpeedForSeconds(float seconds)
    {
        speed *= 2;

        yield return new WaitForSeconds(seconds);

        speed /= 2;
    }

    public void DoubleJumpForce()
    {
        if (currentJumpForce > jumpForce * 2)
        {
            return;
        }
        else
        {
            StartCoroutine(DoubleJumpForceForSeconds(5));
        }
    }

    private IEnumerator DoubleJumpForceForSeconds(float seconds)
    {
        jumpForce *= 2;

        yield return new WaitForSeconds(seconds);

        jumpForce /= 2;
    }

    public void SlowMotion()
    {
        StartCoroutine(SlowMoForSeconds(5));
    }

    private IEnumerator SlowMoForSeconds(float seconds)
    {
        speed /= 2;

        yield return new WaitForSeconds(seconds);

        speed *= 2;
    }
    #endregion

    #region Player UI
    public void TakeDamage(float damage)
    {
        healthPoints -= damage;
        if (healthPoints < 0f)
        {
            Application.Quit();
        }
        healthBar.fillAmount = healthPoints / maxHealthPoints;
    }

    public void Heal(float healAmount)
    {
        healthPoints += healAmount;
        healthPoints = Mathf.Clamp(healthPoints, 0f, maxHealthPoints);
        if (healthPoints > maxHealthPoints)
        {
            healthPoints = maxHealthPoints;
        }
        healthBar.fillAmount = healthPoints / maxHealthPoints;
    }

    public void AddPoints(int pointsToAdd)
    {
        points += pointsToAdd;
        PointCounter.text = points.ToString();
    }

    public void ShowFeedback(string message, float duration)
    {
        feedbackText.text = message;
        StartCoroutine(ClearFeedback(duration));
    }

    IEnumerator ClearFeedback(float duration)
    {
        yield return new WaitForSeconds(duration);
        feedbackText.text = "";
    }
    #endregion

    #region Player Shooting
    public void ShootBullet()
    {
        // Instantiate bullet
        Bullet bullet = Instantiate(Bullet, bulletSpawnPoint.position, Quaternion.identity);

        // Apply force to bullet
        if (bullet.TryGetComponent<Rigidbody>(out var bulletRb))
        {
            bulletRb.velocity = playerCamera.transform.forward * bullet.Speed;
        }
    }
    #endregion
}
