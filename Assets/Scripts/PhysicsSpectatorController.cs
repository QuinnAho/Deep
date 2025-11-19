using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PhysicsSpectatorController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveForce = 50f;
    [SerializeField] private float sprintMultiplier = 3f;
    [SerializeField] private float verticalForce = 40f;
    [SerializeField] private float speedStep = 5f;
    [SerializeField] private float minForce = 10f;
    [SerializeField] private float maxForce = 200f;

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 120f;

    [Header("Physics")]
    [SerializeField] private float drag = 5f;
    [SerializeField] private float angularDrag = 5f;

    private Rigidbody rb;
    private Camera spectatorCamera;
    private Keyboard keyboard;
    private Mouse mouse;

    private float pitch;
    private bool lookActive;

    public Rigidbody Rigidbody => rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        spectatorCamera = GetComponentInChildren<Camera>();

        // Configure rigidbody for underwater physics
        rb.useGravity = false;
        rb.linearDamping = drag;
        rb.angularDamping = angularDrag;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints = RigidbodyConstraints.FreezeRotation; // Prevent tumbling
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
    }

    private void FixedUpdate()
    {
        if (keyboard == null) return;
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
        Vector3 moveDir = Vector3.zero;

        if (keyboard.wKey.isPressed) moveDir += transform.forward;
        if (keyboard.sKey.isPressed) moveDir -= transform.forward;
        if (keyboard.dKey.isPressed) moveDir += transform.right;
        if (keyboard.aKey.isPressed) moveDir -= transform.right;

        float vertical = 0f;
        if (keyboard.eKey.isPressed) vertical += 1f;
        if (keyboard.qKey.isPressed) vertical -= 1f;

        Vector3 force = moveDir.normalized * moveForce;
        if (keyboard.leftShiftKey.isPressed)
        {
            force *= sprintMultiplier;
        }

        force += Vector3.up * vertical * verticalForce;

        if (force.sqrMagnitude > 0.01f)
        {
            rb.AddForce(force, ForceMode.Force);
        }
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
            moveForce = Mathf.Clamp(moveForce + delta, minForce, maxForce);
        }
    }

    private void SetCursorLock(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
