using UnityEngine;

[CreateAssetMenu(menuName = "Deep/Cave Generation Config", fileName = "CaveGenerationConfig")]
public class CaveGenerationConfig : ScriptableObject
{
    public enum MarkerType
    {
        Empty,
        Plane
    }

    [Header("Map Dimensions")]
    public int width = 128;
    public int height = 256;

    [Header("Generation Settings")]
    public string seed = "deep_cave";
    public bool useRandomSeed = true;
    [Range(0, 100)]
    public int randomFillPercent = 45;

    [Header("Smoothing")]
    [Range(1, 10)]
    public int smoothingIterations = 5;

    [Header("Region Processing")]
    public int wallThresholdSize = 50;
    public int roomThresholdSize = 50;

    [Header("Passages")]
    [Range(1, 10)]
    public int passageRadius = 5;

    [Header("Mesh Generation")]
    public float squareSize = 1f;
    public int borderSize = 1;
    public bool is2D = false;
    public float wallHeight = 5f;
    public float depthThickness = 2f;

    [Header("Markers")]
    public MarkerType surfaceMarker = MarkerType.Empty;
    public MarkerType bottomMarker = MarkerType.Empty;
    public MarkerType middleMarker = MarkerType.Empty;


    [Header("Materials")]
    public Material caveMaterial;
    public Material wallMaterial;

    [Header("UV Settings")]
    public int tileAmount = 10;

    [Header("Transform")]
    public Vector3 cavePosition = Vector3.zero;
    public Vector3 caveRotation = Vector3.zero;
}
