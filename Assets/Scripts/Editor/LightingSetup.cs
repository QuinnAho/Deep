using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class LightingSetup : EditorWindow
{
    private Light directionalLight;
    private string lightObjectName = "Sun";

    private Color lightColor = new Color(1f, 0.965f, 0.88f);
    private Vector3 lightEuler = new Vector3(50f, -30f, 0f);
    private float lightIntensity = 1.2f;
    private float shadowStrength = 0.85f;

    private bool enableFog = true;
    private Color fogColor = new Color(0.02f, 0.08f, 0.12f);
    private FogMode fogMode = FogMode.Exponential;
    private float fogDensity = 0.12f;

    private Color ambientColor = new Color(0.06f, 0.09f, 0.12f);
    private float ambientIntensity = 1f;

    [MenuItem("Tools/Environment/Configure Lighting")]
    public static void Open()
    {
        GetWindow<LightingSetup>("Lighting Setup");
    }

    private void OnEnable()
    {
        TryFindDirectionalLight();
        if (directionalLight != null)
        {
            lightObjectName = directionalLight.name;
            lightColor = directionalLight.color;
            lightEuler = directionalLight.transform.eulerAngles;
            lightIntensity = directionalLight.intensity;
            shadowStrength = directionalLight.shadowStrength;
        }

        enableFog = RenderSettings.fog;
        fogColor = RenderSettings.fogColor;
        fogMode = RenderSettings.fogMode;
        fogDensity = RenderSettings.fogDensity;

        ambientColor = RenderSettings.ambientLight;
        ambientIntensity = RenderSettings.ambientIntensity;
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Directional Light", EditorStyles.boldLabel);
        lightObjectName = EditorGUILayout.TextField("Name", lightObjectName);
        lightColor = EditorGUILayout.ColorField("Color", lightColor);
        lightIntensity = EditorGUILayout.FloatField("Intensity", lightIntensity);
        shadowStrength = EditorGUILayout.Slider("Shadow Strength", shadowStrength, 0f, 1f);
        lightEuler = EditorGUILayout.Vector3Field("Rotation", lightEuler);

        if (GUILayout.Button("Find Existing Light"))
        {
            TryFindDirectionalLight();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Ambient", EditorStyles.boldLabel);
        ambientColor = EditorGUILayout.ColorField("Ambient Color", ambientColor);
        ambientIntensity = EditorGUILayout.Slider("Ambient Intensity", ambientIntensity, 0f, 2f);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Fog", EditorStyles.boldLabel);
        enableFog = EditorGUILayout.Toggle("Enable Fog", enableFog);
        fogColor = EditorGUILayout.ColorField("Fog Color", fogColor);
        fogMode = (FogMode)EditorGUILayout.EnumPopup("Fog Mode", fogMode);
        fogDensity = EditorGUILayout.Slider("Fog Density", fogDensity, 0.001f, 0.5f);

        EditorGUILayout.Space();
        if (GUILayout.Button("Apply Lighting Setup"))
        {
            ApplyLighting();
        }
    }

    private void ApplyLighting()
    {
        if (directionalLight == null)
        {
            var lightGO = new GameObject(lightObjectName);
            directionalLight = lightGO.AddComponent<Light>();
            directionalLight.type = LightType.Directional;
        }

        Undo.RegisterFullObjectHierarchyUndo(directionalLight.gameObject, "Configure Lighting");

        directionalLight.name = lightObjectName;
        directionalLight.color = lightColor;
        directionalLight.intensity = lightIntensity;
        directionalLight.shadowStrength = shadowStrength;
        directionalLight.shadows = LightShadows.Soft;
        directionalLight.transform.rotation = Quaternion.Euler(lightEuler);

        RenderSettings.sun = directionalLight;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = ambientColor;
        RenderSettings.ambientIntensity = ambientIntensity;

        RenderSettings.fog = enableFog;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogMode = fogMode;
        RenderSettings.fogDensity = fogDensity;

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Lighting configured. Save your scene (Ctrl+S).");
    }

    private void TryFindDirectionalLight()
    {
        if (RenderSettings.sun != null)
        {
            directionalLight = RenderSettings.sun;
            return;
        }

        Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (Light light in lights)
        {
            if (light.type == LightType.Directional)
            {
                directionalLight = light;
                return;
            }
        }
    }
}
