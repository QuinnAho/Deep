using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class DebugSpectatorSetup : EditorWindow
{
    private const string SpectatorName = "DebugSpectator";
    private const string PhysicsSpectatorName = "PhysicsSpectator";
    private const string PrefabPath = "Assets/Prefabs/DebugSpectator.prefab";
    private const string PhysicsPrefabPath = "Assets/Prefabs/PhysicsSpectator.prefab";

    // Spectator Type
    private bool usePhysicsBased = false;

    // Collision Settings
    private bool enableCollision = true;
    private float collisionRadius = 0.4f;
    private float collisionHeight = 1.8f;
    private float stepOffset = 0.2f;
    private float skinWidth = 0.08f;
    private LayerMask collisionLayers = ~0; // All layers

    // Camera Settings
    private float fieldOfView = 70f;
    private float nearClip = 0.1f;
    private float farClip = 2000f;

    // Spawn Settings
    private Vector3 spawnPosition = new Vector3(0f, 5f, 0f);

    [MenuItem("Tools/Debug/Spectator Setup")]
    public static void ShowWindow()
    {
        GetWindow<DebugSpectatorSetup>("Spectator Setup");
    }

    [MenuItem("Tools/Debug/Create Spectator (Quick)")]
    public static void CreateSpectatorQuick()
    {
        var window = CreateInstance<DebugSpectatorSetup>();
        window.CreateSpectator();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Debug Spectator Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // Spectator Type
        EditorGUILayout.LabelField("Spectator Type", EditorStyles.boldLabel);
        usePhysicsBased = EditorGUILayout.Toggle("Physics-Based (for rope attachment)", usePhysicsBased);

        if (usePhysicsBased)
        {
            EditorGUILayout.HelpBox(
                "Physics-based spectator uses Rigidbody for movement.\n" +
                "Can be attached to ropes and interacts with physics objects.",
                MessageType.Info);
        }

        EditorGUILayout.Space(10);

        // Collision Section
        EditorGUILayout.LabelField("Collision Settings", EditorStyles.boldLabel);
        enableCollision = EditorGUILayout.Toggle("Enable Collision", enableCollision);

        GUI.enabled = enableCollision;
        collisionRadius = EditorGUILayout.Slider("Collision Radius", collisionRadius, 0.1f, 2f);
        collisionHeight = EditorGUILayout.Slider("Collision Height", collisionHeight, 0.5f, 4f);
        stepOffset = EditorGUILayout.Slider("Step Offset", stepOffset, 0f, 1f);
        skinWidth = EditorGUILayout.Slider("Skin Width", skinWidth, 0.001f, 0.2f);
        GUI.enabled = true;

        EditorGUILayout.Space(10);

        // Camera Section
        EditorGUILayout.LabelField("Camera Settings", EditorStyles.boldLabel);
        fieldOfView = EditorGUILayout.Slider("Field of View", fieldOfView, 30f, 120f);
        nearClip = EditorGUILayout.FloatField("Near Clip", nearClip);
        farClip = EditorGUILayout.FloatField("Far Clip", farClip);

        EditorGUILayout.Space(10);

        // Spawn Section
        EditorGUILayout.LabelField("Spawn Settings", EditorStyles.boldLabel);
        spawnPosition = EditorGUILayout.Vector3Field("Spawn Position", spawnPosition);

        if (GUILayout.Button("Use Scene View Position"))
        {
            if (SceneView.lastActiveSceneView != null)
            {
                spawnPosition = SceneView.lastActiveSceneView.camera.transform.position;
            }
        }

        EditorGUILayout.Space(10);

        // Action Buttons
        string buttonText = usePhysicsBased ? "Create Physics Spectator" : "Create Spectator";
        if (GUILayout.Button(buttonText, GUILayout.Height(30)))
        {
            if (usePhysicsBased)
                CreatePhysicsSpectator();
            else
                CreateSpectator();
        }

        if (GUILayout.Button("Update Existing Spectator"))
        {
            UpdateExistingSpectator();
        }

        EditorGUILayout.Space(5);

        EditorGUILayout.HelpBox(
            "Controls:\n" +
            "WASD - Move horizontally\n" +
            "E/Q - Move up/down\n" +
            "Right Mouse - Look around\n" +
            "Shift - Sprint\n" +
            "Scroll - Adjust speed",
            MessageType.Info);
    }

    private void CreateSpectator()
    {
        GameObject existing = GameObject.Find(SpectatorName);
        if (existing != null)
        {
            Selection.activeGameObject = existing;
            Debug.Log("DebugSpectator already exists in the scene. Use 'Update Existing' to modify it.");
            return;
        }

        GameObject spectator = new GameObject(SpectatorName);
        spectator.transform.position = spawnPosition;

        // Add CharacterController for collision
        CharacterController controller = spectator.AddComponent<CharacterController>();
        controller.height = collisionHeight;
        controller.radius = collisionRadius;
        controller.stepOffset = stepOffset;
        controller.skinWidth = skinWidth;
        controller.enabled = enableCollision;

        DebugSpectatorController behaviour = spectator.AddComponent<DebugSpectatorController>();

        GameObject cameraGO = new GameObject("SpectatorCamera");
        cameraGO.transform.SetParent(spectator.transform, false);
        cameraGO.transform.localPosition = new Vector3(0f, collisionHeight * 0.5f - 0.1f, 0f);

        Camera camera = cameraGO.AddComponent<Camera>();
        camera.fieldOfView = fieldOfView;
        camera.nearClipPlane = nearClip;
        camera.farClipPlane = farClip;

        EnsurePrefabDirectory();
        PrefabUtility.SaveAsPrefabAsset(spectator, PrefabPath);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = spectator;

        string collisionStatus = enableCollision ? "enabled" : "disabled";
        Debug.Log($"DebugSpectator created with collision {collisionStatus}. Use WASD/E/Q + right mouse to fly.");
    }

    private void UpdateExistingSpectator()
    {
        GameObject spectator = GameObject.Find(SpectatorName);
        if (spectator == null)
        {
            EditorUtility.DisplayDialog("Error", "No DebugSpectator found in scene.", "OK");
            return;
        }

        Undo.RecordObject(spectator, "Update Spectator");

        CharacterController controller = spectator.GetComponent<CharacterController>();
        if (controller != null)
        {
            Undo.RecordObject(controller, "Update Spectator Collision");
            controller.height = collisionHeight;
            controller.radius = collisionRadius;
            controller.stepOffset = stepOffset;
            controller.skinWidth = skinWidth;
            controller.enabled = enableCollision;
        }

        Camera camera = spectator.GetComponentInChildren<Camera>();
        if (camera != null)
        {
            Undo.RecordObject(camera, "Update Spectator Camera");
            camera.fieldOfView = fieldOfView;
            camera.nearClipPlane = nearClip;
            camera.farClipPlane = farClip;

            // Update camera position based on new height
            camera.transform.localPosition = new Vector3(0f, collisionHeight * 0.5f - 0.1f, 0f);
        }

        EditorUtility.SetDirty(spectator);
        Debug.Log("DebugSpectator updated.");
    }

    private void CreatePhysicsSpectator()
    {
        GameObject existing = GameObject.Find(PhysicsSpectatorName);
        if (existing != null)
        {
            Selection.activeGameObject = existing;
            Debug.Log("PhysicsSpectator already exists in the scene.");
            return;
        }

        GameObject spectator = new GameObject(PhysicsSpectatorName);
        spectator.transform.position = spawnPosition;

        // Add Rigidbody for physics-based movement
        Rigidbody rb = spectator.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.mass = 1f;
        rb.linearDamping = 5f;
        rb.angularDamping = 5f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        // Add CapsuleCollider for collision
        CapsuleCollider capsule = spectator.AddComponent<CapsuleCollider>();
        capsule.height = collisionHeight;
        capsule.radius = collisionRadius;
        capsule.center = new Vector3(0f, collisionHeight * 0.5f, 0f);

        // Add the physics controller
        PhysicsSpectatorController controller = spectator.AddComponent<PhysicsSpectatorController>();

        // Create camera
        GameObject cameraGO = new GameObject("SpectatorCamera");
        cameraGO.transform.SetParent(spectator.transform, false);
        cameraGO.transform.localPosition = new Vector3(0f, collisionHeight * 0.5f + 0.1f, 0f);

        Camera camera = cameraGO.AddComponent<Camera>();
        camera.fieldOfView = fieldOfView;
        camera.nearClipPlane = nearClip;
        camera.farClipPlane = farClip;

        EnsurePrefabDirectory();
        PrefabUtility.SaveAsPrefabAsset(spectator, PhysicsPrefabPath);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = spectator;

        Debug.Log("PhysicsSpectator created. Use WASD/E/Q + right mouse to fly. Can be attached to ropes.");
    }

    private static void EnsurePrefabDirectory()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }
    }
}
