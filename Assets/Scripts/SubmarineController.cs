using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

[RequireComponent(typeof(Rigidbody), typeof(SubmarinePhysics))]
public class SubmarineController : MonoBehaviour
{
    [Header("Propulsion")]
    [SerializeField] private float maxForwardThrust = 55f;
    [SerializeField] private float maxReverseThrust = 30f;
    [SerializeField] private float throttleResponse = 1.2f;

    [Header("Control Surfaces")]
    [SerializeField] private float rudderTorque = 4200f;
    [SerializeField] private float rollTorque = 2200f;
    [SerializeField] private float controlResponse = 2f;

    [Header("Ballast")]
    [SerializeField] private float ballastAdjustRate = 0.4f;
    [SerializeField] private float mouseBallastRate = 2.5f;

    private Rigidbody rb;
    private SubmarinePhysics physics;
    private Keyboard keyboard;
    private Gamepad gamepad;
    private Mouse mouse;

    private float throttle;
    private float targetThrottle;
    private float rudderTarget;
    private float rudderCommand;
    private float rollTarget;
    private float rollCommand;
    private float ballastLevel;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        physics = GetComponent<SubmarinePhysics>();
        keyboard = Keyboard.current;
        gamepad = Gamepad.current;
        mouse = Mouse.current;

        rb.useGravity = false;
        rb.linearDamping = 0.6f;
        rb.angularDamping = 0.8f;
    }

    private void Update()
    {
        if (keyboard == null) keyboard = Keyboard.current;
        if (gamepad == null) gamepad = Gamepad.current;
        if (mouse == null) mouse = Mouse.current;

        float leftStickY = gamepad != null ? gamepad.leftStick.y.ReadValue() : 0f;
        float leftStickX = gamepad != null ? gamepad.leftStick.x.ReadValue() : 0f;
        float rightStickX = gamepad != null ? gamepad.rightStick.x.ReadValue() : 0f;
        float triggerDiff = gamepad != null
            ? gamepad.rightTrigger.ReadValue() - gamepad.leftTrigger.ReadValue()
            : 0f;

        targetThrottle = ComposeInput(keyboard?.wKey, keyboard?.sKey, leftStickY);
        rudderTarget = ComposeInput(keyboard?.dKey, keyboard?.aKey, leftStickX);
        rollTarget = ComposeInput(keyboard?.eKey, keyboard?.qKey, rightStickX);

        float ballastDelta = ComposeInput(keyboard?.spaceKey, keyboard?.leftCtrlKey, triggerDiff);
        float targetBallast = Mathf.Clamp(ballastLevel + ballastDelta * ballastAdjustRate * Time.deltaTime, -1f, 1f);
        float adjustRate = ballastAdjustRate;
        if (mouse != null)
        {
            if (mouse.leftButton.isPressed)
            {
                targetBallast = 1f;
                adjustRate = mouseBallastRate;
            }
            else if (mouse.rightButton.isPressed)
            {
                targetBallast = -1f;
                adjustRate = mouseBallastRate;
            }
        }
        ballastLevel = Mathf.MoveTowards(ballastLevel, targetBallast, adjustRate * Time.deltaTime);
    }

    private void FixedUpdate()
    {
        throttle = Mathf.MoveTowards(throttle, targetThrottle, throttleResponse * Time.fixedDeltaTime);
        rudderCommand = Mathf.MoveTowards(rudderCommand, rudderTarget, controlResponse * Time.fixedDeltaTime);
        rollCommand = Mathf.MoveTowards(rollCommand, rollTarget, controlResponse * Time.fixedDeltaTime);

        float thrustForce = throttle >= 0f ? throttle * maxForwardThrust : throttle * maxReverseThrust;
        rb.AddForce(transform.forward * thrustForce, ForceMode.Force);

        rb.AddTorque(transform.up * rudderCommand * rudderTorque * Time.fixedDeltaTime, ForceMode.Force);
        rb.AddTorque(transform.forward * rollCommand * rollTorque * Time.fixedDeltaTime, ForceMode.Force);

        physics.SetBallastLevel(ballastLevel);
    }

    private float ComposeInput(KeyControl positive, KeyControl negative, float analog = 0f)
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
