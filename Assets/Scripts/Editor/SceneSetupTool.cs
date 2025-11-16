using UnityEditor;
using UnityEngine;

public abstract class SceneSetupTool<TConfig> : EditorWindow where TConfig : ScriptableObject
{
    [SerializeField] private TConfig config;

    protected abstract string DefaultConfigPath { get; }
    protected abstract string ConfigMenuName { get; }

    protected virtual void OnEnable()
    {
        titleContent = new GUIContent(GetType().Name);
        if (config == null)
        {
            config = LoadDefaultConfig();
        }
    }

    protected virtual void OnGUI()
    {
        EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            config = (TConfig)EditorGUILayout.ObjectField(config, typeof(TConfig), false);
            if (GUILayout.Button("New", GUILayout.Width(60)))
            {
                CreateConfigAsset();
            }
        }

        if (config == null)
        {
            EditorGUILayout.HelpBox("Assign or create a configuration asset to continue.", MessageType.Info);
            return;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Apply Setup"))
            {
                ApplySetup(config);
            }

            if (GUILayout.Button("Capture From Scene"))
            {
                CaptureFromScene(config);
            }
        }
    }

    protected abstract void ApplySetup(TConfig configAsset);
    protected abstract void CaptureFromScene(TConfig configAsset);

    private TConfig LoadDefaultConfig()
    {
        if (string.IsNullOrEmpty(DefaultConfigPath))
        {
            return null;
        }

        return AssetDatabase.LoadAssetAtPath<TConfig>(DefaultConfigPath);
    }

    private void CreateConfigAsset()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Create Config",
            typeof(TConfig).Name + ".asset",
            "asset",
            "Select where to save the configuration asset.");

        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        TConfig instance = CreateInstance<TConfig>();
        AssetDatabase.CreateAsset(instance, path);
        AssetDatabase.SaveAssets();
        config = instance;
    }
}
