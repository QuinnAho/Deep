using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class WaterSetup : EditorWindow
{
    private const string DefaultMaterialPath = "Assets/Materials/WaterMaterial.mat";
    private const string DemoMaterialPath = "Assets/ThirdParty/URPUnderwaterEffects/Demos/DemoAssets/Water.mat";

    private string waterObjectName = "Water";
    private float waterHeight = 0f;
    private Vector2 waterSize = new Vector2(400f, 400f);
    private Material waterMaterial;

    [MenuItem("Tools/Environment/Setup Water")]
    public static void OpenWindow()
    {
        GetWindow<WaterSetup>("Water Setup");
    }

    private void OnEnable()
    {
        TryInitializeFromScene();
        if (waterMaterial == null)
        {
            waterMaterial = AssetDatabase.LoadAssetAtPath<Material>(DefaultMaterialPath);
        }
        if (waterMaterial == null)
        {
            waterMaterial = AssetDatabase.LoadAssetAtPath<Material>(DemoMaterialPath);
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Water Plane", EditorStyles.boldLabel);
        waterObjectName = EditorGUILayout.TextField("Object Name", waterObjectName);
        waterHeight = EditorGUILayout.FloatField("Water Height (Y)", waterHeight);
        waterSize = EditorGUILayout.Vector2Field("Size (meters)", waterSize);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Material", EditorStyles.boldLabel);
        waterMaterial = (Material)EditorGUILayout.ObjectField("Water Material", waterMaterial, typeof(Material), false);

        if (GUILayout.Button("Use Demo Material"))
        {
            waterMaterial = AssetDatabase.LoadAssetAtPath<Material>(DemoMaterialPath);
        }
        if (GUILayout.Button("Create Basic Material"))
        {
            waterMaterial = CreateDefaultMaterial();
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("Create / Update Water"))
        {
            CreateOrUpdateWater();
        }
    }

    private void TryInitializeFromScene()
    {
        GameObject existing = GameObject.Find(waterObjectName);
        if (existing == null) return;

        waterHeight = existing.transform.position.y;
        Vector3 scale = existing.transform.localScale;
        waterSize = new Vector2(scale.x * 10f, scale.z * 10f);

        MeshRenderer renderer = existing.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            waterMaterial = renderer.sharedMaterial;
        }
    }

    private void CreateOrUpdateWater()
    {
        GameObject water = GameObject.Find(waterObjectName);
        if (water == null)
        {
            water = GameObject.CreatePrimitive(PrimitiveType.Plane);
            water.name = waterObjectName;
        }

        Undo.RegisterFullObjectHierarchyUndo(water, "Configure Water");

        Vector3 position = water.transform.position;
        position.y = waterHeight;
        water.transform.position = position;

        water.transform.localScale = new Vector3(Mathf.Max(0.1f, waterSize.x / 10f), 1f, Mathf.Max(0.1f, waterSize.y / 10f));

        if (waterMaterial != null)
        {
            MeshRenderer renderer = water.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = waterMaterial;
            }
        }

        SetWaterLayer(water);

        Selection.activeGameObject = water;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log($"Water setup complete at height Y={waterHeight:F2}. Remember to save the scene.");
    }

    private void SetWaterLayer(GameObject water)
    {
        int waterLayer = LayerMask.NameToLayer("Water");
        if (waterLayer == -1)
        {
            Debug.LogWarning("Layer 'Water' not found. Please add it in Project Settings > Tags and Layers.");
            return;
        }

        if (water.layer != waterLayer)
        {
            void ApplyLayer(Transform t)
            {
                t.gameObject.layer = waterLayer;
                foreach (Transform child in t)
                {
                    ApplyLayer(child);
                }
            }

            ApplyLayer(water.transform);
        }
    }

    private Material CreateDefaultMaterial()
    {
        Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
        if (urpShader == null)
        {
            Debug.LogError("URP Lit shader not found. Make sure URP is installed.");
            return null;
        }

        Material material = new Material(urpShader)
        {
            name = "WaterMaterial"
        };
        material.SetColor("_BaseColor", new Color(0.1f, 0.4f, 0.6f, 0.6f));
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_AlphaClip", 0f);
        material.SetFloat("_Metallic", 0.8f);
        material.SetFloat("_Smoothness", 0.9f);
        material.renderQueue = 3000;
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }
        AssetDatabase.CreateAsset(material, DefaultMaterialPath);
        AssetDatabase.SaveAssets();
        return material;
    }
}
