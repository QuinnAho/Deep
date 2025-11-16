using UnityEngine;

[CreateAssetMenu(menuName = "Deep/Submarine Setup Config", fileName = "SubmarineSetupConfig")]
public class SubmarineSetupConfig : ScriptableObject
{
    [Header("Model")]
    public string modelPath = "Assets/Models/Titan Submersible-1.obj";

    [Header("Submarine Transform")]
    public Vector3 submarinePosition = new Vector3(0f, -5f, 0f);
    public Vector3 submarineEuler = Vector3.zero;

    [Header("Rigidbody")]
    public float mass = 2000f;
    public float linearDamping = 1f;
    public float angularDamping = 2f;

    [Header("Collider")]
    public float colliderRadius = 1f;
    public float colliderHeight = 4f;

    [Header("Third Person Camera")]
    public Vector3 thirdPersonLocalPosition = new Vector3(0f, 2f, 8f);
    public Vector3 thirdPersonLocalEuler = new Vector3(10f, 180f, 0f);
    public float thirdPersonFov = 60f;

    [Header("First Person Camera")]
    public Vector3 firstPersonLocalPosition = new Vector3(0f, 1f, -3f);
    public Vector3 firstPersonLocalEuler = new Vector3(0f, 180f, 0f);
    public float firstPersonFov = 70f;

    [Header("Headlights")]
    public Vector3 leftHeadlightLocalPosition = new Vector3(-0.6f, 0.2f, -2.2f);
    public Vector3 leftHeadlightLocalEuler = new Vector3(-5f, 180f, 0f);
    public Vector3 rightHeadlightLocalPosition = new Vector3(0.6f, 0.2f, -2.2f);
    public Vector3 rightHeadlightLocalEuler = new Vector3(-5f, 180f, 0f);
    public Color headlightColor = new Color(0.9f, 0.95f, 1f);
    public float headlightIntensity = 3f;
    public float headlightRange = 50f;
    public float headlightSpotAngle = 60f;
}
