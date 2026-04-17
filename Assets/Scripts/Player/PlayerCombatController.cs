using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家战斗控制器组件, 控制玩家技能及武器的显示
/// </summary>
public class PlayerCombatController : MonoBehaviour
{
    [SerializeField]
    private GameObject weaponParent;// 武器父物体, 用于控制武器的显示与隐藏
    [SerializeField]
    private GameObject armorParent;// 防具父物体
    [SerializeField]
    private List<PlayerEquipmentModel> weaponModels;// 武器ID与玩家手中武器的对应关系(这里是预先摆放好了不同武器模型在手中的位置)
    [SerializeField]
    private List<PlayerEquipmentModel> armorModels;// 防具ID与玩家防具的对应关系
    public AudioSource combatAudioSource;

    [HideInInspector]
    public PlayerMoveController playerMoveController;// 玩家移动组件
    [HideInInspector]
    public Animator animator;// 玩家动画组件

    private float hitPauseTimer;// 击打顿帧计时器
    [HideInInspector]
    public int equipedWeaponID;// 装备中的武器
    private GameObject runtimeWeaponInstance; // instantiated at runtime if inspector models are missing

    public bool playerSkillControllable = true;// 是否能够使用技能

    private static bool firstLoad = true;

    public bool WeaponVisible { get { return weaponParent != null && weaponParent.activeSelf; } }


    private void Awake()
    {
        playerMoveController = GetComponent<PlayerMoveController>();
        animator = GetComponent<Animator>();

        // 为动画添加旋转事件, 玩家在释放此动画对应的技能时会立刻改变朝向
        AddEventToClips("PlayerRotationUpdate", 0.1f, new List<string> { "Dodge", "CommonAttack_0", "CommonAttack_1", "CommonAttack_2" });

        // 为动画添加武器显示事件, 玩家在释放此动画对应的技能时会让武器变为可见状态
        AddEventToClips("SetWeaponVisible", 0f, new List<string> { "CommonAttack_0", "CommonAttack_1", "CommonAttack_2" }, intParameter: 1);
        // 如果当前没有装备武器，默认隐藏weaponParent（避免预先显示为饰品）
        if (weaponParent != null && equipedWeaponID <= 0)
        {
            weaponParent.SetActive(false);
        }

        // 确保weaponModels中的模型在启动时根据equipedWeaponID处于正确的激活状态，防止作为摆设始终可见
        if (weaponModels != null)
        {
            bool anyActive = false;
            foreach (PlayerEquipmentModel model in weaponModels)
            {
                if (model == null || model.equipmentModel == null) continue;
                bool shouldActive = (model.equipmentID == equipedWeaponID && equipedWeaponID > 0);
                model.equipmentModel.SetActive(shouldActive);
                if (shouldActive) anyActive = true;
            }
            if (weaponParent != null)
                weaponParent.SetActive(anyActive);
        }
        Debug.Log($"PlayerCombatController Awake: equipedWeaponID={equipedWeaponID}, weaponParent={(weaponParent!=null)}, weaponModelsCount={(weaponModels!=null?weaponModels.Count:0)}");
    }

    private void Update()
    {
        HideWeapon();
        HitPauseUpdate();
    }

    private void OnEnable()
    {
        if (GameEventManager.Instance != null)
            GameEventManager.Instance.pickUpItemEvent.AddListener(OnPickUpItem);
        // Attempt to reapply equipped weapon when component becomes enabled (useful after scene load)
        try { ReapplyEquippedWeaponFromSave(); } catch { }
    }

    private void OnDisable()
    {
        if (GameEventManager.Instance != null)
            GameEventManager.Instance.pickUpItemEvent.RemoveListener(OnPickUpItem);
    }

    // 当拾取物品时自动检查是否是武器，若是则切换并显示在手中
    private void OnPickUpItem(int itemID)
    {
        Debug.Log("PlayerCombatController.OnPickUpItem: " + itemID);
        if (DataManager.Instance == null || DataManager.Instance.itemConfig == null) return;
        Item item = DataManager.Instance.itemConfig.FindItemByID(itemID);
        if (item != null && item.itemType == ItemType.Weapon)
        {
            SwitchWeapon(itemID);
            SetWeaponVisible(true);
        }
    }

    private void OnDestroy()
    {
        firstLoad = false;
    }

    private void Start()
    {
        // Ensure weapon visuals are present on Start as well
        try { ReapplyEquippedWeaponFromSave(); } catch { }
    }

    /// <summary>
    /// Reapply equipped weapon from Inventory/DataManager/save or current equipedWeaponID.
    /// Called on Start/OnEnable to ensure weapon model is present after scene transitions.
    /// </summary>
    private void ReapplyEquippedWeaponFromSave()
    {
        int weaponToApply = -1;
        try
        {
            if (InventoryManager.Instance != null && InventoryManager.Instance.WeaponID > 0)
                weaponToApply = InventoryManager.Instance.WeaponID;
            else if (DataManager.Instance != null && DataManager.Instance.saveData != null && DataManager.Instance.saveData.inventorySaveData != null && DataManager.Instance.saveData.inventorySaveData.weaponID > 0)
                weaponToApply = DataManager.Instance.saveData.inventorySaveData.weaponID;
            else if (equipedWeaponID > 0)
                weaponToApply = equipedWeaponID;
        }
        catch { }

        if (weaponToApply > 0)
        {
            Debug.Log($"Reapplying equipped weapon from save: {weaponToApply}");
            SwitchWeapon(weaponToApply);
            SetWeaponVisible(true);
        }
    }

    /// <summary>
    /// 初始化战斗控制
    /// </summary>
    public void InitCombat()
    {
        animator.ResetTrigger("Dodge");
        animator.ResetTrigger("CommonAttack");
    }

    /// <summary>
    /// 添加事件至动画片段列表
    /// </summary>
    /// <param name="functionName">事件函数名</param>
    /// <param name="relativeTime">在动画片段中所处的相对时间, 例如0.5表示在该动画片段进行到一半时触发该事件</param>
    /// <param name="clipsName">目标动画列表</param>
    /// <param name="intParameter">发送给函数的int参数</param>
    /// <param name="floatParameter">发送给函数的float参数</param>
    /// <param name="stringParameter">发送给函数的string参数</param>
    /// <param name="objectReferenceParameter">发送给函数的object参数</param>
    public void AddEventToClips(string functionName, float relativeTime, List<string> clipsName, int intParameter = 0, float floatParameter = 0f, string stringParameter = "", Object objectReferenceParameter = null)
    {
        // 用于解决一个奇怪的Unity问题（在编辑器下出现，打包后的效果未测试）
        // 添加Event对Clip的修改在场景切换时被保存（在退出运行模式后修改会被复原）
        // 导致来回切换场景时，反复触发的Awake Start函数中的AddEvent被重复触发，使得动画中被添加同样的Event
#if UNITY_EDITOR
        if (!firstLoad) return;
#endif

        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;

        // 在动画组件中查找对应的动画片段
        foreach(AnimationClip clip in clips)
        {
            if (clipsName.Contains(clip.name))
            {
                AnimationEvent evt = new AnimationEvent();

                evt.functionName = functionName;
                evt.time = clip.length * relativeTime;
                
                evt.intParameter = intParameter;
                evt.floatParameter = floatParameter;
                evt.stringParameter = stringParameter;
                evt.objectReferenceParameter = objectReferenceParameter;

                // 为动画片段添加事件
                clip.AddEvent(evt);
            }
        }
    }

    /// <summary>
    /// 击打顿帧, 玩家的动画机会被暂停
    /// </summary>
    /// <param name="seconds">停顿时间(sec)</param>
    public void PlayerHitPause(float seconds)
    {
        if (seconds > hitPauseTimer) hitPauseTimer = seconds;
        if (seconds > 0) animator.speed = 0;
    }

    /// <summary>
    /// 重置技能触发器, 在打断技能时调用
    /// </summary>
    public void ResetSkillTrigger()
    {
        animator.ResetTrigger("Dodge");
        animator.ResetTrigger("CommonAttack");
    }

    /// <summary>
    /// 切换武器模型, 在更换武器时被调用
    /// </summary>
    public void SwitchWeapon(int id)
    {
        // Ensure weaponParent reference exists (useful for persistent objects across scenes)
        if (weaponParent == null)
            FindWeaponParentIfNull();

        bool foundStatic = false;
        if (weaponModels != null)
        {
            foreach (PlayerEquipmentModel model in weaponModels)
            {
                if (model == null) continue;
                if (model.equipmentModel == null) continue;
                bool shouldActive = (model.equipmentID == id);
                model.equipmentModel.SetActive(shouldActive);
                if (shouldActive)
                {
                    foundStatic = true;
                    // Put the active model on IgnoreRaycast layer so ViewController occlusion checks ignore it
                    SetLayerRecursively(model.equipmentModel, 2);
                    // Ensure the static model is parented under weaponParent so it follows player across scenes
                    if (weaponParent != null && model.equipmentModel.transform.parent != weaponParent.transform)
                    {
                        // Preserve world transform (position/rotation/scale) when reparenting to avoid scale changes
                        model.equipmentModel.transform.SetParent(weaponParent.transform, true);
                        Debug.Log($"SwitchWeapon: reparented static model '{model.equipmentModel.name}' to '{weaponParent.name}' (world transform preserved)");
                    }
                    Debug.Log($"SwitchWeapon: activated static model '{model.equipmentModel.name}'");
                    LogGameObjectInfo(model.equipmentModel);
                    EnableRenderersRecursively(model.equipmentModel, true);
                }
            }
        }

        // destroy previous runtime instance if any
        if (runtimeWeaponInstance != null)
        {
            Destroy(runtimeWeaponInstance);
            runtimeWeaponInstance = null;
        }

        // If no static model found, try instantiate prefab from ItemConfig (robust across scenes)
        if (!foundStatic && id > 0)
        {
            try
            {
                if (DataManager.Instance != null && DataManager.Instance.itemConfig != null)
                {
                    Item it = DataManager.Instance.itemConfig.FindItemByID(id);
                    if (it != null && it.itemPrefab != null && weaponParent != null)
                    {
                        // Instantiate under weaponParent and preserve the prefab's local transform
                        runtimeWeaponInstance = Instantiate(it.itemPrefab, weaponParent.transform);
                        // Apply prefab's local transform so scale/offset are correct when attached
                        runtimeWeaponInstance.transform.localPosition = it.itemPrefab.transform.localPosition;
                        runtimeWeaponInstance.transform.localRotation = it.itemPrefab.transform.localRotation;
                        runtimeWeaponInstance.transform.localScale = it.itemPrefab.transform.localScale;
                        // Ensure runtime-instantiated prefab doesn't block camera raycasts
                        SetLayerRecursively(runtimeWeaponInstance, 2);
                        Debug.Log($"SwitchWeapon: instantiated runtime weapon '{runtimeWeaponInstance.name}' under '{weaponParent.name}'");
                        LogGameObjectInfo(runtimeWeaponInstance);
                        EnableRenderersRecursively(runtimeWeaponInstance, true);
                        foundStatic = true;
                    }
                }
            }
            catch { }
        }

        equipedWeaponID = id;
        Debug.Log($"SwitchWeapon: equipped set to {id}, foundStatic={foundStatic}");

        // Ensure weaponParent active state reflects whether we have a weapon model
        if (weaponParent != null)
        {
            weaponParent.SetActive(foundStatic);
            if (foundStatic)
                SetLayerRecursively(weaponParent, 2);
        }
            // Additional diagnostic: list children under weaponParent
            if (weaponParent != null)
            {
                Debug.Log($"WeaponParent '{weaponParent.name}' active={weaponParent.activeSelf}, children={weaponParent.transform.childCount}, worldPos={weaponParent.transform.position}");
                for (int i = 0; i < weaponParent.transform.childCount; i++)
                {
                    var c = weaponParent.transform.GetChild(i).gameObject;
                    Debug.Log($"  child[{i}]='{c.name}', active={c.activeSelf}, layer={c.layer}, scene='{c.scene.name}', root='{c.transform.root.name}'");
                    LogGameObjectInfo(c);
                }
                if (Camera.main != null)
                    Debug.Log($"MainCamera cullingMask for layer 2 (IgnoreRaycast) = {((Camera.main.cullingMask & (1<<2))!=0)} (mask={Camera.main.cullingMask})");
            }
    }

    private void FindWeaponParentIfNull()
    {
        // Try common child names
        var t = transform.Find("WeaponParent") ?? transform.Find("weaponParent") ?? transform.Find("Weapons") ?? transform.Find("WeaponHolder");
        if (t != null)
        {
            weaponParent = t.gameObject;
            return;
        }

        // Try to find any child with 'weapon' in name
        foreach (Transform child in transform)
        {
            if (child.name.ToLower().Contains("weapon"))
            {
                weaponParent = child.gameObject;
                return;
            }
        }

        Debug.LogWarning("PlayerCombatController: weaponParent is null and couldn't be found automatically.");
    }

    private void SetLayerRecursively(GameObject go, int layer)
    {
        if (go == null) return;
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    private void LogGameObjectInfo(GameObject go)
    {
        if (go == null)
        {
            Debug.Log("LogGameObjectInfo: null");
            return;
        }
        string fullPath = go.name;
        Transform p = go.transform.parent;
        while (p != null)
        {
            fullPath = p.name + "/" + fullPath;
            p = p.parent;
        }

        var meshes = go.GetComponentsInChildren<MeshRenderer>(true);
        var skinned = go.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        int totalRenderers = meshes.Length + skinned.Length;
        Debug.Log($"GameObjectInfo: '{go.name}' path='{fullPath}' active={go.activeInHierarchy} scene='{(go.scene.IsValid()?go.scene.name:"<no scene>")}' root='{go.transform.root.name}' worldPos={go.transform.position} localPos={go.transform.localPosition} scale={go.transform.lossyScale} renderers={totalRenderers}");

        for (int i = 0; i < meshes.Length; i++)
        {
            Debug.Log($"  MeshRenderer[{i}] name={meshes[i].gameObject.name} enabled={meshes[i].enabled} bounds={meshes[i].bounds}");
        }
        for (int i = 0; i < skinned.Length; i++)
        {
            Debug.Log($"  SkinnedMeshRenderer[{i}] name={skinned[i].gameObject.name} enabled={skinned[i].enabled} bounds={skinned[i].bounds}");
        }
    }

    private void EnableRenderersRecursively(GameObject go, bool enable)
    {
        if (go == null) return;
        var meshes = go.GetComponentsInChildren<MeshRenderer>(true);
        foreach (var m in meshes) m.enabled = enable;
        var skinned = go.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (var s in skinned) s.enabled = enable;
    }

    /// <summary>
    /// 切换防具模型, 在更换防具时被调用
    /// </summary>
    public void SwitchArmor(int id)
    {
        foreach (PlayerEquipmentModel model in armorModels)
        {
            model.equipmentModel.SetActive(model.equipmentID == id);
        }
    }

    /// <summary>
    /// 在不需要显示武器时隐藏武器模型
    /// </summary>
    private void HideWeapon()
    {
        if (weaponParent.activeSelf == true && animator.GetCurrentAnimatorStateInfo(0).IsName("Move") && !animator.IsInTransition(0) && playerMoveController.velocity != Vector3.zero)
        {
            SetWeaponVisible(0);
        }
    }

    /// <summary>
    /// 计时并解除顿帧效果
    /// </summary>
    private void HitPauseUpdate()
    {
        if (hitPauseTimer > 0)
            hitPauseTimer -= Time.deltaTime;
        else if (animator.speed == 0)
            animator.speed = 1;
    }

    /// <summary>
    /// 立刻更新玩家朝向至移动输入方向
    /// </summary>
    private void PlayerRotationUpdate()
    {
        Vector3 dir = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        if (dir != Vector3.zero)
            playerMoveController.SetRotation(dir);
    }

    /// <summary>
    /// 设置武器是否可见
    /// </summary>
    /// <param name="visible">0为不可见, 其余为可见</param>
    private void SetWeaponVisible(int visible)
    {
        //if (visible != 0)
        //    weaponParent.SetActive(true);
        //else
        //    weaponParent.SetActive(false);
    }

    public void SetWeaponVisible(bool active)
    {
        if (weaponParent == null)
            FindWeaponParentIfNull();

        if (weaponParent != null)
        {
            weaponParent.SetActive(active);
            Debug.Log($"SetWeaponVisible: active={active}");
            // Diagnostic: list children and renderer states
            Debug.Log($"WeaponParent '{weaponParent.name}' active={weaponParent.activeSelf}, children={weaponParent.transform.childCount}, worldPos={weaponParent.transform.position}");
            for (int i = 0; i < weaponParent.transform.childCount; i++)
            {
                var c = weaponParent.transform.GetChild(i).gameObject;
                Debug.Log($"  child[{i}]='{c.name}', active={c.activeSelf}, layer={c.layer}, scene='{c.scene.name}', root='{c.transform.root.name}'");
                LogGameObjectInfo(c);
            }
            if (Camera.main != null)
                Debug.Log($"MainCamera cullingMask for layer 2 (IgnoreRaycast) = {((Camera.main.cullingMask & (1<<2))!=0)} (mask={Camera.main.cullingMask})");
        }
        else
        {
            Debug.LogWarning("SetWeaponVisible called but weaponParent is null");
        }
    }
}

/// <summary>
/// 用于装备ID与玩家手持装备模型对应
/// </summary>
[System.Serializable]
public class PlayerEquipmentModel
{
    public int equipmentID;
    public GameObject equipmentModel;
}
