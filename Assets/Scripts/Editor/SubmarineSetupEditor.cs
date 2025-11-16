using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class SubmarineSetupEditor : SceneSetupTool<SubmarineSetupConfig>
{
    private const string SubmarineName = "TitanSubmarine";
    private const string CameraRigName = "CameraRig";
    private const string ThirdPersonName = "ThirdPersonView";
    private const string FirstPersonName = "FirstPersonView";
    private const string EffectsName = "Effects";
    private const string HeadlightsName = "Headlights";

    protected override string DefaultConfigPath => "Assets/Settings/SubmarineSetupConfig.asset";

    [MenuItem("Tools/Submarine/Setup Titan Submarine")]
    private static void OpenWindow()
    {
        GetWindow<SubmarineSetupEditor>();
    }

    protected override string ConfigMenuName => "Submarine Setup";

    protected override void ApplySetup(SubmarineSetupConfig config)
    {
        if (config == null)
        {
            Debug.LogError("Config asset missing.");
            return;
        }

        GameObject submarine = GameObject.Find(SubmarineName);
        if (submarine != null)
        {
            DestroyImmediate(submarine);
        }

        GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(config.modelPath);
        if (modelAsset == null)
        {
            Debug.LogError($"Could not find model at {config.modelPath}");
            return;
        }

        submarine = new GameObject(SubmarineName);
        submarine.transform.position = config.submarinePosition;
        submarine.transform.rotation = Quaternion.Euler(config.submarineEuler);

        Rigidbody rb = submarine.AddComponent<Rigidbody>();
        rb.mass = config.mass;
        rb.useGravity = false;
        rb.linearDamping = config.linearDamping;
        rb.angularDamping = config.angularDamping;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        CapsuleCollider collider = submarine.AddComponent<CapsuleCollider>();
        collider.radius = config.colliderRadius;
        collider.height = config.colliderHeight;
        collider.direction = 2;

        submarine.AddComponent<SubmarineController>();
        submarine.AddComponent<SubmarinePhysics>();

        GameObject modelInstance = PrefabUtility.InstantiatePrefab(modelAsset) as GameObject;
        modelInstance.transform.SetParent(submarine.transform, false);
        modelInstance.name = "Model";

        GameObject cameraRig = CreateChild(submarine.transform, CameraRigName);

        Camera thirdPersonCamera = CreateCamera(cameraRig.transform, ThirdPersonName, config.thirdPersonLocalPosition, config.thirdPersonLocalEuler, config.thirdPersonFov, true);
        Camera firstPersonCamera = CreateCamera(cameraRig.transform, FirstPersonName, config.firstPersonLocalPosition, config.firstPersonLocalEuler, config.firstPersonFov, false);

        SubmarineCameraController cameraController = submarine.AddComponent<SubmarineCameraController>();
        var controllerType = typeof(SubmarineCameraController);
        var firstPersonField = controllerType.GetField("firstPersonCamera", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var thirdPersonField = controllerType.GetField("thirdPersonCamera", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        firstPersonField?.SetValue(cameraController, firstPersonCamera);
        thirdPersonField?.SetValue(cameraController, thirdPersonCamera);

        Transform effects = CreateChild(submarine.transform, EffectsName).transform;
        CreateHeadlights(effects, config);

        GameObject audio = CreateChild(submarine.transform, "Audio");
        AudioSource audioSource = audio.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f;
        audioSource.loop = true;

        string prefabDir = "Assets/Prefabs";
        if (!AssetDatabase.IsValidFolder(prefabDir))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        string prefabPath = $"{prefabDir}/TitanSubmarine.prefab";
        PrefabUtility.SaveAsPrefabAsset(submarine, prefabPath);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = submarine;
    }

    protected override void CaptureFromScene(SubmarineSetupConfig config)
    {
        GameObject submarine = GameObject.Find(SubmarineName);
        if (submarine == null)
        {
            Debug.LogWarning("Submarine not found in scene.");
            return;
        }

        config.submarinePosition = submarine.transform.position;
        config.submarineEuler = submarine.transform.rotation.eulerAngles;

        Rigidbody rb = submarine.GetComponent<Rigidbody>();
        if (rb != null)
        {
            config.mass = rb.mass;
            config.linearDamping = rb.linearDamping;
            config.angularDamping = rb.angularDamping;
        }

        CapsuleCollider collider = submarine.GetComponent<CapsuleCollider>();
        if (collider != null)
        {
            config.colliderRadius = collider.radius;
            config.colliderHeight = collider.height;
        }

        Transform cameraRig = submarine.transform.Find(CameraRigName);
        if (cameraRig != null)
        {
            Transform third = cameraRig.Find(ThirdPersonName);
            Transform first = cameraRig.Find(FirstPersonName);

            if (third != null)
            {
                config.thirdPersonLocalPosition = third.localPosition;
                config.thirdPersonLocalEuler = third.localEulerAngles;
                Camera cam = third.GetComponent<Camera>();
                if (cam != null) config.thirdPersonFov = cam.fieldOfView;
            }

            if (first != null)
            {
                config.firstPersonLocalPosition = first.localPosition;
                config.firstPersonLocalEuler = first.localEulerAngles;
                Camera cam = first.GetComponent<Camera>();
                if (cam != null) config.firstPersonFov = cam.fieldOfView;
            }
        }

        Transform effects = submarine.transform.Find($"{EffectsName}/{HeadlightsName}");
        if (effects != null)
        {
            Transform left = effects.Find("LeftHeadlight");
            Transform right = effects.Find("RightHeadlight");
            if (left != null)
            {
                config.leftHeadlightLocalPosition = left.localPosition;
                config.leftHeadlightLocalEuler = left.localEulerAngles;
                Light light = left.GetComponent<Light>();
                if (light != null)
                {
                    config.headlightIntensity = light.intensity;
                    config.headlightRange = light.range;
                    config.headlightSpotAngle = light.spotAngle;
                    config.headlightColor = light.color;
                }
            }

            if (right != null)
            {
                config.rightHeadlightLocalPosition = right.localPosition;
                config.rightHeadlightLocalEuler = right.localEulerAngles;
            }
        }

        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        Debug.Log("Captured submarine configuration from scene.");
    }

    private GameObject CreateChild(Transform parent, string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        return child;
    }

    private Camera CreateCamera(Transform parent, string name, Vector3 localPosition, Vector3 localEuler, float fov, bool enable)
    {
        GameObject camGO = new GameObject(name);
        camGO.transform.SetParent(parent, false);
        camGO.transform.localPosition = localPosition;
        camGO.transform.localRotation = Quaternion.Euler(localEuler);

        Camera cam = camGO.AddComponent<Camera>();
        cam.fieldOfView = fov;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 1000f;
        cam.enabled = enable;

        if (enable)
        {
            camGO.AddComponent<AudioListener>();
        }

        return cam;
    }

    private void CreateHeadlights(Transform effectsParent, SubmarineSetupConfig config)
    {
        Transform headlights = effectsParent.Find(HeadlightsName);
        if (headlights != null)
        {
            DestroyImmediate(headlights.gameObject);
        }

        GameObject headlightRoot = new GameObject(HeadlightsName);
        headlightRoot.transform.SetParent(effectsParent, false);

        CreateHeadlight(headlightRoot.transform, "LeftHeadlight", config.leftHeadlightLocalPosition, config.leftHeadlightLocalEuler, config);
        CreateHeadlight(headlightRoot.transform, "RightHeadlight", config.rightHeadlightLocalPosition, config.rightHeadlightLocalEuler, config);
    }

    private void CreateHeadlight(Transform parent, string name, Vector3 localPosition, Vector3 localEuler, SubmarineSetupConfig config)
    {
        GameObject lightGO = new GameObject(name);
        lightGO.transform.SetParent(parent, false);
        lightGO.transform.localPosition = localPosition;
        lightGO.transform.localRotation = Quaternion.Euler(localEuler);

        Light light = lightGO.AddComponent<Light>();
        light.type = LightType.Spot;
        light.color = config.headlightColor;
        light.intensity = config.headlightIntensity;
        light.range = config.headlightRange;
        light.spotAngle = config.headlightSpotAngle;
        light.shadows = LightShadows.Soft;
        light.renderMode = LightRenderMode.ForcePixel;
    }
}
