using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Procedurally builds a performant underwater HDRP presentation (water surface, fog volumes,
/// volumetric lighting, particulates, and caustics) so designers always land in a moody scene.
/// </summary>
[ExecuteAlways]
public class UnderwaterSceneDirector : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private Transform submarineRoot;
    [SerializeField] private Material waterMaterial;
    [SerializeField] private Texture causticsTexture;

    [Header("Water")]
    [SerializeField] private float waterHeight = 0f;
    [SerializeField] private float waterMotionSpeed = 0.35f;
    [SerializeField] private float absorptionDistance = 8f;
    [SerializeField] private Color waterScatteringColor = new Color(0.05f, 0.15f, 0.2f);
    [SerializeField] private float surfaceBlendDepth = 3f;

    [Header("Global Fog")]
    [SerializeField] private float globalMeanFreePath = 18f;
    [SerializeField, Range(-1f, 1f)] private float globalAnisotropy = 0.35f;
    [SerializeField] private Color globalFogAlbedo = new Color(0.08f, 0.15f, 0.21f);

    [Header("Local Claustrophobic Volume")]
    [SerializeField] private Vector3 dreadVolumeSize = new Vector3(26f, 10f, 40f);
    [SerializeField] private float dreadMeanFreePath = 6f;
    [SerializeField, Range(-1f, 1f)] private float dreadAnisotropy = 0.45f;

    [Header("Particles")]
    [SerializeField] private float particulateRate = 120f;

    [Header("Caustics")]
    [SerializeField] private float causticsBaseFade = 0.65f;

    [Header("Headlight Beam")]
    [SerializeField] private Color headlightColor = new Color(0.85f, 0.95f, 1f);
    [SerializeField] private float headlightIntensity = 65000f;
    [SerializeField] private float headlightRange = 60f;
    [SerializeField] private float headlightSpotAngle = 32f;
    [SerializeField] private float headlightVolumetricDimmer = 1.2f;

    private Transform environmentRoot;
    private Transform waterTransform;
    private Volume globalVolume;
    private LocalVolumetricFog localFog;
    private Transform particleTransform;
    private ParticleSystem suspendedParticles;
    private DecalProjector causticsProjector;
    private Light headlightLight;
    private HDAdditionalLightData headlightHdData;
    private Material runtimeCausticMaterial;
    private Material runtimeParticleMaterial;
    private Gradient runtimeGradient;
#if UNITY_EDITOR
    private bool editorSetupQueued;
#endif

    private void OnEnable()
    {
        EnsureSetup();
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (!EditorApplication.isPlayingOrWillChangePlaymode)
        {
            QueueEditorSetup();
            return;
        }
#endif
        EnsureSetup();
    }

    private void Update()
    {
        AnimateCausticsMaterial();
        UpdateEffectVisibility();
    }

    private void OnDisable()
    {
        CleanupRuntimeMaterial(ref runtimeCausticMaterial);
        CleanupRuntimeMaterial(ref runtimeParticleMaterial);
#if UNITY_EDITOR
        if (editorSetupQueued)
        {
            editorSetupQueued = false;
            EditorApplication.delayCall -= PerformEditorSetup;
        }
#endif
    }

    private void EnsureSetup()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        if (transform.parent == null && submarineRoot == null)
        {
            submarineRoot = FindFirstObjectByType<SubmarineController>()?.transform;
        }

        environmentRoot = EnsureChild(environmentRoot, transform, "UnderwaterEnvironment");
        ConfigureWaterSurface();
        ConfigureGlobalVolume();
        ConfigureLocalVolume();
        ConfigureParticles();
        ConfigureCaustics();
        ConfigureHeadlight();
    }

    private void ConfigureWaterSurface()
    {
        waterTransform = EnsureChild(waterTransform, environmentRoot, "HDRP Water Surface");
        waterTransform.localPosition = new Vector3(0f, waterHeight, 0f);
        waterTransform.localRotation = Quaternion.identity;

        var surface = waterTransform.GetComponent<WaterSurface>();
        if (surface == null)
        {
            surface = waterTransform.gameObject.AddComponent<WaterSurface>();
        }

        surface.surfaceType = WaterSurfaceType.OceanSeaLake;
        surface.geometryType = WaterGeometryType.Infinite;
        surface.timeMultiplier = waterMotionSpeed;
        surface.startSmoothness = 0.92f;
        surface.endSmoothness = 0.65f;
        surface.smoothnessFadeStart = 3f;
        surface.smoothnessFadeDistance = 25f;
        surface.absorptionDistance = Mathf.Max(1f, absorptionDistance);
        surface.scatteringColor = waterScatteringColor;
        surface.ambientScattering = 0.35f;
        surface.heightScattering = 0.6f;
        surface.directLightTipScattering = 0.85f;
        surface.displacementScattering = 0.2f;
        surface.customMaterial = waterMaterial;
    }

    private void ConfigureGlobalVolume()
    {
        var volumeTransform = EnsureChild(globalVolume != null ? globalVolume.transform : null, environmentRoot, "Global Underwater Volume");
        volumeTransform.localPosition = Vector3.zero;

        globalVolume = volumeTransform.GetComponent<Volume>();
        if (globalVolume == null)
        {
            globalVolume = volumeTransform.gameObject.AddComponent<Volume>();
        }

        globalVolume.isGlobal = true;
        globalVolume.priority = 50f;
        globalVolume.blendDistance = 3f;
        globalVolume.weight = 1f;

        var profile = EnsureProfile(globalVolume);
        ConfigureGlobalOverrides(profile);
    }

    private void ConfigureGlobalOverrides(VolumeProfile profile)
    {
        var visualEnvironment = GetOrCreate<VisualEnvironment>(profile);
        visualEnvironment.skyType.Override((int)SkyType.PhysicallyBased);

        var pbrSky = GetOrCreate<PhysicallyBasedSky>(profile);
        pbrSky.spaceEmissionTexture.Override(null);

        var fog = GetOrCreate<Fog>(profile);
        fog.enabled.Override(true);
        fog.albedo.Override(globalFogAlbedo);
        fog.tint.Override(globalFogAlbedo);
        fog.color.Override(globalFogAlbedo);
        fog.meanFreePath.Override(Mathf.Max(1f, globalMeanFreePath));
        fog.baseHeight.Override(waterHeight - 4f);
        fog.maximumHeight.Override(waterHeight + 30f);
        fog.enableVolumetricFog.Override(true);
        fog.anisotropy.Override(Mathf.Clamp(globalAnisotropy, -1f, 1f));
        fog.depthExtent.Override(60f);
        fog.globalLightProbeDimmer.Override(0.35f);

        var color = GetOrCreate<ColorAdjustments>(profile);
        color.postExposure.Override(-0.75f);
        color.contrast.Override(25f);
        color.saturation.Override(-28f);
        color.colorFilter.Override(new Color(0.5f, 0.9f, 1f));

        var vignette = GetOrCreate<Vignette>(profile);
        vignette.intensity.Override(0.45f);
        vignette.smoothness.Override(0.65f);
        vignette.rounded.Override(true);

        var bloom = GetOrCreate<Bloom>(profile);
        bloom.intensity.Override(0.35f);
        bloom.scatter.Override(0.7f);
        bloom.tint.Override(new Color(0.35f, 0.5f, 0.55f));

        var grain = GetOrCreate<FilmGrain>(profile);
        grain.type.Override(FilmGrainLookup.Medium1);
        grain.intensity.Override(0.3f);
        grain.response.Override(0.35f);
    }

    private void ConfigureLocalVolume()
    {
        var targetParent = submarineRoot != null ? submarineRoot : environmentRoot;
        var localTransform = EnsureChild(localFog != null ? localFog.transform : null, targetParent, "Local Dread Volume");
        localTransform.localPosition = submarineRoot != null ? Vector3.forward * 8f : Vector3.zero;
        localTransform.localRotation = Quaternion.identity;

        localFog = localTransform.GetComponent<LocalVolumetricFog>();
        if (localFog == null)
        {
            localFog = localTransform.gameObject.AddComponent<LocalVolumetricFog>();
        }

        var parameters = localFog.parameters;
        parameters.albedo = globalFogAlbedo * 0.75f;
        parameters.meanFreePath = Mathf.Max(0.5f, dreadMeanFreePath);
        parameters.anisotropy = Mathf.Clamp(dreadAnisotropy, -1f, 1f);
        parameters.size = dreadVolumeSize;
        parameters.positiveFade = Vector3.one * 0.25f;
        parameters.negativeFade = Vector3.one * 0.25f;
        parameters.distanceFadeStart = 0f;
        parameters.distanceFadeEnd = 15f;
        parameters.priority = 5;
        localFog.parameters = parameters;
    }

    private void ConfigureParticles()
    {
        particleTransform = EnsureChild(particleTransform, environmentRoot, "Suspended Particulates");
        particleTransform.localPosition = new Vector3(0f, waterHeight - 1f, 12f);

        suspendedParticles = particleTransform.GetComponent<ParticleSystem>();
        if (suspendedParticles == null)
        {
            suspendedParticles = particleTransform.gameObject.AddComponent<ParticleSystem>();
        }

        var main = suspendedParticles.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 1800;
        main.startSpeed = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);
        main.startLifetime = new ParticleSystem.MinMaxCurve(12f, 24f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.01f, 0.05f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.7f, 0.9f, 1f, 0.3f));

        var emission = suspendedParticles.emission;
        emission.rateOverTime = particulateRate;

        var shape = suspendedParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(60f, 30f, 80f);

        var colorOverLifetime = suspendedParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(GetParticleGradient());

        var renderer = suspendedParticles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = EnsureParticleMaterial();
        renderer.maxParticleSize = 0.15f;
        if (!suspendedParticles.isPlaying)
        {
            suspendedParticles.Play();
        }
    }

    private void ConfigureCaustics()
    {
        var causticsTransform = EnsureChild(causticsProjector != null ? causticsProjector.transform : null, environmentRoot, "Caustics Projector");
        causticsTransform.localPosition = new Vector3(0f, waterHeight - 0.5f, 0f);
        causticsTransform.localRotation = Quaternion.identity;

        causticsProjector = causticsTransform.GetComponent<DecalProjector>();
        if (causticsProjector == null)
        {
            causticsProjector = causticsTransform.gameObject.AddComponent<DecalProjector>();
        }

        causticsProjector.size = new Vector3(40f, 40f, 30f);
        causticsProjector.fadeFactor = causticsBaseFade;

        var causticMat = EnsureCausticMaterial();
        causticsProjector.material = causticMat;
        causticsProjector.enabled = causticMat != null;
    }

    private void ConfigureHeadlight()
    {
        if (submarineRoot == null)
        {
            return;
        }

        var headlightTransform = EnsureChild(headlightLight != null ? headlightLight.transform : null, submarineRoot, "HDRP Horror Headlight");
        headlightTransform.localPosition = new Vector3(0f, -0.1f, 4.5f);
        headlightTransform.localRotation = Quaternion.identity;

        headlightLight = headlightTransform.GetComponent<Light>();
        if (headlightLight == null)
        {
            headlightLight = headlightTransform.gameObject.AddComponent<Light>();
        }

        headlightLight.type = LightType.Spot;
        headlightLight.color = headlightColor;
        headlightLight.range = headlightRange;
        headlightLight.intensity = headlightIntensity;
        headlightLight.spotAngle = headlightSpotAngle;

        headlightHdData = headlightTransform.GetComponent<HDAdditionalLightData>();
        if (headlightHdData == null)
        {
            headlightHdData = headlightTransform.gameObject.AddComponent<HDAdditionalLightData>();
        }

        headlightHdData.EnableShadows(true);
        headlightHdData.SetRange(headlightRange);
        headlightHdData.SetSpotAngle(headlightSpotAngle, headlightSpotAngle * 0.45f);
        headlightHdData.affectsVolumetric = true;
        headlightHdData.volumetricDimmer = headlightVolumetricDimmer;
        headlightHdData.lightDimmer = 1f;
        headlightHdData.shadowDimmer = 0.9f;
    }

    private Transform EnsureChild(Transform cached, Transform parent, string name)
    {
        if (cached != null)
        {
            return cached;
        }

        Transform child = null;
        if (parent != null)
        {
            child = parent.Find(name);
        }

        if (child == null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            child = go.transform;
        }

        return child;
    }

    private VolumeProfile EnsureProfile(Volume volume)
    {
        if (volume.sharedProfile != null)
        {
            return volume.sharedProfile;
        }

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.hideFlags = HideFlags.DontSave;
        volume.sharedProfile = profile;
        return profile;
    }

    private T GetOrCreate<T>(VolumeProfile profile) where T : VolumeComponent
    {
        if (!profile.TryGet(out T component))
        {
            component = profile.Add<T>(true);
        }

        component.active = true;
        return component;
    }

    private Material EnsureCausticMaterial()
    {
        if (runtimeCausticMaterial != null)
        {
            return runtimeCausticMaterial;
        }

        var shader = Shader.Find("HDRP/Decal/Decal");
        if (shader == null)
        {
            return null;
        }

        runtimeCausticMaterial = new Material(shader)
        {
            hideFlags = HideFlags.DontSave
        };

        runtimeCausticMaterial.SetColor("_BaseColor", new Color(0.35f, 0.6f, 0.75f, 0.55f));
        runtimeCausticMaterial.SetTexture("_BaseColorMap", causticsTexture ?? Texture2D.whiteTexture);
        runtimeCausticMaterial.SetTextureScale("_BaseColorMap", Vector2.one * 4f);
        runtimeCausticMaterial.SetFloat("_Metallic", 0f);
        runtimeCausticMaterial.SetFloat("_Smoothness", 0.7f);
        runtimeCausticMaterial.SetFloat("_DecalBlend", 0.8f);
        runtimeCausticMaterial.EnableKeyword("_ALPHATEST_ON");

        return runtimeCausticMaterial;
    }

    private Material EnsureParticleMaterial()
    {
        if (runtimeParticleMaterial != null)
        {
            return runtimeParticleMaterial;
        }

        var shader = Shader.Find("HDRP/Unlit");
        if (shader == null)
        {
            return null;
        }

        runtimeParticleMaterial = new Material(shader)
        {
            hideFlags = HideFlags.DontSave
        };
        runtimeParticleMaterial.SetColor("_UnlitColor", new Color(0.9f, 0.95f, 1f, 0.25f));
        runtimeParticleMaterial.EnableKeyword("_ALPHABLEND_ON");
        return runtimeParticleMaterial;
    }

    private void AnimateCausticsMaterial()
    {
        if (runtimeCausticMaterial == null)
        {
            return;
        }

        float time = Application.isPlaying ? Time.time : GetEditorTime();
        var offset = new Vector2(time * 0.05f, -time * 0.03f);
        runtimeCausticMaterial.SetTextureOffset("_BaseColorMap", offset);
    }

    private void UpdateEffectVisibility()
    {
        float depthBelowSurface = waterHeight - GetViewerHeight();
        float blend = Mathf.Clamp01(depthBelowSurface / Mathf.Max(0.1f, surfaceBlendDepth));

        if (globalVolume != null)
        {
            globalVolume.weight = blend;
        }

        if (localFog != null)
        {
            localFog.enabled = blend > 0.02f;
        }

        if (suspendedParticles != null)
        {
            var emission = suspendedParticles.emission;
            emission.rateOverTime = particulateRate * blend;
        }

        if (causticsProjector != null)
        {
            if (causticsProjector.material != null)
            {
                causticsProjector.fadeFactor = causticsBaseFade * blend;
            }
            causticsProjector.enabled = blend > 0.02f && causticsProjector.material != null;
        }

        if (headlightLight != null)
        {
            bool enable = blend > 0.02f;
            headlightLight.enabled = enable;
            headlightLight.intensity = headlightIntensity * blend;
        }

        if (headlightHdData != null)
        {
            headlightHdData.lightDimmer = blend;
            headlightHdData.shadowDimmer = 0.9f * blend;
            headlightHdData.volumetricDimmer = headlightVolumetricDimmer * blend;
        }
    }

    private void CleanupRuntimeMaterial(ref Material material)
    {
        if (material == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(material);
        }
        else
        {
            DestroyImmediate(material);
        }

        material = null;
    }

    private static float GetEditorTime()
    {
#if UNITY_EDITOR
        return (float)EditorApplication.timeSinceStartup;
#else
        return Time.time;
#endif
    }

#if UNITY_EDITOR
    private void QueueEditorSetup()
    {
        if (editorSetupQueued)
        {
            return;
        }

        editorSetupQueued = true;
        EditorApplication.delayCall += PerformEditorSetup;
    }

    private void PerformEditorSetup()
    {
        if (!editorSetupQueued)
        {
            return;
        }

        editorSetupQueued = false;
        EditorApplication.delayCall -= PerformEditorSetup;

        if (this == null)
        {
            return;
        }

        EnsureSetup();
    }
#endif

    private Gradient GetParticleGradient()
    {
        if (runtimeGradient != null)
        {
            return runtimeGradient;
        }

        runtimeGradient = new Gradient();
        runtimeGradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.25f, 0.45f, 0.55f), 0f),
                new GradientColorKey(new Color(0.75f, 0.95f, 1f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.3f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            });
        return runtimeGradient;
    }

    private float GetViewerHeight()
    {
        if (Camera.main != null)
        {
            return Camera.main.transform.position.y;
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null && sceneView.camera != null)
            {
                return sceneView.camera.transform.position.y;
            }
        }
#endif

        if (submarineRoot != null)
        {
            return submarineRoot.position.y;
        }

        return transform.position.y;
    }
}
