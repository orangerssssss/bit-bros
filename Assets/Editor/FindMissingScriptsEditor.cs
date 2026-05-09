using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Editor utility: scan selected GameObjects (or entire open scenes) and Project prefabs
/// for missing MonoBehaviour components and log the results. Helps diagnose
/// SerializedObjectNotCreatableException / Inspector NRE caused by missing scripts.
/// </summary>
public static class FindMissingScriptsEditor
{
    [MenuItem("Tools/Find Missing Scripts/Selected GameObjects")]
    public static void FindInSelected()
    {
        var objs = Selection.gameObjects;
        if (objs == null || objs.Length == 0)
        {
            Debug.Log("FindMissingScripts: No GameObjects selected.");
            return;
        }
        int total = 0;
        foreach (var go in objs)
        {
            total += FindInGameObject(go);
        }
        Debug.Log($"FindMissingScripts: Completed scan for {objs.Length} selected GameObjects. Missing count={total}.");
    }

    [MenuItem("Tools/Find Missing Scripts/All Open Scenes")]
    public static void FindInOpenScenes()
    {
        int total = 0;
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;
            var roots = scene.GetRootGameObjects();
            foreach (var r in roots) total += FindInGameObject(r);
        }
        Debug.Log($"FindMissingScripts: Completed scan for open scenes. Missing count={total}.");
    }

    [MenuItem("Tools/Find Missing Scripts/All Project Prefabs (slow)")]
    public static void FindInProjectPrefabs()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        int total = 0;
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;
            total += FindInPrefab(prefab, path);
        }
        Debug.Log($"FindMissingScripts: Completed scan of {guids.Length} prefabs. Missing count={total}.");
    }

    private static int FindInPrefab(GameObject prefab, string path)
    {
        int missing = 0;
        var components = prefab.GetComponentsInChildren<Component>(true);
        foreach (var c in components)
        {
            if (c == null)
            {
                Debug.LogWarning($"Missing script in prefab '{path}'.");
                missing++;
            }
        }
        return missing;
    }

    private static int FindInGameObject(GameObject go)
    {
        int missing = 0;
        var components = go.GetComponentsInChildren<Component>(true);
        foreach (var c in components)
        {
            if (c == null)
            {
                string path = GetGameObjectPath(go);
                Debug.LogWarning($"Missing script on GameObject '{path}' (in scene '{go.scene.name}').");
                missing++;
            }
        }
        return missing;
    }

    private static string GetGameObjectPath(GameObject go)
    {
        string path = go.name;
        Transform t = go.transform;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}
