using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Editor utility: cari prefab musuh (mengandung FightAttributes/FightAI atau nama mengandung "enemy"/"soldier")
/// dan tambahkan komponen `SpawnPortalOnDeath` serta set `portalPrefab` ke Assets/PortalToScene3.prefab.
/// Jalankan lewat menu: Tools -> Attach Portal To Enemies
/// </summary>
public static class AttachPortalToEnemiesEditor
{
    [MenuItem("Tools/Attach Portal To Enemies")]
    public static void AttachPortalToEnemies()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        GameObject portal = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PortalToScene3.prefab");
        int attached = 0;

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (string.IsNullOrEmpty(path)) continue;

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (root == null) continue;

            bool looksLikeEnemy = false;
            // heuristics: name or components
            string lname = root.name.ToLower();
            if (lname.Contains("enemy") || lname.Contains("soldier") || lname.Contains("villain") || lname.Contains("boar") || lname.Contains("hare")) looksLikeEnemy = true;

            if (!looksLikeEnemy)
            {
                if (root.GetComponentInChildren(typeof(FightAttributes)) != null) looksLikeEnemy = true;
                if (root.GetComponentInChildren(typeof(FightAI)) != null) looksLikeEnemy = true;
            }

            if (looksLikeEnemy)
            {
                // add SpawnPortalOnDeath if missing
                if (root.GetComponent<SpawnPortalOnDeath>() == null)
                {
                    var comp = root.AddComponent<SpawnPortalOnDeath>();
                    comp.portalPrefab = portal;
                    comp.spawnDelay = 0f;
                    comp.onlyOnce = true;
                    attached++;

                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
            }

            PrefabUtility.UnloadPrefabContents(root);
        }

        EditorUtility.DisplayDialog("Attach Portal", $"Attached SpawnPortalOnDeath to {attached} prefabs.", "OK");
    }
}
