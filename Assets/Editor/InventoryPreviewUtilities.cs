using UnityEngine;
using UnityEditor;

/// <summary>
/// Small Editor utilities to manage the Inventory Preview objects created by InventoryPreviewSetup.
/// Provides safe removal of preview camera/scene and reassigns RenderTexture to RawImage if needed.
/// Uses string-based GetComponent to avoid hard dependency on URP assemblies.
/// </summary>
public static class InventoryPreviewUtilities
{
    [MenuItem("Tools/Inventory Preview/Remove Preview Camera (safe)")]
    public static void RemovePreviewCamera()
    {
        var camGo = GameObject.Find("InventoryPreviewCamera");
        if (camGo == null)
        {
            Debug.LogWarning("InventoryPreviewUtilities: InventoryPreviewCamera not found in scene.");
            return;
        }

        // Remove URP component by name if present to avoid blocking deletion
        var urpComp = camGo.GetComponent("UniversalAdditionalCameraData");
        if (urpComp != null)
        {
            Undo.DestroyObjectImmediate((Component)urpComp);
            Debug.Log("InventoryPreviewUtilities: removed UniversalAdditionalCameraData from InventoryPreviewCamera.");
        }

        Undo.DestroyObjectImmediate(camGo);
        Debug.Log("InventoryPreviewUtilities: destroyed InventoryPreviewCamera GameObject.");
    }

    [MenuItem("Tools/Inventory Preview/Remove InventoryPreviewScene (safe)")]
    public static void RemovePreviewScene()
    {
        var root = GameObject.Find("InventoryPreviewScene");
        if (root == null)
        {
            Debug.LogWarning("InventoryPreviewUtilities: InventoryPreviewScene not found.");
            return;
        }

        var camT = root.transform.Find("InventoryPreviewCamera");
        if (camT != null)
        {
            var urpComp = camT.GetComponent("UniversalAdditionalCameraData");
            if (urpComp != null)
            {
                Undo.DestroyObjectImmediate((Component)urpComp);
                Debug.Log("InventoryPreviewUtilities: removed UniversalAdditionalCameraData from InventoryPreviewCamera.");
            }
        }

        Undo.DestroyObjectImmediate(root);
        Debug.Log("InventoryPreviewUtilities: destroyed InventoryPreviewScene and its children.");
    }

    [MenuItem("Tools/Inventory Preview/Assign RenderTexture to RawImage")]
    public static void AssignRenderTextureToRawImage()
    {
        string rtPath = "Assets/Resources/PreviewRenderTextures/InventoryPreviewRT.asset";
        var rt = AssetDatabase.LoadAssetAtPath<RenderTexture>(rtPath);
        if (rt == null)
        {
            Debug.LogWarning("InventoryPreviewUtilities: RenderTexture asset not found at " + rtPath);
            return;
        }

        var gui = GameObject.FindObjectOfType<GameUIManager>();
        if (gui != null && gui.package != null)
        {
            var existing = gui.package.transform.Find("InventoryPreviewRawImage");
            if (existing != null)
            {
                var raw = existing.GetComponent<UnityEngine.UI.RawImage>();
                if (raw != null)
                {
                    raw.texture = rt;
                    Debug.Log("InventoryPreviewUtilities: assigned InventoryPreviewRT to InventoryPreviewRawImage.");
                    return;
                }
                else
                {
                    Debug.LogWarning("InventoryPreviewUtilities: InventoryPreviewRawImage found but has no RawImage component.");
                    return;
                }
            }
            else
            {
                Debug.LogWarning("InventoryPreviewUtilities: InventoryPreviewRawImage not found under package UI.");
                return;
            }
        }
        else
        {
            Debug.LogWarning("InventoryPreviewUtilities: GameUIManager or package not found in the scene.");
            return;
        }
    }
}
