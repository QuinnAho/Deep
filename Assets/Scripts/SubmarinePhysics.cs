using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SubmarinePhysics : MonoBehaviour
{
    [Header("Buoyancy Settings")]
    [SerializeField] private float waterLevel = 0f;
    [SerializeField] private float waterDrag = 3f;
    [SerializeField] private float waterAngularDrag = 1f;

    [Header("Submarine Properties")]
    [SerializeField] private float displacedVolume = 9.5f; // cubic meters
    [SerializeField] private float waterDensity = 1027f;   // kg per cubic meter
    [SerializeField] private float trimDepth = 4f;
    [SerializeField] private float ballastForce = 2.5f;

    [Header("Hydrodynamics")]
    [SerializeField] private float hydrodynamicDrag = 0.8f;
    [SerializeField] private float hydrodynamicAngularDrag = 0.4f;

    [Header("Depth Settings")]
    [SerializeField] private float maxDepth = 100f;
    [SerializeField] private float crushDepth = 150f;
    [SerializeField] private bool enableDepthLimit = false;

    [Header("Current Settings")]
    [SerializeField] private Vector3 oceanCurrent = Vector3.zero;
    [SerializeField] private float currentStrength = 1f;

    private Rigidbody rb;
    private float surfaceDrag = 0f;
    private float surfaceAngularDrag = 0.05f;
    private float displacedMass = 1f;
    private float ballastLevel;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        surfaceDrag = rb.linearDamping;
        surfaceAngularDrag = rb.angularDamping;
        displacedMass = displacedVolume * waterDensity;
    }

    private void FixedUpdate()
    {
        ApplyBuoyancy();
        ApplyWaterResistance();
        ApplyHydrodynamics();
        ApplyOceanCurrent();
        CheckDepthLimits();
    }

    private void ApplyBuoyancy()
    {
        float depth = Mathf.Max(0f, waterLevel - transform.position.y);

        if (depth > 0) // Submarine is underwater
        {
            // Neutral buoyancy derived from displaced volume + ballast compensation
            float normalizedDepth = trimDepth <= 0f ? 1f : Mathf.Clamp01(depth / trimDepth);
            float gravity = Physics.gravity.magnitude;
            float buoyancyAcceleration = (displacedMass * gravity * normalizedDepth) / Mathf.Max(rb.mass, 0.01f);
            float ballastAcceleration = ballastLevel * ballastForce;

            rb.AddForce(Vector3.up * (buoyancyAcceleration + ballastAcceleration), ForceMode.Acceleration);

            // Apply water drag
            rb.linearDamping = waterDrag;
            rb.angularDamping = waterAngularDrag;
        }
        else // Submarine is above water
        {
            // Reduced drag when above water
            rb.linearDamping = surfaceDrag;
            rb.angularDamping = surfaceAngularDrag;
        }
    }

    private void ApplyWaterResistance()
    {
        // Additional velocity-based drag for more realistic water resistance
        float depth = waterLevel - transform.position.y;
        if (depth <= 0f) return;

        Vector3 velocity = rb.linearVelocity;
        if (velocity.sqrMagnitude > 0.0001f)
        {
            Vector3 drag = -velocity * velocity.magnitude * hydrodynamicDrag;
            rb.AddForce(drag / Mathf.Max(rb.mass, 0.01f), ForceMode.Acceleration);
        }
    }

    private void ApplyHydrodynamics()
    {
        float depth = waterLevel - transform.position.y;
        if (depth <= 0f) return;

        Vector3 angularVelocity = rb.angularVelocity;
        if (angularVelocity.sqrMagnitude > 0.0001f)
        {
            Vector3 angDrag = -angularVelocity * angularVelocity.magnitude * hydrodynamicAngularDrag;
            rb.AddTorque(angDrag, ForceMode.Acceleration);
        }

    }

    private void ApplyOceanCurrent()
    {
        float depth = waterLevel - transform.position.y;
        if (depth > 0 && oceanCurrent != Vector3.zero)
        {
            rb.AddForce(oceanCurrent * currentStrength, ForceMode.Acceleration);
        }
    }

    private void CheckDepthLimits()
    {
        if (!enableDepthLimit) return;

        float currentDepth = waterLevel - transform.position.y;

        // Warn if approaching max depth
        if (currentDepth > maxDepth)
        {
            Debug.LogWarning($"Warning: Approaching maximum safe depth! Current: {currentDepth:F1}m, Max: {maxDepth}m");
        }

        // Crush if too deep
        if (currentDepth > crushDepth)
        {
            Debug.LogError("Submarine crushed by pressure!");
            // You could trigger destruction or damage here
        }

        // Limit descent past max depth
        if (currentDepth > maxDepth && rb.linearVelocity.y < 0)
        {
            Vector3 velocity = rb.linearVelocity;
            velocity.y = Mathf.Max(velocity.y, -1f); // Slow down descent
            rb.linearVelocity = velocity;
        }
    }

    public void SetBallastLevel(float value)
    {
        ballastLevel = Mathf.Clamp(value, -1f, 1f);
    }

    public float GetBallastLevel()
    {
        return ballastLevel;
    }

    public float GetCurrentDepth()
    {
        return waterLevel - transform.position.y;
    }

    public bool IsUnderwater()
    {
        return transform.position.y < waterLevel;
    }

    private void OnDrawGizmosSelected()
    {
        // Draw water level
        Gizmos.color = new Color(0, 0.5f, 1f, 0.3f);
        Gizmos.DrawCube(new Vector3(transform.position.x, waterLevel, transform.position.z), new Vector3(20, 0.1f, 20));

        // Draw max depth line
        if (enableDepthLimit)
        {
            Gizmos.color = Color.yellow;
            float maxDepthY = waterLevel - maxDepth;
            Gizmos.DrawCube(new Vector3(transform.position.x, maxDepthY, transform.position.z), new Vector3(20, 0.1f, 20));

            // Draw crush depth line
            Gizmos.color = Color.red;
            float crushDepthY = waterLevel - crushDepth;
            Gizmos.DrawCube(new Vector3(transform.position.x, crushDepthY, transform.position.z), new Vector3(20, 0.1f, 20));
        }
    }
}
