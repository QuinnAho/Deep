using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

[RequireComponent(typeof(Rigidbody))]
public class SubmarineController : MonoBehaviour
{
    [Header("Propulsion")]
    [SerializeField] private float maxForwardThrust = 55f;
    [SerializeField] private float maxReverseThrust = 30f;
    [SerializeField] private float throttleResponse = 1.2f;

    [Header("Control Surfaces")]
    [SerializeField] private float rudderTorque = 4200f;
    [SerializeField] private float controlResponse = 2f;

    [Header("Vertical Control")]
    [SerializeField] private float verticalThrust = 1800f;

    private Rigidbody rb;
    private Keyboard keyboard;
    private Gamepad gamepad;

    private float throttle;
    private float targetThrottle;
    private float rudderTarget;
    private float rudderCommand;
    private float verticalTarget;
    private float verticalCommand;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        keyboard = Keyboard.current;
        gamepad = Gamepad.current;

        rb.useGravity = false;
        rb.linearDamping = 0.6f;
        rb.angularDamping = 0.8f;
    }

    private void Update()
    {
        if (keyboard == null) keyboard = Keyboard.current;
        if (gamepad == null) gamepad = Gamepad.current;

        bool hasKeyboard = keyboard != null;
        bool hasGamepad = gamepad != null;

        float leftStickY = hasGamepad ? gamepad.leftStick.y.ReadValue() : 0f;
        float leftStickX = hasGamepad ? gamepad.leftStick.x.ReadValue() : 0f;
        float triggerDiff = hasGamepad
            ? gamepad.rightTrigger.ReadValue() - gamepad.leftTrigger.ReadValue()
            : 0f;

        targetThrottle = ComposeInput(
            hasKeyboard ? keyboard.wKey : null,
            hasKeyboard ? keyboard.sKey : null,
            leftStickY);

        rudderTarget = ComposeInput(
            hasKeyboard ? keyboard.dKey : null,
            hasKeyboard ? keyboard.aKey : null,
            leftStickX);

        verticalTarget = ComposeInput(
            hasKeyboard ? keyboard.eKey : null,
            hasKeyboard ? keyboard.qKey : null,
            triggerDiff);
    }

    private void FixedUpdate()
    {
        throttle = Mathf.MoveTowards(throttle, targetThrottle, throttleResponse * Time.fixedDeltaTime);
        rudderCommand = Mathf.MoveTowards(rudderCommand, rudderTarget, controlResponse * Time.fixedDeltaTime);
        verticalCommand = Mathf.MoveTowards(verticalCommand, verticalTarget, controlResponse * Time.fixedDeltaTime);

        float thrustForce = throttle >= 0f ? throttle * maxForwardThrust : throttle * maxReverseThrust;
        rb.AddForce(transform.forward * thrustForce, ForceMode.Force);

        rb.AddTorque(transform.up * rudderCommand * rudderTorque * Time.fixedDeltaTime, ForceMode.Force);
        rb.AddForce(Vector3.up * verticalCommand * verticalThrust, ForceMode.Force);
    }

    private float ComposeInput(
        KeyControl positive,
        KeyControl negative,
        float analog = 0f)
    {
        float value = analog;
        if (positive != null && positive.isPressed) value += 1f;
        if (negative != null && negative.isPressed) value -= 1f;
        return Mathf.Clamp(value, -1f, 1f);
    }

    private void OnValidate()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
        }
    }
}
