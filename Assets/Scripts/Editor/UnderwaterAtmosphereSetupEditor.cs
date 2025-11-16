using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public class UnderwaterAtmosphereSetupEditor : SceneSetupTool<UnderwaterAtmosphereConfig>
{
    private const string VolumeObjectName = "Underwater Global Volume";
    private const string DebrisObjectName = "Underwater Debris";

    protected override string DefaultConfigPath => "Assets/Settings/UnderwaterAtmosphereConfig.asset";
    protected override string ConfigMenuName => "Underwater Atmosphere";

    [MenuItem("Tools/Environment/Underwater Atmosphere")]
    private static void Open()
    {
        GetWindow<UnderwaterAtmosphereSetupEditor>();
    }

    protected override void ApplySetup(UnderwaterAtmosphereConfig config)
    {
        if (config == null)
        {
            Debug.LogError("Missing atmosphere config.");
            return;
        }

        if (config.enableFog)
        {
            RenderSettings.fog = true;
            RenderSettings.fogColor = config.fogColor;
            RenderSettings.fogMode = config.fogMode;
            RenderSettings.fogDensity = config.fogDensity;
        }

        VolumeProfile profile = EnsureVolumeProfile(config);
        EnsureGlobalVolume(config, profile);
        EnsureDebris(config);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    protected override void CaptureFromScene(UnderwaterAtmosphereConfig config)
    {
        if (config == null)
        {
            return;
        }

        config.fogColor = RenderSettings.fogColor;
        config.fogMode = RenderSettings.fogMode;
        config.fogDensity = RenderSettings.fogDensity;

        Volume volume = FindOrCreateObject(VolumeObjectName).GetComponent<Volume>();
        if (volume != null)
        {
            config.volumeProfile = volume.sharedProfile;
            config.volumePriority = volume.priority;
            string assetPath = AssetDatabase.GetAssetPath(config.volumeProfile);
            if (!string.IsNullOrEmpty(assetPath))
            {
                config.volumeProfileAssetPath = assetPath;
            }
        }

        GameObject debrisGO = GameObject.Find(DebrisObjectName);
        if (debrisGO != null)
        {
            ParticleSystem ps = debrisGO.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                config.debrisLifetime = main.startLifetime.constant;
                config.debrisSpeed = main.startSpeed.constant;
                config.debrisSizeRange = new Vector2(main.startSize.constantMin, main.startSize.constantMax);
                config.debrisColor = main.startColor.color;
                config.debrisMaxParticles = main.maxParticles;

                var emission = ps.emission;
                config.debrisEmissionRate = emission.rateOverTime.constant;

                var shape = ps.shape;
                config.debrisBoxSize = shape.scale;
            }

            ParticleSystemRenderer renderer = debrisGO.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                config.debrisMaterial = renderer.sharedMaterial;
            }
        }

        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        Debug.Log("Captured underwater atmosphere settings from scene.");
    }

    private VolumeProfile EnsureVolumeProfile(UnderwaterAtmosphereConfig config)
    {
        string assetPath = string.IsNullOrEmpty(config.volumeProfileAssetPath)
            ? "Assets/Settings/UnderwaterVolumeProfile.asset"
            : config.volumeProfileAssetPath;

        VolumeProfile profile = config.volumeProfile;
        if (profile == null)
        {
            profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(assetPath);
        }

        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            EnsureFolderForAsset(assetPath);
            AssetDatabase.CreateAsset(profile, assetPath);
        }

        ConfigureVolumeProfile(profile);

        if (config.volumeProfile != profile)
        {
            config.volumeProfile = profile;
            EditorUtility.SetDirty(config);
        }

        AssetDatabase.SaveAssets();
        return profile;
    }

    private void ConfigureVolumeProfile(VolumeProfile profile)
    {
        // The legacy project no longer ships the URP post-processing components that
        // previously lived here, so this method intentionally does nothing beyond
        // keeping the volume asset alive.
        if (profile == null)
        {
            Debug.LogWarning("Underwater volume profile was missing.");
        }
    }

    private void EnsureGlobalVolume(UnderwaterAtmosphereConfig config, VolumeProfile profile)
    {
        GameObject volumeGO = FindOrCreateObject(VolumeObjectName);
        Volume volume = volumeGO.GetComponent<Volume>();
        if (volume == null)
        {
            volume = volumeGO.AddComponent<Volume>();
        }

        volume.isGlobal = true;
        volume.priority = config.volumePriority;
        volume.sharedProfile = profile;
    }

    private void EnsureDebris(UnderwaterAtmosphereConfig config)
    {
        if (!config.spawnDebris)
        {
            GameObject existing = GameObject.Find(DebrisObjectName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }
            return;
        }

        GameObject debrisGO = FindOrCreateObject(DebrisObjectName);
        ParticleSystem ps = debrisGO.GetComponent<ParticleSystem>();
        if (ps == null)
        {
            ps = debrisGO.AddComponent<ParticleSystem>();
        }

        ConfigureDebris(ps, config);
    }

    private void ConfigureDebris(ParticleSystem ps, UnderwaterAtmosphereConfig config)
    {
        ps.transform.position = Vector3.zero;
        ps.transform.rotation = Quaternion.identity;

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = config.debrisLifetime;
        main.startSpeed = config.debrisSpeed;
        main.startSize = new ParticleSystem.MinMaxCurve(config.debrisSizeRange.x, config.debrisSizeRange.y);
        main.startColor = new ParticleSystem.MinMaxGradient(config.debrisColor);
        main.maxParticles = config.debrisMaxParticles;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = config.debrisEmissionRate;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = config.debrisBoxSize;

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
        velocity.y = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);
        velocity.z = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (renderer == null)
        {
            renderer = ps.gameObject.AddComponent<ParticleSystemRenderer>();
        }

        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sharedMaterial = config.debrisMaterial;
        renderer.enableGPUInstancing = true;
    }

    private void EnsureFolderForAsset(string assetPath)
    {
        string directory = Path.GetDirectoryName(assetPath);
        if (string.IsNullOrEmpty(directory)) return;

        string[] parts = directory.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }
            current = next;
        }
    }

    private GameObject FindOrCreateObject(string name)
    {
        GameObject existing = GameObject.Find(name);
        if (existing != null)
        {
            return existing;
        }

        GameObject go = new GameObject(name);
        return go;
    }
}
