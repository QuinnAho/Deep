using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[DisallowMultipleComponent]
public class SubmarinePhysics : MonoBehaviour
{
    [Header("Simple Physics")]
    [SerializeField] private float neutralLift = 9.81f;
    [SerializeField] private float linearDamping = 0.6f;
    [SerializeField] private float angularDamping = 0.8f;

    private Rigidbody rb;
    private float ballastLevel;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null) return;

        rb.useGravity = false;
        rb.linearDamping = linearDamping;
        rb.angularDamping = angularDamping;
    }

    private void FixedUpdate()
    {
        if (rb == null) return;
        if (Mathf.Approximately(ballastLevel, 0f)) return;

        rb.AddForce(Vector3.up * ballastLevel * neutralLift, ForceMode.Acceleration);
    }

    public void SetBallastLevel(float value)
    {
        ballastLevel = Mathf.Clamp(value, -1f, 1f);
    }
}
