using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

/// <summary>
/// Editor helper to set up a RenderTexture-based inventory preview: creates scene root, model container,
/// preview Camera rendering a dedicated layer, a RenderTexture asset, and a RawImage under the package UI.
/// Run: Tools -> Setup Inventory Preview
/// </summary>
public static class InventoryPreviewSetup
{
    [MenuItem("Tools/Setup Inventory Preview")]
    public static void SetupInventoryPreview()
    {
        const int previewLayer = 5; // default used by InventoryPlayerPreview
        const string rtFolder = "Assets/Resources/PreviewRenderTextures";
        const string rtPath = rtFolder + "/InventoryPreviewRT.asset";

        // Ensure folder exists
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(rtFolder)) AssetDatabase.CreateFolder("Assets/Resources", "PreviewRenderTextures");

        // Create RenderTexture asset if missing
        RenderTexture rt = AssetDatabase.LoadAssetAtPath<RenderTexture>(rtPath);
        if (rt == null)
        {
            rt = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32);
            rt.name = "InventoryPreviewRT";
            AssetDatabase.CreateAsset(rt, rtPath);
            AssetDatabase.SaveAssets();
            Debug.Log("InventoryPreviewSetup: created RenderTexture asset at " + rtPath);
        }

        // Create scene root container
        GameObject root = GameObject.Find("InventoryPreviewScene");
        if (root == null)
        {
            root = new GameObject("InventoryPreviewScene");
            Undo.RegisterCreatedObjectUndo(root, "Create InventoryPreviewScene");
        }

        // Create model root
        Transform modelRoot = root.transform.Find("InventoryPreviewModelRoot");
        if (modelRoot == null)
        {
            var go = new GameObject("InventoryPreviewModelRoot");
            go.transform.SetParent(root.transform, false);
            modelRoot = go.transform;
            Undo.RegisterCreatedObjectUndo(go, "Create InventoryPreviewModelRoot");
        }
        modelRoot.gameObject.layer = previewLayer;

        // Create preview camera
        Transform camT = root.transform.Find("InventoryPreviewCamera");
        Camera cam = null;
        if (camT == null)
        {
            var camGo = new GameObject("InventoryPreviewCamera");
            camGo.transform.SetParent(root.transform, false);
            cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0, 0, 0, 0);
            cam.cullingMask = 1 << previewLayer;
            cam.targetTexture = rt;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;
            cam.transform.localPosition = new Vector3(0, 1.0f, -3f);
            cam.transform.localRotation = Quaternion.Euler(10f, 0f, 0f);
            Undo.RegisterCreatedObjectUndo(camGo, "Create InventoryPreviewCamera");
        }
        else
        {
            cam = camT.GetComponent<Camera>();
            if (cam == null) cam = camT.gameObject.AddComponent<Camera>();
            cam.cullingMask = 1 << previewLayer;
            cam.targetTexture = rt;
        }

        // Ensure model root uses preview layer for all children
        SetLayerRecursively(modelRoot.gameObject, previewLayer);

        // Try to find GameUIManager package and create RawImage
        var gui = GameObject.FindObjectOfType<GameUIManager>();
        if (gui != null && gui.package != null)
        {
            Transform existing = gui.package.transform.Find("InventoryPreviewRawImage");
            GameObject rawGO;
            if (existing == null)
            {
                rawGO = new GameObject("InventoryPreviewRawImage", typeof(RectTransform), typeof(RawImage));
                rawGO.transform.SetParent(gui.package.transform, false);
                var rtComp = rawGO.GetComponent<RawImage>();
                rtComp.texture = rt;
                var rect = rawGO.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(256, 256);
                rect.anchoredPosition = new Vector2(0, 0);
                Undo.RegisterCreatedObjectUndo(rawGO, "Create InventoryPreviewRawImage");
                Debug.Log("InventoryPreviewSetup: created RawImage under package UI and assigned RenderTexture.");
            }
            else
            {
                rawGO = existing.gameObject;
                var rtComp = rawGO.GetComponent<RawImage>();
                if (rtComp != null) rtComp.texture = rt;
            }
        }
        else
        {
            Debug.LogWarning("InventoryPreviewSetup: GameUIManager or package not found in the active scene. Created preview scene and camera only.");
        }

        // Finalize
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("InventoryPreviewSetup: setup complete. Place preview models as children of InventoryPreviewModelRoot; the camera renders them into InventoryPreviewRT.");
    }

    private static void SetLayerRecursively(GameObject go, int layer)
    {
        if (go == null) return;
        go.layer = layer;
        foreach (Transform t in go.transform)
            SetLayerRecursively(t.gameObject, layer);
    }
}
