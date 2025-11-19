using UnityEngine;

namespace Rope
{
    public class UnderwaterRopeController : MonoBehaviour
    {
        [Header("Rope Configuration")]
        [SerializeField] private int segmentCount = 20;
        [SerializeField] private float segmentLength = 0.5f;
        [SerializeField] private float ropeWidth = 0.05f;

        [Header("Physics Settings")]
        [SerializeField] private float segmentMass = 0.1f;
        [SerializeField] private float drag = 2f;
        [SerializeField] private float angularDrag = 1f;
        [SerializeField] private float jointDamper = 10f;
        [SerializeField] private float jointSpring = 100f;

        [Header("Attachment Points")]
        [SerializeField] private Rigidbody startObject;
        [SerializeField] private Rigidbody endObject;

        [Header("Rendering")]
        [SerializeField] private Material ropeMaterial;
        [SerializeField] private int splineSmoothing = 4;

        private GameObject[] segments;
        private LineRenderer lineRenderer;
        private CatmullRomSpline splineX;
        private CatmullRomSpline splineY;
        private CatmullRomSpline splineZ;
        private float[] xPositions;
        private float[] yPositions;
        private float[] zPositions;

        public Rigidbody StartObject
        {
            get => startObject;
            set => startObject = value;
        }

        public Rigidbody EndObject
        {
            get => endObject;
            set => endObject = value;
        }

        public int SegmentCount => segmentCount;
        public float SegmentLength => segmentLength;

        private void Start()
        {
            if (segments == null || segments.Length == 0)
            {
                GenerateRope();
            }
        }

        public void GenerateRope()
        {
            ClearExistingRope();

            if (startObject == null || endObject == null)
            {
                Debug.LogError("UnderwaterRopeController: Both start and end objects must be assigned!");
                return;
            }

            segments = new GameObject[segmentCount];

            SetupLineRenderer();
            CreateSegments();
            SetupSplines();
            AttachToEndObjects();
        }

        private void ClearExistingRope()
        {
            if (segments != null)
            {
                foreach (var segment in segments)
                {
                    if (segment != null)
                    {
                        if (Application.isPlaying)
                            Destroy(segment);
                        else
                            DestroyImmediate(segment);
                    }
                }
            }
            segments = null;
        }

        private void SetupLineRenderer()
        {
            lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null)
            {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
            }

            lineRenderer.positionCount = (segmentCount - 1) * splineSmoothing + 1;
            lineRenderer.startWidth = ropeWidth;
            lineRenderer.endWidth = ropeWidth;

            if (ropeMaterial != null)
            {
                lineRenderer.material = ropeMaterial;
            }
        }

        private void CreateSegments()
        {
            // Calculate direction and spacing between the two objects
            Vector3 startPos = startObject.position;
            Vector3 endPos = endObject.position;
            Vector3 direction = (endPos - startPos).normalized;
            float totalDistance = Vector3.Distance(startPos, endPos);
            float spacing = totalDistance / (segmentCount + 1); // +1 to leave room for attachments

            for (int i = 0; i < segmentCount; i++)
            {
                // Position segments evenly between start and end
                Vector3 segmentPos = startPos + direction * (spacing * (i + 1));

                GameObject segment = new GameObject($"RopeSegment_{i}");
                segment.transform.SetParent(transform);
                segment.transform.position = segmentPos;

                // Add sphere collider for physics
                SphereCollider collider = segment.AddComponent<SphereCollider>();
                collider.radius = ropeWidth * 2f;

                // Add rigidbody with zero gravity underwater settings
                Rigidbody rb = segment.AddComponent<Rigidbody>();
                rb.mass = segmentMass;
                rb.useGravity = false; // Zero gravity for underwater
                rb.linearDamping = drag;
                rb.angularDamping = angularDrag;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

                // Add configurable joint to connect to previous segment
                if (i > 0)
                {
                    ConfigurableJoint joint = segment.AddComponent<ConfigurableJoint>();
                    joint.connectedBody = segments[i - 1].GetComponent<Rigidbody>();
                    ConfigureJoint(joint);
                }

                segments[i] = segment;
            }
        }

        private void ConfigureJoint(ConfigurableJoint joint)
        {
            // Lock all rotation - rope segments don't rotate independently
            joint.angularXMotion = ConfigurableJointMotion.Locked;
            joint.angularYMotion = ConfigurableJointMotion.Locked;
            joint.angularZMotion = ConfigurableJointMotion.Locked;

            // Limit linear motion to segment length
            joint.xMotion = ConfigurableJointMotion.Limited;
            joint.yMotion = ConfigurableJointMotion.Limited;
            joint.zMotion = ConfigurableJointMotion.Limited;

            // Set up linear limit
            SoftJointLimit linearLimit = new SoftJointLimit();
            linearLimit.limit = segmentLength;
            linearLimit.bounciness = 0f;
            linearLimit.contactDistance = 0.01f;
            joint.linearLimit = linearLimit;

            // Add spring/damper for smoother rope behavior
            SoftJointLimitSpring limitSpring = new SoftJointLimitSpring();
            limitSpring.spring = jointSpring;
            limitSpring.damper = jointDamper;
            joint.linearLimitSpring = limitSpring;

            // Configure anchors
            joint.autoConfigureConnectedAnchor = false;
            joint.anchor = Vector3.zero;
            joint.connectedAnchor = Vector3.zero;
        }

        private void SetupSplines()
        {
            xPositions = new float[segmentCount];
            yPositions = new float[segmentCount];
            zPositions = new float[segmentCount];

            splineX = new CatmullRomSpline(xPositions);
            splineY = new CatmullRomSpline(yPositions);
            splineZ = new CatmullRomSpline(zPositions);
        }

        private void AttachToEndObjects()
        {
            if (segments == null || segments.Length == 0)
                return;

            // Attach first segment to start object
            GameObject firstSegment = segments[0];
            ConfigurableJoint startJoint = firstSegment.AddComponent<ConfigurableJoint>();
            startJoint.connectedBody = startObject;
            ConfigureJoint(startJoint);

            // Attach last segment to end object
            GameObject lastSegment = segments[segmentCount - 1];
            ConfigurableJoint endJoint = lastSegment.AddComponent<ConfigurableJoint>();
            endJoint.connectedBody = endObject;
            ConfigureJoint(endJoint);
        }


        private void LateUpdate()
        {
            if (segments == null || lineRenderer == null)
                return;

            // Update spline positions from segment positions
            for (int i = 0; i < segmentCount; i++)
            {
                Vector3 position = segments[i].transform.position;
                xPositions[i] = position.x;
                yPositions[i] = position.y;
                zPositions[i] = position.z;
            }

            // Render smooth rope using Catmull-Rom spline interpolation
            int totalPoints = (segmentCount - 1) * splineSmoothing + 1;
            for (int i = 0; i < totalPoints; i++)
            {
                float t = i / (float)splineSmoothing;
                lineRenderer.SetPosition(i, new Vector3(
                    splineX.GetValue(t),
                    splineY.GetValue(t),
                    splineZ.GetValue(t)));
            }
        }

        private void OnDestroy()
        {
            ClearExistingRope();
        }

        // Public method to get rope tension (useful for gameplay)
        public float GetRopeTension()
        {
            if (segments == null || segments.Length < 2)
                return 0f;

            float totalTension = 0f;
            for (int i = 1; i < segmentCount; i++)
            {
                float distance = Vector3.Distance(
                    segments[i].transform.position,
                    segments[i - 1].transform.position);
                totalTension += Mathf.Max(0f, distance - segmentLength);
            }

            return totalTension;
        }

        // Public method to apply force to all segments (e.g., current)
        public void ApplyCurrentForce(Vector3 force)
        {
            if (segments == null)
                return;

            foreach (var segment in segments)
            {
                Rigidbody rb = segment.GetComponent<Rigidbody>();
                if (!rb.isKinematic)
                {
                    rb.AddForce(force, ForceMode.Force);
                }
            }
        }
    }
}
