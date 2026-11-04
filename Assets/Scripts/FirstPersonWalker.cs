using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonWalker : MonoBehaviour
{
    [Header("Spawn")]
    public Transform spawnPoint;

    [Header("Camera")]
    public Transform playerCamera;
    public float mouseSensitivity = 0.12f;

    [Header("Movement")]
    public float walkSpeed = 4f;
    public float runSpeed = 6f;
    public float jumpHeight = 1.2f;
    public float gravity = -9.81f;

    private CharacterController controller;
    private float lookUpDown;
    private float verticalSpeed;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (playerCamera == null)
        {
            Camera childCamera = GetComponentInChildren<Camera>();
            if (childCamera != null)
                playerCamera = childCamera.transform;
        }

        if (playerCamera == null)
        {
            Debug.LogError("FirstPersonWalker: Add a Camera as a child of Player.", this);
            enabled = false;
        }
    }

    private void Start()
    {
        if (spawnPoint != null)
        {
            controller.enabled = false;
            transform.position = spawnPoint.position;
            transform.rotation = Quaternion.Euler(0f, spawnPoint.eulerAngles.y, 0f);
            controller.enabled = true;
        }

        LockMouse();
    }

    private void Update()
    {
        if (Keyboard.current == null || Mouse.current == null)
            return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        if (Cursor.lockState != CursorLockMode.Locked)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
                LockMouse();
            return;
        }

        Vector2 mouse = Mouse.current.delta.ReadValue();
        transform.Rotate(Vector3.up * mouse.x * mouseSensitivity);
        lookUpDown = Mathf.Clamp(lookUpDown - mouse.y * mouseSensitivity, -85f, 85f);
        playerCamera.localRotation = Quaternion.Euler(lookUpDown, 0f, 0f);

        float x = 0f;
        float z = 0f;
        if (Keyboard.current.aKey.isPressed) x -= 1f;
        if (Keyboard.current.dKey.isPressed) x += 1f;
        if (Keyboard.current.sKey.isPressed) z -= 1f;
        if (Keyboard.current.wKey.isPressed) z += 1f;

        Vector3 movement = Vector3.ClampMagnitude(transform.right * x + transform.forward * z, 1f);
        bool running = Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;
        float speed = running ? runSpeed : walkSpeed;

        if (controller.isGrounded && verticalSpeed < 0f)
            verticalSpeed = -2f;

        if (controller.isGrounded && Keyboard.current.spaceKey.wasPressedThisFrame)
            verticalSpeed = Mathf.Sqrt(jumpHeight * -2f * gravity);

        verticalSpeed += gravity * Time.deltaTime;
        controller.Move((movement * speed + Vector3.up * verticalSpeed) * Time.deltaTime);
    }

    private void LockMouse()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
