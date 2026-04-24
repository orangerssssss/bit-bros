using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor utility: create preview prefabs under Assets/Resources/PreviewPrefabs
/// from model assets (FBX) found in the project. Run from menu: Tools/Create Preview Prefabs
/// </summary>
public static class PreviewPrefabCreator
{
    [MenuItem("Tools/Create Preview Prefabs (arthur)")]
    public static void CreateArthurPreviewPrefab()
    {
        CreatePreviewPrefabsForName("arthurbland");
    }

    public static void CreatePreviewPrefabsForName(string nameQuery)
    {
        string filter = nameQuery + " t:Model";
        string[] guids = AssetDatabase.FindAssets(filter);
        if (guids == null || guids.Length == 0)
        {
            Debug.LogWarning($"PreviewPrefabCreator: no model assets found matching '{nameQuery}'.");
            return;
        }

        string outFolder = "Assets/Resources/PreviewPrefabs";
        if (!AssetDatabase.IsValidFolder(outFolder))
        {
            AssetDatabase.CreateFolder("Assets/Resources", "PreviewPrefabs");
        }

        int created = 0;
        foreach (var g in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (model == null) continue;

            string prefabPath = outFolder + "/" + model.name + ".prefab";
            // Save the model asset itself as a prefab so Resources.Load can find it at runtime
            var prefab = PrefabUtility.SaveAsPrefabAsset(model, prefabPath);
            if (prefab != null) { created++; Debug.Log($"Created preview prefab: {prefabPath} from {path}"); }
        }

        if (created == 0) Debug.LogWarning("PreviewPrefabCreator: no prefabs created.");
        else Debug.Log($"PreviewPrefabCreator: created {created} prefabs under {outFolder}.");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
