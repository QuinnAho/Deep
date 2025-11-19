using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class DebugSpectatorController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float sprintMultiplier = 3f;
    [SerializeField] private float verticalSpeed = 6f;
    [SerializeField] private float speedStep = 1.5f;
    [SerializeField] private float minSpeed = 1f;
    [SerializeField] private float maxSpeed = 50f;

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 120f;

    private CharacterController characterController;
    private Camera spectatorCamera;
    private Keyboard keyboard;
    private Mouse mouse;

    private float pitch;
    private bool lookActive;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        spectatorCamera = GetComponentInChildren<Camera>();
    }

    private void Update()
    {
        if (keyboard == null) keyboard = Keyboard.current;
        if (mouse == null) mouse = Mouse.current;
        if (keyboard == null || mouse == null) return;

        HandleLookToggle();
        HandleSpeedAdjust();
        if (lookActive)
        {
            HandleLook();
        }
        HandleMovement();
    }

    private void HandleLook()
    {
        if (spectatorCamera == null) return;

        Vector2 lookDelta = mouse.delta.ReadValue() * mouseSensitivity * Time.deltaTime;
        pitch = Mathf.Clamp(pitch - lookDelta.y, -89f, 89f);
        spectatorCamera.transform.localEulerAngles = new Vector3(pitch, 0f, 0f);
        transform.Rotate(Vector3.up, lookDelta.x);
    }

    private void HandleMovement()
    {
        Vector3 move = Vector3.zero;

        if (keyboard.wKey.isPressed) move += transform.forward;
        if (keyboard.sKey.isPressed) move -= transform.forward;
        if (keyboard.dKey.isPressed) move += transform.right;
        if (keyboard.aKey.isPressed) move -= transform.right;

        float vertical = 0f;
        if (keyboard.eKey.isPressed) vertical += 1f;
        if (keyboard.qKey.isPressed) vertical -= 1f;

        Vector3 velocity = move.normalized * moveSpeed;
        if (keyboard.leftShiftKey.isPressed)
        {
            velocity *= sprintMultiplier;
        }

        velocity += Vector3.up * vertical * verticalSpeed;
        characterController.Move(velocity * Time.deltaTime);
    }

    private void HandleLookToggle()
    {
        if (mouse.rightButton.wasPressedThisFrame)
        {
            SetCursorLock(true);
            lookActive = true;
        }

        if (mouse.rightButton.wasReleasedThisFrame)
        {
            SetCursorLock(false);
            lookActive = false;
        }
    }

    private void HandleSpeedAdjust()
    {
        Vector2 scroll = mouse.scroll.ReadValue();
        if (Mathf.Abs(scroll.y) > 0.01f)
        {
            float delta = Mathf.Sign(scroll.y) * speedStep;
            moveSpeed = Mathf.Clamp(moveSpeed + delta, minSpeed, maxSpeed);
        }
    }

    private void SetCursorLock(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody rb = hit.collider.attachedRigidbody;

        if (rb == null || rb.isKinematic)
            return;

        // Don't push objects below us
        if (hit.moveDirection.y < -0.3f)
            return;

        // Apply push force
        Vector3 pushDir = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);
        rb.AddForce(pushDir * moveSpeed * 0.5f, ForceMode.Impulse);
    }
}
