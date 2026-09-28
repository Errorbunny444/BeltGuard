using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

// Editor-only material/lighting pass. Never runs health or conveyor controls.
public static class IndustrialVisualPolish
{
    const string Folder = "Assets/Generated/IndustrialPolish";
    const string Marker = "Visual Polish v1";

    [InitializeOnLoadMethod]
    static void QueueOpenScene()
    {
        EditorApplication.delayCall += () =>
        {
            if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != "Assets/Scenes/SampleScene.unity") return;
            if (!GameObject.Find(Marker)) Apply();
        };
    }

    [MenuItem("Belt Health/Phase 1 - Apply visual polish")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var setup = Object.FindAnyObjectByType<BeltHealthSceneSetup>();
        var cell = setup ? setup.GetComponentInChildren<IndustrialInspectionCell>(true) : null;
        if (!cell || !setup.existingBelt) throw new InvalidOperationException("Open the existing baked SampleScene first.");
        if (cell.transform.Find(Marker)) return;
        var original = new Dictionary<Transform, Matrix4x4>();
        var activeStates = new Dictionary<GameObject, bool>();
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
        {
            if (t.gameObject.scene != setup.gameObject.scene) continue;
            original[t] = t.localToWorldMatrix; activeStates[t.gameObject] = t.gameObject.activeSelf;
        }
        Directory.CreateDirectory("MigrationBackup/phase1_visuals");
        // Save a separate copy of the current open scene, including unsaved user edits.
        EditorSceneManager.SaveScene(setup.gameObject.scene, "MigrationBackup/phase1_visuals/SampleScene.unity", true);
        Directory.CreateDirectory(Folder);
        AssetDatabase.Refresh();
        var materials = new Dictionary<Material, Material>();
        var grain = new Texture2D(128, 128, TextureFormat.RGBA32, true) { name = "Fine concrete grain", wrapMode = TextureWrapMode.Repeat };
        for (int y = 0; y < 128; y++) for (int x = 0; x < 128; x++)
        {
            float v = .86f + .14f * Mathf.PerlinNoise(x * .4f, y * .4f);
            grain.SetPixel(x, y, new Color(v, v, v));
        }
        grain.Apply();
        AssetDatabase.CreateAsset(grain, AssetDatabase.GenerateUniqueAssetPath(Folder + "/Concrete grain.asset"));
        foreach (var renderer in Object.FindObjectsByType<MeshRenderer>())
        {
            if (renderer.GetComponent<TMP_Text>()) continue;
            // Only the existing industrial cell and the known CAD material family.
            var slots = renderer.sharedMaterials;
            for (int i = 0; i < slots.Length; i++)
            {
                var source = slots[i];
                if (!source || (!renderer.transform.IsChildOf(cell.transform) &&
                    !source.name.StartsWith("Conveyor") && !source.name.StartsWith("Healthy conveyor belt"))) continue;
                if (!materials.TryGetValue(source, out var finish))
                {
                    finish = new Material(source) { name = source.name + " - polished" };
                    string n = source.name.ToLowerInvariant();
                    if (n.Contains("concrete")) Set(finish, new Color(.32f,.34f,.35f), 0, .2f);
                    else if (n.Contains("wall")) Set(finish, new Color(.43f,.46f,.48f), 0, .18f);
                    else if (n.Contains("aluminium")) Set(finish, new Color(.57f,.61f,.64f), .78f, .43f);
                    else if (n.Contains("stainless")) Set(finish, new Color(.65f,.68f,.7f), .88f, .55f);
                    else if (n.Contains("graphite")) Set(finish, new Color(.075f,.09f,.105f), .25f, .32f);
                    else if (n.Contains("yellow")) Set(finish, new Color(.95f,.64f,.045f), 0, .26f);
                    else if (n.Contains("belt")) Set(finish, new Color(.035f,.28f,.095f), .02f, .22f);
                    if (n.Contains("concrete")) { finish.SetTexture("_BaseMap", grain); finish.SetTextureScale("_BaseMap", new Vector2(18,22)); }
                    if (n.Contains("inspection led"))
                    {
                        finish.EnableKeyword("_EMISSION");
                        finish.SetColor("_EmissionColor", new Color(.7f,.82f,1) * 1.4f);
                    }
                    string path = Folder + "/Finish " + materials.Count + ".mat";
                    AssetDatabase.CreateAsset(finish, AssetDatabase.GenerateUniqueAssetPath(path));
                    materials.Add(source, finish);
                }
                slots[i] = finish;
            }
            renderer.sharedMaterials = slots;
        }
        var workshop = Group(cell.transform, "01 Workshop");
        var inspection = Group(cell.transform, "02 Inspection Equipment");
        var safety = Group(cell.transform, "03 Safety Markings");
        var lighting = Group(cell.transform, "04 Lighting and Reflections");
        AddCautionLabels(cell.transform, safety);
        var children = new List<Transform>();
        foreach (Transform child in cell.transform) children.Add(child);
        foreach (var child in children)
        {
            if (child.name.StartsWith("0")) continue;
            string n = child.name.ToLowerInvariant();
            var target = n.Contains("hazard") || n.Contains("boundary") || n.Contains("plate") ? safety :
                n.Contains("workshop") || n.Contains("wall") || n.Contains("structural") || n.Contains("foundation") || n.Contains("skid") ? workshop : inspection;
            child.SetParent(target, true);
        }
        foreach (var light in setup.GetComponentsInChildren<Light>(true))
        {
            if (light.type != LightType.Directional || !light.name.StartsWith("Baked")) continue;
            light.transform.SetParent(lighting, true);
            if (light.name.Contains("overhead"))
            {
                light.intensity = 1.25f; light.color = new Color(1,.97f,.93f);
                light.shadows = LightShadows.Soft; light.shadowStrength = .78f;
                light.shadowBias = .05f; light.shadowNormalBias = .1f;
            }
            else light.intensity = light.name.Contains("fill") ? .48f : .7f;
        }
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(.53f,.58f,.64f);
        RenderSettings.ambientEquatorColor = new Color(.32f,.36f,.4f);
        RenderSettings.ambientGroundColor = new Color(.18f,.2f,.22f);
        var bounds = setup.existingBelt.GetComponent<Renderer>().bounds;
        var probe = new GameObject("Conveyor reflection probe").AddComponent<ReflectionProbe>();
        probe.transform.SetParent(lighting, false);
        probe.transform.position = bounds.center + Vector3.up * bounds.size.z;
        // Probe bounds are in metres/scene units, not the generated cell's local scale.
        probe.transform.SetParent(setup.transform, true);
        probe.transform.localScale = Vector3.one;
        probe.size = new Vector3(bounds.size.x + 35, 65, bounds.size.z + 70);
        probe.boxProjection = true; probe.resolution = 128; probe.intensity = .8f;
        probe.clearFlags = ReflectionProbeClearFlags.SolidColor;
        probe.backgroundColor = new Color(.4f,.44f,.49f);
        probe.mode = ReflectionProbeMode.Baked;
        foreach (var text in cell.GetComponentsInChildren<TextMeshPro>(true))
        {
            if (text.name == "Warning label text") continue;
            text.color = new Color(.95f,.97f,1);
            text.fontSizeMax = 4; text.fontSizeMin = .2f;
            text.enableAutoSizing = true;
            text.ForceMeshUpdate();
        }
        if (Camera.main)
        {
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            Camera.main.backgroundColor = new Color(.29f,.33f,.37f);
            Camera.main.allowMSAA = true;
        }
        foreach (var pair in original)
        {
            if (!pair.Key || pair.Key.gameObject.activeSelf != activeStates[pair.Key.gameObject])
                throw new InvalidOperationException("An existing object's active state changed during polish.");
            var now = pair.Key.localToWorldMatrix;
            for (int i = 0; i < 16; i++)
                if (Mathf.Abs(now[i] - pair.Value[i]) > .001f)
                    throw new InvalidOperationException("Existing transform changed: " + pair.Key.name);
        }
        Group(cell.transform, Marker);
        AssetDatabase.SaveAssets();
        bool baked = Lightmapping.BakeReflectionProbe(probe, Folder + "/Conveyor reflection.exr");
        EditorSceneManager.MarkSceneDirty(setup.gameObject.scene);
        EditorSceneManager.SaveScene(setup.gameObject.scene);
        Directory.CreateDirectory("Tools/Preview");
        Capture();
        File.WriteAllText("Tools/phase1_visuals.txt", $"Visual pass saved. Material finishes: {materials.Count}. Reflection bake: {baked}. Verified {original.Count} existing transforms and active states. Runtime scripts unchanged.");
        Debug.Log("[Phase 1] Visual polish saved; runtime scripts unchanged.");
    }

    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        Apply();
        EditorApplication.Exit(0);
    }
    static Transform Group(Transform parent, string name)
    {
        var group = parent.Find(name);
        if (!group) { group = new GameObject(name).transform; group.SetParent(parent, false); }
        return group;
    }
    static void Set(Material material, Color color, float metal, float smooth)
    {
        material.SetColor("_BaseColor", color); material.color = color;
        material.SetFloat("_Metallic", metal); material.SetFloat("_Smoothness", smooth);
    }
    static void AddCautionLabels(Transform cell, Transform parent)
    {
        var paint = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Safety label yellow" };
        Set(paint, new Color(.95f,.69f,.08f), 0, .2f);
        AssetDatabase.CreateAsset(paint, AssetDatabase.GenerateUniqueAssetPath(Folder + "/Warning label.mat"));
        foreach (int side in new[] { -1, 1 })
        {
            var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = "Caution - moving belt";
            board.transform.SetParent(cell, false);
            board.transform.localPosition = new Vector3(side * .72f, .92f, -.085f);
            board.transform.localScale = new Vector3(.3f,.17f,.008f);
            Object.DestroyImmediate(board.GetComponent<Collider>());
            board.GetComponent<Renderer>().sharedMaterial = paint;
            board.transform.SetParent(parent, true);
            var label = new GameObject("Warning label text").AddComponent<TextMeshPro>();
            label.transform.SetParent(cell, false);
            label.transform.localPosition = new Vector3(side * .72f,.92f,-.091f);
            label.rectTransform.sizeDelta = new Vector2(.28f,.15f);
            label.text = "CAUTION\nMOVING BELT\nKEEP HANDS CLEAR";
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(.035f,.04f,.045f);
            label.enableAutoSizing = true; label.fontSizeMin = .1f; label.fontSizeMax = 3;
            label.transform.SetParent(parent,true);
        }
    }
    static void Capture()
    {
        var camera = Camera.main;
        if (!camera) return;
        var target = new RenderTexture(1280,720,24);
        var old = camera.targetTexture; var active = RenderTexture.active;
        var pixels = new Texture2D(1280,720,TextureFormat.RGB24,false);
        try
        {
            camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0,0,1280,720),0,0); pixels.Apply();
            File.WriteAllBytes("Tools/Preview/phase1_edit.png", pixels.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = old; RenderTexture.active = active;
            target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(pixels);
        }
    }
}
