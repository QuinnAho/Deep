using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Deep/Underwater Atmosphere Config", fileName = "UnderwaterAtmosphereConfig")]
public class UnderwaterAtmosphereConfig : ScriptableObject
{
    [Header("Fog Settings")]
    public bool enableFog = true;
    public Color fogColor = new Color(0.02f, 0.08f, 0.12f);
    public FogMode fogMode = FogMode.Exponential;
    public float fogDensity = 0.15f;

    [Header("Volume")]
    public string volumeProfileAssetPath = "Assets/Settings/UnderwaterVolumeProfile.asset";
    public VolumeProfile volumeProfile;
    public float volumePriority = 1f;

    [Header("Debris")]
    public bool spawnDebris = true;
    public Material debrisMaterial;
    public Vector3 debrisBoxSize = new Vector3(100f, 50f, 100f);
    public float debrisLifetime = 30f;
    public float debrisSpeed = 0.05f;
    public Vector2 debrisSizeRange = new Vector2(0.02f, 0.08f);
    public Color debrisColor = new Color(0.4f, 0.45f, 0.5f, 0.4f);
    public int debrisMaxParticles = 800;
    public float debrisEmissionRate = 40f;
}
