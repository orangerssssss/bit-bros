using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor utility: assign preview prefab assets under Assets/Resources/PreviewPrefabs
/// to any InventoryPlayerPreview components in the open scenes that have a null previewPrefab.
/// Run: Tools -> Assign Inventory Preview Prefabs
/// </summary>
public static class InventoryPreviewAutoAssign
{
    [MenuItem("Tools/Assign Inventory Preview Prefabs")]
    public static void AssignPreviewPrefabs()
    {
        const string searchFolder = "Assets/Resources/PreviewPrefabs";

        if (!AssetDatabase.IsValidFolder(searchFolder))
        {
            Debug.LogWarning($"InventoryPreviewAutoAssign: folder '{searchFolder}' not found. Run Tools/Create Preview Prefabs first.");
            return;
        }

        // Gather prefabs under the PreviewPrefabs folder
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { searchFolder });
        var prefabs = new List<GameObject>();
        foreach (var g in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go != null) prefabs.Add(go);
        }

        if (prefabs.Count == 0)
        {
            Debug.LogWarning($"InventoryPreviewAutoAssign: no prefabs found in {searchFolder}. Run Tools/Create Preview Prefabs.");
            return;
        }

        // Find InventoryPlayerPreview components in open scenes (include inactive)
        var compsAll = Resources.FindObjectsOfTypeAll<InventoryPlayerPreview>();
        int assigned = 0;
        foreach (var comp in compsAll)
        {
            // skip assets/prefab instances stored on disk
            if (EditorUtility.IsPersistent(comp)) continue;

            if (comp.previewPrefab == null)
            {
                GameObject chosen = null;
                // Try to match name from defaultPreviewResourcePath
                if (!string.IsNullOrEmpty(comp.defaultPreviewResourcePath))
                {
                    string want = comp.defaultPreviewResourcePath;
                    int idx = want.LastIndexOf('/');
                    if (idx >= 0) want = want.Substring(idx + 1);
                    foreach (var p in prefabs)
                    {
                        if (p.name.ToLower().Contains(want.ToLower())) { chosen = p; break; }
                    }
                }
                if (chosen == null) chosen = prefabs[0];

                Undo.RecordObject(comp, "Assign previewPrefab");
                comp.previewPrefab = chosen;
                EditorUtility.SetDirty(comp);
                assigned++;
            }
        }

        Debug.Log($"InventoryPreviewAutoAssign: assigned previewPrefab to {assigned} InventoryPlayerPreview component(s). If 0, either none were missing or no scene components found.");
    }
}
