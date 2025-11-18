using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class CaveGenerationSetupEditor : SceneSetupTool<CaveGenerationConfig>
{
    private const string CaveSystemName = "CaveSystem";
    private const string CaveMeshName = "CaveMesh";
    private const string WallMeshName = "WallMesh";
    private const string SurfaceName = "Surface";
    private const string BottomName = "Bottom";
    private const string MiddleTopName = "MiddleTerrainTop";
    private const string MiddleBottomName = "MiddleTerrainBottom";
    private const string MarkersParentName = "Markers";

    protected override string DefaultConfigPath => "Assets/Settings/CaveGenerationConfig.asset";
    protected override string ConfigMenuName => "Cave Generation Setup";

    [MenuItem("Tools/Cave Generation/Setup Cave System")]
    private static void OpenWindow()
    {
        GetWindow<CaveGenerationSetupEditor>();
    }

    protected override void ApplySetup(CaveGenerationConfig config)
    {
        if (config == null)
        {
            Debug.LogError("Config asset missing.");
            return;
        }

        GameObject caveSystem = GameObject.Find(CaveSystemName);
        if (caveSystem != null)
        {
            DestroyImmediate(caveSystem);
        }

        caveSystem = new GameObject(CaveSystemName);
        caveSystem.transform.position = config.cavePosition;
        caveSystem.transform.rotation = Quaternion.Euler(config.caveRotation);

        GameObject caveMeshObj = CreateChild(caveSystem.transform, CaveMeshName);
        MeshFilter caveMeshFilter = caveMeshObj.AddComponent<MeshFilter>();
        MeshRenderer caveRenderer = caveMeshObj.AddComponent<MeshRenderer>();
        if (config.caveMaterial != null)
        {
            caveRenderer.sharedMaterial = config.caveMaterial;
        }

        GameObject wallMeshObj = CreateChild(caveSystem.transform, WallMeshName);
        MeshFilter wallMeshFilter = wallMeshObj.AddComponent<MeshFilter>();
        MeshRenderer wallRenderer = wallMeshObj.AddComponent<MeshRenderer>();
        if (config.wallMaterial != null)
        {
            wallRenderer.sharedMaterial = config.wallMaterial;
        }

        CaveMeshBuilder meshBuilder = caveSystem.AddComponent<CaveMeshBuilder>();
        var builderType = typeof(CaveMeshBuilder);
        var caveMeshField = builderType.GetField("caveMeshFilter", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var wallMeshField = builderType.GetField("wallMeshFilter", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        caveMeshField?.SetValue(meshBuilder, caveMeshFilter);
        wallMeshField?.SetValue(meshBuilder, wallMeshFilter);

        CaveGenerator generator = caveSystem.AddComponent<CaveGenerator>();
        generator.Initialize(meshBuilder);
        generator.GenerateCave(config);

        Transform markersParent = CreateMarkersParent(caveSystem.transform);
        CreateMarker(markersParent, SurfaceName, config.surfaceMarker, config, config.height * 0.5f);
        CreateMarker(markersParent, BottomName, config.bottomMarker, config, -config.height * 0.5f);

        float upperMiddleBoundary = config.height / 6f;   // top - (height/3)
        float lowerMiddleBoundary = -config.height / 6f;  // bottom + (height/3)
        CreateMarker(markersParent, MiddleTopName, config.middleMarker, config, upperMiddleBoundary);
        CreateMarker(markersParent, MiddleBottomName, config.middleMarker, config, lowerMiddleBoundary);

        string prefabDir = "Assets/Prefabs";
        if (!AssetDatabase.IsValidFolder(prefabDir))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        string prefabPath = $"{prefabDir}/CaveSystem.prefab";
        PrefabUtility.SaveAsPrefabAsset(caveSystem, prefabPath);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = caveSystem;

        Debug.Log($"Cave system generated with {config.width}x{config.height} map.");
    }

    protected override void CaptureFromScene(CaveGenerationConfig config)
    {
        GameObject caveSystem = GameObject.Find(CaveSystemName);
        if (caveSystem == null)
        {
            Debug.LogWarning("Cave system not found in scene.");
            return;
        }

        config.cavePosition = caveSystem.transform.position;
        config.caveRotation = caveSystem.transform.rotation.eulerAngles;

        CaveMeshBuilder meshBuilder = caveSystem.GetComponent<CaveMeshBuilder>();
        if (meshBuilder != null)
        {
            Transform caveMesh = caveSystem.transform.Find(CaveMeshName);
            if (caveMesh != null)
            {
                MeshRenderer renderer = caveMesh.GetComponent<MeshRenderer>();
                if (renderer != null && renderer.sharedMaterial != null)
                {
                    config.caveMaterial = renderer.sharedMaterial;
                }
            }

            Transform wallMesh = caveSystem.transform.Find(WallMeshName);
            if (wallMesh != null)
            {
                MeshRenderer renderer = wallMesh.GetComponent<MeshRenderer>();
                if (renderer != null && renderer.sharedMaterial != null)
                {
                    config.wallMaterial = renderer.sharedMaterial;
                }
            }
        }

        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        Debug.Log("Captured cave generation configuration from scene.");
    }

    protected override void OnGUI()
    {
        base.OnGUI();

        EditorGUILayout.Space();

        if (GUILayout.Button("Regenerate Cave"))
        {
            GameObject caveSystem = GameObject.Find(CaveSystemName);
            if (caveSystem != null)
            {
                CaveGenerator generator = caveSystem.GetComponent<CaveGenerator>();
                CaveMeshBuilder meshBuilder = caveSystem.GetComponent<CaveMeshBuilder>();

                if (generator != null && meshBuilder != null)
                {
                    CaveGenerationConfig config = AssetDatabase.LoadAssetAtPath<CaveGenerationConfig>(DefaultConfigPath);
                    if (config != null)
                    {
                        generator.Initialize(meshBuilder);
                        generator.GenerateCave(config);
                        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                        Debug.Log("Cave regenerated.");
                    }
                }
            }
            else
            {
                Debug.LogWarning("No cave system in scene. Use 'Apply Setup' first.");
            }
        }
    }

    private GameObject CreateChild(Transform parent, string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        return child;
    }

    private void RemoveChildIfExists(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child != null)
        {
            DestroyImmediate(child.gameObject);
        }
    }

    private Transform CreateMarkersParent(Transform root)
    {
        RemoveChildIfExists(root, MarkersParentName);
        GameObject markers = new GameObject(MarkersParentName);
        markers.transform.SetParent(root, false);
        return markers.transform;
    }

    private void CreateMarker(Transform parent, string name, CaveGenerationConfig.MarkerType markerType, CaveGenerationConfig config, float yPos)
    {
        RemoveChildIfExists(parent, name);

        GameObject marker;
        if (markerType == CaveGenerationConfig.MarkerType.Plane)
        {
            marker = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Collider col = marker.GetComponent<Collider>();
            if (col != null)
            {
                DestroyImmediate(col);
            }
        }
        else
        {
            marker = new GameObject();
        }

        marker.name = name;
        marker.transform.SetParent(parent, false);

        float mapWidth = config.width * config.squareSize;
        float mapHeight = config.height * config.squareSize;

        float depth = config.depthThickness;
        float xPos = -mapWidth / 2f; // left side of the map
        marker.transform.localPosition = new Vector3(xPos, yPos, -depth / 2f);
        if (markerType == CaveGenerationConfig.MarkerType.Plane)
        {
            marker.transform.localScale = new Vector3(mapWidth, depth, 1f);
            marker.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }
}
