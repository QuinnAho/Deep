using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class DebugSpectatorSetup
{
    private const string SpectatorName = "DebugSpectator";
    private const string PrefabPath = "Assets/Prefabs/DebugSpectator.prefab";

    [MenuItem("Tools/Debug/Create Spectator")]
    public static void CreateSpectator()
    {
        GameObject existing = GameObject.Find(SpectatorName);
        if (existing != null)
        {
            Selection.activeGameObject = existing;
            Debug.Log("DebugSpectator already exists in the scene.");
            return;
        }

        GameObject spectator = new GameObject(SpectatorName);
        spectator.transform.position = new Vector3(0f, 5f, 0f);

        CharacterController controller = spectator.AddComponent<CharacterController>();
        controller.height = 1.8f;
        controller.radius = 0.4f;
        controller.stepOffset = 0.2f;

        DebugSpectatorController behaviour = spectator.AddComponent<DebugSpectatorController>();

        GameObject cameraGO = new GameObject("SpectatorCamera");
        cameraGO.transform.SetParent(spectator.transform, false);
        cameraGO.transform.localPosition = new Vector3(0f, 0.2f, 0f);

        Camera camera = cameraGO.AddComponent<Camera>();
        camera.fieldOfView = 70f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 2000f;

        EnsurePrefabDirectory();
        PrefabUtility.SaveAsPrefabAsset(spectator, PrefabPath);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = spectator;
        Debug.Log("DebugSpectator created. Use WASD/Space/Ctrl + mouse to fly.");
    }

    private static void EnsurePrefabDirectory()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }
    }
}
