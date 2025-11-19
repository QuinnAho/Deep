using UnityEngine;

namespace Rope
{
    [CreateAssetMenu(fileName = "UnderwaterRopeConfig", menuName = "Deep/Underwater Rope Config")]
    public class UnderwaterRopeConfig : ScriptableObject
    {
        [Header("Rope Structure")]
        [Tooltip("Number of physics segments in the rope")]
        [Range(5, 100)]
        public int segmentCount = 20;

        [Tooltip("Length of each rope segment")]
        [Range(0.1f, 2f)]
        public float segmentLength = 0.5f;

        [Tooltip("Visual width of the rope")]
        [Range(0.01f, 0.5f)]
        public float ropeWidth = 0.05f;

        [Header("Physics Settings")]
        [Tooltip("Mass of each rope segment")]
        [Range(0.01f, 1f)]
        public float segmentMass = 0.1f;

        [Tooltip("Linear drag for underwater resistance")]
        [Range(0f, 10f)]
        public float drag = 2f;

        [Tooltip("Angular drag for underwater rotation resistance")]
        [Range(0f, 5f)]
        public float angularDrag = 1f;

        [Tooltip("Joint spring for smoother rope behavior")]
        [Range(0f, 500f)]
        public float jointSpring = 100f;

        [Tooltip("Joint damper for smoother rope behavior")]
        [Range(0f, 50f)]
        public float jointDamper = 10f;

        [Header("Rendering")]
        [Tooltip("Material for the rope LineRenderer")]
        public Material ropeMaterial;

        [Tooltip("Smoothing factor for spline interpolation")]
        [Range(1, 8)]
        public int splineSmoothing = 4;

        [Header("Attachment References")]
        [Tooltip("The fixed anchor point (e.g., submarine, platform)")]
        public Transform anchorPoint;

        [Tooltip("The moving object attached to rope end (e.g., player, cargo)")]
        public Rigidbody attachedObject;

        public float TotalRopeLength => segmentCount * segmentLength;
    }
}
