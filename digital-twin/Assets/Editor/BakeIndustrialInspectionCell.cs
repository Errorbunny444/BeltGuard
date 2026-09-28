using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Saves the visual inspection rig as ordinary scene objects.  This is intentionally
// an editor-only command: UDP, health state and animation remain Play-mode services.
public static class BakeIndustrialInspectionCell
{
    const string ScenePath = "Assets/Scenes/SampleScene.unity";
    const string AssetFolder = "Assets/Generated/BeltHealthIndustrial";
    const int CurrentStructureVersion = 5;

    [MenuItem("Belt Health/Bake industrial inspection cell into SampleScene")]
    public static void Bake()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != ScenePath)
            EditorSceneManager.OpenScene(ScenePath);
        BakeOpenScene();
    }

    [InitializeOnLoadMethod]
    static void RestoreCellInOpenScene()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != ScenePath) return;
            var setup = Object.FindAnyObjectByType<BeltHealthSceneSetup>();
            if (setup && setup.bakedStructureVersion < CurrentStructureVersion) BakeOpenScene();
        };
    }

    static void BakeOpenScene()
    {
        var setup = Object.FindAnyObjectByType<BeltHealthSceneSetup>();
        if (!setup || !setup.existingBelt)
            throw new System.InvalidOperationException("BeltHealthSystem and its CAD belt reference are required.");

        var beltRenderer = setup.existingBelt.GetComponent<Renderer>();
        if (!beltRenderer) throw new System.InvalidOperationException("The assigned CAD belt has no renderer.");
        Bounds belt = beltRenderer.bounds;
        bool alongX = belt.size.x >= belt.size.z;
        EnsureFolder("Assets/Generated");
        EnsureFolder(AssetFolder);

        // Remove only the previous generated cell. Imported CAD is never touched.
        var oldCell = setup.GetComponentInChildren<IndustrialInspectionCell>(true);
        if (oldCell) Object.DestroyImmediate(oldCell.gameObject);

        foreach (string oldName in new[] { "Wall", "LeftWall", "RightWall", "WallAccent", "Floor", "WorkcellPad" })
        {
            var old = GameObject.Find(oldName);
            if (old) old.SetActive(false);
        }

        ApplyHealthyGreenBelt(beltRenderer);
        ConfigureOverviewCamera(belt, alongX);
        CreateSavedLights(setup.transform);

        var cellObject = new GameObject("IndustrialInspectionCell");
        cellObject.transform.SetParent(setup.transform, false);
        var cell = cellObject.AddComponent<IndustrialInspectionCell>();
        cell.Build(belt, alongX);
        PersistGeneratedAssets(cellObject);

        setup.sceneEquipmentBaked = true;
        setup.bakedStructureVersion = CurrentStructureVersion;
        EditorUtility.SetDirty(setup);
        EditorSceneManager.MarkSceneDirty(setup.gameObject.scene);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(setup.gameObject.scene);
        Debug.Log("[SIH26008] Industrial inspection cell baked into SampleScene. It is now visible outside Play mode.");
        if (Application.isBatchMode) EditorApplication.Exit(0);
    }

    static void ApplyHealthyGreenBelt(Renderer belt)
    {
        var source = belt.sharedMaterial;
        var green = AssetDatabase.LoadAssetAtPath<Material>(AssetFolder + "/Healthy conveyor belt green.mat");
        if (!green)
        {
            green = source ? new Material(source) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            green.name = "Healthy conveyor belt green";
            AssetDatabase.CreateAsset(green, AssetFolder + "/Healthy conveyor belt green.mat");
        }
        Color colour = new Color(.035f, .28f, .095f, 1);
        green.color = colour;
        if (green.HasProperty("_BaseColor")) green.SetColor("_BaseColor", colour);
        if (green.HasProperty("_Metallic")) green.SetFloat("_Metallic", .02f);
        if (green.HasProperty("_Smoothness")) green.SetFloat("_Smoothness", .26f);
        belt.sharedMaterial = green;
        EditorUtility.SetDirty(green);
        EditorUtility.SetDirty(belt);
    }

    static void ConfigureOverviewCamera(Bounds belt, bool alongX)
    {
        var camera = Camera.main ? Camera.main : Object.FindAnyObjectByType<Camera>();
        if (!camera) return;
        camera.tag = "MainCamera";
        Vector3 axis = alongX ? Vector3.right : Vector3.forward;
        Vector3 across = alongX ? Vector3.forward : Vector3.right;
        float length = alongX ? belt.size.x : belt.size.z;
        camera.transform.position = belt.center + axis * length * .4f - across * length * .95f + Vector3.up * length * .8f;
        camera.transform.LookAt(belt.center);
        camera.fieldOfView = 45;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.25f, .28f, .31f);
        EditorUtility.SetDirty(camera);
    }

    static void CreateSavedLights(Transform parent)
    {
        foreach (var light in Object.FindObjectsByType<Light>(FindObjectsInactive.Include))
            if (light.type == LightType.Directional) light.enabled = false;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(.54f, .58f, .63f);
        RenderSettings.ambientEquatorColor = new Color(.36f, .38f, .41f);
        RenderSettings.ambientGroundColor = new Color(.23f, .24f, .25f);
        RenderSettings.fog = false;
        AddDirectional(parent, "Baked neutral overhead", new Vector3(52, -28, 0), new Color(1, .98f, .94f), 1.05f, true);
        AddDirectional(parent, "Baked cool room fill", new Vector3(26, 122, 0), new Color(.82f, .88f, 1), .42f, false);
        AddDirectional(parent, "Baked rear edge light", new Vector3(36, 175, 0), Color.white, .62f, false);
    }

    static void AddDirectional(Transform parent, string name, Vector3 angles, Color colour, float intensity, bool shadows)
    {
        var old = parent.Find(name);
        if (old) Object.DestroyImmediate(old.gameObject);
        var light = new GameObject(name).AddComponent<Light>();
        light.transform.SetParent(parent, false);
        light.transform.rotation = Quaternion.Euler(angles);
        light.type = LightType.Directional; light.color = colour; light.intensity = intensity;
        light.shadows = shadows ? LightShadows.Soft : LightShadows.None;
        light.shadowStrength = .68f; light.shadowBias = .02f; light.shadowNormalBias = .04f;
        if (shadows) RenderSettings.sun = light;
    }

    static void PersistGeneratedAssets(GameObject root)
    {
        var saved = new HashSet<Object>();
        int index = 0;
        foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
            Persist(filter.sharedMesh, "mesh", saved, ref index);
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            foreach (var material in renderer.sharedMaterials) Persist(material, "material", saved, ref index);
    }

    static void Persist(Object asset, string kind, HashSet<Object> saved, ref int index)
    {
        if (!asset || AssetDatabase.Contains(asset) ||
            (asset.hideFlags & HideFlags.DontSaveInEditor) != 0 || !saved.Add(asset)) return;
        string clean = string.Concat(asset.name.Split(Path.GetInvalidFileNameChars()));
        if (string.IsNullOrWhiteSpace(clean)) clean = kind;
        string path = AssetFolder + "/" + kind + "_" + index++ + "_" + clean + ".asset";
        AssetDatabase.CreateAsset(asset, AssetDatabase.GenerateUniqueAssetPath(path));
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
