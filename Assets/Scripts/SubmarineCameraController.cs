using UnityEngine;
using UnityEngine.InputSystem;

public class SubmarineCameraController : MonoBehaviour
{
    [Header("Camera References")]
    [SerializeField] private Camera firstPersonCamera;
    [SerializeField] private Camera thirdPersonCamera;

    [Header("Third Person Settings")]
    [SerializeField] private float followSpeed = 5f;
    [SerializeField] private float rotationSpeed = 3f;

    [Header("Camera Shake")]
    [SerializeField] private float shakeAmount = 0.1f;
    [SerializeField] private bool enableShake = false;

    private bool isFirstPerson;
    private Keyboard keyboard;
    private Transform submarine;
    private Vector3 thirdPersonOffset;
    private Quaternion thirdPersonRotationOffset;
    private AudioListener firstPersonListener;
    private AudioListener thirdPersonListener;

    private void Awake()
    {
        keyboard = Keyboard.current;
        submarine = transform;

        if (thirdPersonCamera != null)
        {
            thirdPersonOffset = thirdPersonCamera.transform.localPosition;
            thirdPersonRotationOffset = thirdPersonCamera.transform.localRotation;
        }

        firstPersonListener = firstPersonCamera != null ? firstPersonCamera.GetComponent<AudioListener>() : null;
        thirdPersonListener = thirdPersonCamera != null ? thirdPersonCamera.GetComponent<AudioListener>() : null;
    }

    private void Start()
    {
        SwitchToThirdPerson();
    }

    private void Update()
    {
        HandleCameraSwitch();
    }

    private void LateUpdate()
    {
        if (!isFirstPerson && thirdPersonCamera != null)
        {
            UpdateThirdPersonCamera();
        }

        if (enableShake)
        {
            ApplyCameraShake();
        }
    }

    private void HandleCameraSwitch()
    {
        if (keyboard == null) keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.cKey.wasPressedThisFrame)
        {
            if (isFirstPerson)
                SwitchToThirdPerson();
            else
                SwitchToFirstPerson();
        }
    }

    private void SwitchToFirstPerson()
    {
        isFirstPerson = true;
        if (firstPersonCamera != null) firstPersonCamera.enabled = true;
        if (thirdPersonCamera != null) thirdPersonCamera.enabled = false;
        UpdateAudioState();
    }

    private void SwitchToThirdPerson()
    {
        isFirstPerson = false;
        if (firstPersonCamera != null) firstPersonCamera.enabled = false;
        if (thirdPersonCamera != null) thirdPersonCamera.enabled = true;
        UpdateAudioState();
    }

    private void UpdateThirdPersonCamera()
    {
        Vector3 desiredPosition = submarine.position + submarine.TransformDirection(thirdPersonOffset);
        thirdPersonCamera.transform.position = Vector3.Lerp(
            thirdPersonCamera.transform.position,
            desiredPosition,
            followSpeed * Time.deltaTime
        );

        Quaternion desiredRotation = submarine.rotation * thirdPersonRotationOffset;
        thirdPersonCamera.transform.rotation = Quaternion.Slerp(
            thirdPersonCamera.transform.rotation,
            desiredRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    private void ApplyCameraShake()
    {
        Camera activeCamera = isFirstPerson ? firstPersonCamera : thirdPersonCamera;
        if (activeCamera != null)
        {
            Vector3 shake = Random.insideUnitSphere * shakeAmount * Time.deltaTime;
            activeCamera.transform.localPosition += shake;
        }
    }

    private void UpdateAudioState()
    {
        if (firstPersonListener != null)
        {
            firstPersonListener.enabled = isFirstPerson;
        }

        if (thirdPersonListener != null)
        {
            thirdPersonListener.enabled = !isFirstPerson;
        }
    }

    public void EnableCameraShake(bool enable)
    {
        enableShake = enable;
    }

    public bool IsFirstPerson()
    {
        return isFirstPerson;
    }
}
