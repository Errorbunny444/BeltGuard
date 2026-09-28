using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class ReferenceConveyorMaterials
{
    [MenuItem("Belt Health/Apply reference conveyor finishes")]
    public static void ApplyAndSave()
    {
        if (Application.isPlaying) return;
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        var root = GameObject.Find("FlatBelt v7");
        if (!root) throw new System.Exception("Existing FlatBelt v7 CAD root not found");
        // Some animated parts were reparented in the old scene. Match by the original CAD mesh,
        // rather than assuming that every belt/pulley is still beneath the assembly root.
        var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/FlatBelt v7.fbx");
        var reference = Object.Instantiate(source);
        RealisticConveyorAppearance.ApplyMaterials(reference.GetComponentsInChildren<Renderer>(true), reference.transform);
        var recipes = new Dictionary<Mesh, Material[]>();
        foreach (var filter in reference.GetComponentsInChildren<MeshFilter>(true))
            if (filter.sharedMesh && filter.GetComponent<Renderer>())
                recipes[filter.sharedMesh] = filter.GetComponent<Renderer>().sharedMaterials;
        var renderers = new List<Renderer>();
        foreach (var filter in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include))
        {
            if (filter.transform.IsChildOf(reference.transform)) continue;
            var renderer = filter.GetComponent<Renderer>();
            if (renderer && filter.sharedMesh && recipes.TryGetValue(filter.sharedMesh, out var finishes))
            { renderer.sharedMaterials = finishes; renderers.Add(renderer); }
        }
        Object.DestroyImmediate(reference);
        const string folder = "Assets/Materials/IndustrialReference";
        Directory.CreateDirectory(folder);
        var saved = new Dictionary<Material, Material>();
        foreach (var renderer in renderers)
        {
            var materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                var generated = materials[i];
                if (!saved.TryGetValue(generated, out var persistent))
                {
                    string path = folder + "/" + generated.name.Replace(" | ", " - ") + ".mat";
                    persistent = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (persistent) EditorUtility.CopySerialized(generated, persistent);
                    else { persistent = generated; AssetDatabase.CreateAsset(persistent, path); }
                    saved.Add(generated, persistent);
                }
                materials[i] = persistent;
            }
            renderer.sharedMaterials = materials;
        }
        foreach (var pair in saved) if (pair.Key != pair.Value) Object.DestroyImmediate(pair.Key);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(root.scene);
        EditorSceneManager.SaveScene(root.scene);
        Debug.Log($"[REFERENCE FINISHES] Saved {saved.Count} reference materials on {renderers.Count} CAD renderers.");
    }
}
