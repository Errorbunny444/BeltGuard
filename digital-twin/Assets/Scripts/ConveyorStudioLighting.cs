using UnityEngine;
using UnityEngine.Rendering;

// Neutral light and reflection recipes adapted from the supplied metrology scene.
public class ConveyorStudioLighting : MonoBehaviour
{
    private Cubemap reflection;
    void Awake()
    {
        foreach (var light in FindObjectsByType<Light>())
            if (light.type == LightType.Directional) light.enabled = false;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(.54f, .58f, .63f);
        RenderSettings.ambientEquatorColor = new Color(.36f, .38f, .41f);
        RenderSettings.ambientGroundColor = new Color(.23f, .24f, .25f);
        RenderSettings.fog = false;
        if (Camera.main)
        {
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            Camera.main.backgroundColor = new Color(.25f, .28f, .31f);
        }
        AddLight("Neutral overhead", new Vector3(52, -28, 0), new Color(1, .98f, .94f), 1.05f, true);
        AddLight("Cool room fill", new Vector3(26, 122, 0), new Color(.82f, .88f, 1), .42f, false);
        AddLight("Rear edge light", new Vector3(36, 175, 0), Color.white, .62f, false);
        const int size = 64;
        reflection = new Cubemap(size, TextureFormat.RGBAHalf, true);
        for (int face = 0; face < 6; face++)
        {
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float u = (x + .5f) / size, v = (y + .5f) / size;
                Color color = Color.Lerp(new Color(.19f, .21f, .23f), new Color(.64f, .67f, .70f), v);
                if (face == (int)CubemapFace.PositiveY) color = new Color(.76f, .78f, .79f);
                if (face == (int)CubemapFace.NegativeY) color = new Color(.23f, .25f, .27f);
                float strip = Mathf.Exp(-Mathf.Pow((u - .34f) * 11, 2)) * Mathf.SmoothStep(.45f, .9f, v);
                pixels[y * size + x] = Color.Lerp(color, new Color(.96f, .97f, .98f), strip * .85f);
            }
            reflection.SetPixels(pixels, (CubemapFace)face);
        }
        reflection.Apply();
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
        RenderSettings.customReflectionTexture = reflection;
        RenderSettings.reflectionIntensity = .9f;
    }
    void AddLight(string name, Vector3 angles, Color color, float intensity, bool shadows)
    {
        var light = new GameObject(name).AddComponent<Light>();
        light.transform.SetParent(transform); light.transform.rotation = Quaternion.Euler(angles);
        light.type = LightType.Directional; light.color = color; light.intensity = intensity;
        if (shadows) RenderSettings.sun = light;
        light.shadows = shadows ? LightShadows.Soft : LightShadows.None;
        light.shadowStrength = .68f; light.shadowBias = .015f; light.shadowNormalBias = .025f;
    }
    void OnDestroy() { if (reflection) Destroy(reflection); }
}
