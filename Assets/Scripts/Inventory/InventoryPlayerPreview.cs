using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Menampilkan preview model pemain pada panel inventory dan menyinkronkan senjata yang sedang dipakai.
/// Pasang skrip ini pada GameObject placeholder di dalam panel `package` UI.
/// Jika `previewPrefab` tidak diisi, skrip akan mencoba meng-clone GameObject ber-tag "Player".
/// </summary>
public class InventoryPlayerPreview : MonoBehaviour
{
    [Header("Preview Setup")]
    [Tooltip("Parent transform di UI yang menjadi tempat spawn model preview (biasanya kosong di dalam panel package)")]
    public Transform previewParent;
    [Tooltip("Optional: prefab model pemain khusus untuk preview. Jika kosong, akan diconnect dari Player di scene saat runtime.")]
    public GameObject previewPrefab;
    [Tooltip("Skala lokal preview (dapat disesuaikan di inspector)")]
    public Vector3 previewLocalScale = Vector3.one * 1f;
    [Tooltip("Rotasi lokal preview (deg)")]
    public Vector3 previewLocalEuler = new Vector3(0, 180f, 0);
    [Tooltip("Posisi lokal preview")]
    public Vector3 previewLocalPos = Vector3.zero;

    [Header("Behavior")]
    [Tooltip("Jika true dan tidak ada prefab, skrip akan meng-clone GameObject Player yang ada di scene")]
    public bool clonePlayerIfNoPrefab = true;
    [Tooltip("Layer yang digunakan agar UI Camera merender model preview (default: UI layer 5)")]
    public int previewRenderLayer = 5;

    private GameObject previewInstance;
    private int lastWeaponID = -9999;

    private void Awake()
    {
        if (previewParent == null)
            previewParent = this.transform;
    }

    private void OnEnable()
    {
        EnsurePreviewExists();
        UpdateWeaponFromPlayer();
    }

    private void OnDisable()
    {
        if (previewInstance != null)
            previewInstance.SetActive(false);
    }

    private void Update()
    {
        // Poll for weapon changes (InventoryManager or actual player)
        int currentWeapon = GetCurrentPlayerWeaponID();
        if (currentWeapon != lastWeaponID)
        {
            UpdateWeapon(currentWeapon);
            lastWeaponID = currentWeapon;
        }
    }

    private void EnsurePreviewExists()
    {
        if (previewInstance != null) 
        {
            previewInstance.SetActive(true);
            return;
        }

        if (previewPrefab != null)
        {
            previewInstance = Instantiate(previewPrefab, previewParent);
            previewInstance.name = previewPrefab.name + "_Preview";
            ApplyPreviewTransform(previewInstance);
            SetLayerRecursively(previewInstance, previewRenderLayer);
            return;
        }

        if (clonePlayerIfNoPrefab)
        {
            GameObject source = FindBestPlayerVisual();
            if (source != null)
            {
                Debug.Log("InventoryPlayerPreview: cloning preview from source '" + GetGameObjectPath(source) + "'");
                // Instantiate a lightweight clone of player's visual root
                previewInstance = Instantiate(source, previewParent);
                previewInstance.name = source.name + "_Preview";

                // Remove runtime-only components that aren't needed in UI preview
                RemoveGameplayComponents(previewInstance);

                ApplyPreviewTransform(previewInstance);
                SetLayerRecursively(previewInstance, previewRenderLayer);
                return;
            }
        }

        Debug.LogWarning("InventoryPlayerPreview: no previewPrefab set and Player not found to clone.");
    }

    private void ApplyPreviewTransform(GameObject go)
    {
        go.transform.localPosition = previewLocalPos;
        go.transform.localEulerAngles = previewLocalEuler;
        go.transform.localScale = previewLocalScale;
    }

    private void RemoveGameplayComponents(GameObject root)
    {
        // Make preview strictly visual: remove gameplay & physics components so it never acts like the real player.
        // Also ensure preview is not tagged as Player so GameObject.FindGameObjectWithTag("Player") points to the real player.
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            try { t.gameObject.tag = "Untagged"; } catch { }
        }

        var allComps = root.GetComponentsInChildren<Component>(true);
        foreach (var comp in allComps)
        {
            if (comp == null) continue;
            // keep these component types for visual preview
            if (comp is Transform) continue;
            if (comp is Animator) continue;
            if (comp is MeshRenderer) continue;
            if (comp is SkinnedMeshRenderer) continue;
            if (comp is Renderer) continue;
            if (comp is ParticleSystem) continue;
            if (comp is CanvasRenderer) continue;

            // destroy everything else (colliders, controllers, custom gameplay scripts, rigidbodies, audio, etc.)
            Destroy(comp);
        }

        // Also explicitly remove PlayerAttributes if present on preview to avoid duplicate state
        var previewAttr = root.GetComponentInChildren<PlayerAttributes>(true);
        if (previewAttr != null) Destroy(previewAttr);
    }

    private int GetCurrentPlayerWeaponID()
    {
        // Prefer InventoryManager (what UI shows), fallback to live PlayerCombatController
        try
        {
            if (InventoryManager.Instance != null && InventoryManager.Instance.WeaponID > 0)
                return InventoryManager.Instance.WeaponID;
        }
        catch { }

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var pc = player.GetComponent<PlayerCombatController>();
            if (pc != null) return pc.equipedWeaponID;
        }
        return -1;
    }

    private void UpdateWeaponFromPlayer()
    {
        int id = GetCurrentPlayerWeaponID();
        lastWeaponID = id;
        UpdateWeapon(id);
    }

    private void UpdateWeapon(int weaponID)
    {
        if (previewInstance == null) return;

        // First try to find PlayerCombatController on preview and call its SwitchWeapon (reuse existing logic)
        var previewCombat = previewInstance.GetComponentInChildren<PlayerCombatController>();
        if (previewCombat != null)
        {
            try
            {
                previewCombat.SwitchWeapon(weaponID);
                previewCombat.SetWeaponVisible(weaponID > 0);
                return;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("InventoryPlayerPreview: failed to use PlayerCombatController on preview: " + e.Message);
            }
        }

        // Otherwise try to find a WeaponParent and instantiate prefab under it
        Transform weaponParent = FindWeaponParent(previewInstance.transform);
        if (weaponParent == null)
        {
            // try to find any child with name containing 'weapon'
            foreach (Transform t in previewInstance.GetComponentsInChildren<Transform>(true))
            {
                if (t.name.ToLower().Contains("weapon") || t.name.ToLower().Contains("weap")) { weaponParent = t; break; }
            }
        }

        // Clear existing preview weapons
        if (weaponParent != null)
        {
            for (int i = weaponParent.childCount - 1; i >= 0; i--)
            {
                Destroy(weaponParent.GetChild(i).gameObject);
            }

            if (weaponID > 0 && DataManager.Instance != null && DataManager.Instance.itemConfig != null)
            {
                var item = DataManager.Instance.itemConfig.FindItemByID(weaponID);
                if (item != null && item.itemPrefab != null)
                {
                    GameObject inst = Instantiate(item.itemPrefab, weaponParent);
                    inst.transform.localPosition = item.itemPrefab.transform.localPosition;
                    inst.transform.localRotation = item.itemPrefab.transform.localRotation;
                    inst.transform.localScale = item.itemPrefab.transform.localScale;
                    SetLayerRecursively(inst, previewRenderLayer);
                }
            }
        }
    }

    private Transform FindWeaponParent(Transform root)
    {
        // Common names used in project
        string[] names = new[] { "WeaponParent", "weaponParent", "Weapons", "WeaponHolder", "weapons" };
        foreach (var n in names)
        {
            var t = root.Find(n);
            if (t != null) return t;
        }
        // fallback: search descendants
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name.ToLower().Contains("weapon")) return t;
        }
        return null;
    }

    private void SetLayerRecursively(GameObject go, int layer)
    {
        if (go == null) return;
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    private GameObject FindBestPlayerVisual()
    {
        // Prefer PlayerInputManager's object (PlayerControl) and its child named "Player" or component roots.
        try
        {
            if (PlayerInputManager.Instance != null)
            {
                var root = PlayerInputManager.Instance.gameObject;
                // child named "Player"
                var child = root.transform.Find("Player");
                if (child != null && !IsUnderUI(child.gameObject)) return child.gameObject;

                // any child with PlayerCombatController
                var pc = root.GetComponentInChildren<PlayerCombatController>();
                if (pc != null && !IsUnderUI(pc.gameObject)) return pc.gameObject;
            }
        }
        catch { }

        // Try common scene objects
        try
        {
            var byName = GameObject.Find("Player");
            if (byName != null && !IsUnderUI(byName)) return byName;
        }
        catch { }

        try
        {
            var byTag = GameObject.FindGameObjectWithTag("Player");
            if (byTag != null && !IsUnderUI(byTag)) return byTag;
        }
        catch { }

        // fallback: any PlayerCombatController in scene not under UI
        try
        {
            var combats = GameObject.FindObjectsOfType<PlayerCombatController>();
            foreach (var c in combats)
            {
                if (!IsUnderUI(c.gameObject)) return c.gameObject;
            }
        }
        catch { }

        return null;
    }

    private bool IsUnderUI(GameObject go)
    {
        if (go == null) return true;
        // inside package UI parent
        try
        {
            if (GameUIManager.Instance != null && GameUIManager.Instance.package != null)
            {
                if (go.transform.IsChildOf(GameUIManager.Instance.package.transform)) return true;
            }
        }
        catch { }
        // any Canvas parent
        if (go.GetComponentInParent<Canvas>() != null) return true;
        return false;
    }

    private string GetGameObjectPath(GameObject go)
    {
        if (go == null) return "<null>";
        var names = new System.Collections.Generic.List<string>();
        Transform t = go.transform;
        while (t != null)
        {
            names.Insert(0, t.name);
            t = t.parent;
        }
        return string.Join("/", names.ToArray());
    }

    /// <summary>
    /// Public helper to ensure preview exists and refresh weapon display.
    /// Can be called by UI manager when opening the package panel.
    /// </summary>
    public void CreateOrRefreshPreview()
    {
        EnsurePreviewExists();
        UpdateWeaponFromPlayer();
        if (previewInstance != null) previewInstance.SetActive(true);
    }
}
