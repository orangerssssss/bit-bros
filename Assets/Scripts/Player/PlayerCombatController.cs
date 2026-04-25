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
    public Animator animator;// 玩家动画组件`

    [Header("Shield Visuals")]
    [SerializeField] private Transform shieldHolder;
    [SerializeField] private GameObject shieldPrefab;
    private GameObject shieldInstance;
    private bool hasIsShieldingParam = false;
    private Transform shieldVisualParent;
    private GameObject equippedShieldModel; // reference to the equipped (static or runtime-instantiated) visual

    [Header("Shield Pose (optional)")]
    [Tooltip("Force script-driven shield pose even if Animator parameter exists (useful for quick testing)")]
    [SerializeField] private bool forceManualShieldPose = false;
    [Tooltip("Enable manual pose blending when Animator parameter is not present or forced")]
    [SerializeField] private bool enableManualShieldPose = true;
    [Tooltip("Local position offset applied for the blocking pose (added to rest local position)")]
    [SerializeField] private Vector3 shieldBlockLocalPosOffset = new Vector3(0f, 0.02f, -0.05f);
    [Tooltip("Local rotation (euler) offset applied for the blocking pose")]
    [SerializeField] private Vector3 shieldBlockLocalRotOffset = new Vector3(-20f, 0f, 0f);
    [Tooltip("Speed of pose blending (higher = snappier)")]
    [SerializeField] private float shieldPoseLerpSpeed = 10f;

    private Vector3 shieldRestLocalPos;
    private Quaternion shieldRestLocalRot;
    private Vector3 shieldBlockLocalPos;
    private Quaternion shieldBlockLocalRot;
    private bool shieldPoseInitialized = false;


[HideInInspector]
public int equipedArmorID; // Shield yang sedang dipakai

[HideInInspector]
public bool isShielding = false; // Status blocking

[Header("Shield Stats")]
[SerializeField] private float maxShieldHealth = 100f;
[SerializeField] private float shieldRechargeRate = 15f;
[SerializeField] private float shieldRechargeDelay = 2f;

private float currentShieldHealth;
private float lastShieldDamageTime;
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
        hasIsShieldingParam = HasAnimatorParameter("isShielding");
        FindShieldHolderIfNull();
        // find or create a parent for shield visuals that is always active
        var t = transform.Find("ShieldVisualParent");
        if (t != null) shieldVisualParent = t;
        else
        {
            var go = new GameObject("ShieldVisualParent");
            shieldVisualParent = go.transform;
            shieldVisualParent.SetParent(this.transform, false);
            shieldVisualParent.localPosition = Vector3.zero;
            shieldVisualParent.localRotation = Quaternion.identity;
            shieldVisualParent.localScale = Vector3.one;
        }

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

        // Initialize shield values
        currentShieldHealth = maxShieldHealth;
        lastShieldDamageTime = -999f;
    }

    private void Update()
    {
        HideWeapon();
        HitPauseUpdate();
        HandleShieldInput();
        HandleShieldRecharge();
    }

    private void LateUpdate()
    {
        // If we have an equipped shield model, ensure it is parented to the bone when possible
        if (equippedShieldModel != null && shieldHolder != null)
        {
            // If the holder is active, prefer to parent the model under the holder so it moves with animation
            if (shieldHolder.gameObject.activeInHierarchy)
            {
                if (equippedShieldModel.transform.parent != shieldHolder)
                {
                    // preserve world scale/pos before reparent
                    Vector3 worldScale = equippedShieldModel.transform.lossyScale;
                    equippedShieldModel.transform.SetParent(shieldHolder, true);
                    equippedShieldModel.transform.localPosition = Vector3.zero;
                    equippedShieldModel.transform.localRotation = Quaternion.identity;
                    // adjust local scale to approximate previous world scale
                    Vector3 parentLossy = shieldHolder.lossyScale;
                    Vector3 newLocalScale = new Vector3(
                        parentLossy.x != 0 ? worldScale.x / parentLossy.x : worldScale.x,
                        parentLossy.y != 0 ? worldScale.y / parentLossy.y : worldScale.y,
                        parentLossy.z != 0 ? worldScale.z / parentLossy.z : worldScale.z
                    );
                    equippedShieldModel.transform.localScale = newLocalScale;
                }
            }
            else
            {
                // If holder isn't active, keep model under ShieldVisualParent and snap it to the holder world transform each frame
                if (equippedShieldModel.transform.parent != shieldVisualParent)
                {
                    Vector3 worldScale = equippedShieldModel.transform.lossyScale;
                    equippedShieldModel.transform.SetParent(shieldVisualParent, true);
                    Vector3 parentLossy = shieldVisualParent.lossyScale;
                    Vector3 newLocalScale = new Vector3(
                        parentLossy.x != 0 ? worldScale.x / parentLossy.x : worldScale.x,
                        parentLossy.y != 0 ? worldScale.y / parentLossy.y : worldScale.y,
                        parentLossy.z != 0 ? worldScale.z / parentLossy.z : worldScale.z
                    );
                    equippedShieldModel.transform.localScale = newLocalScale;
                }

                if (shieldHolder != null)
                {
                    equippedShieldModel.transform.position = shieldHolder.position;
                    equippedShieldModel.transform.rotation = shieldHolder.rotation;
                }
            }
        }
        // Also, if we instantiated a temporary shieldInstance under shieldHolder while shielding, ensure it follows (should already be parented)
        // Apply manual shield pose blending when appropriate (fallback if Animator parameter/animation not driving the shield)
        UpdateShieldPose();
    }

    private void UpdateShieldPose()
    {
        if (equippedShieldModel == null || !shieldPoseInitialized) return;

        // Only apply manual pose when enabled and either forced or Animator parameter is missing
        if (!enableManualShieldPose) return;
        if (!forceManualShieldPose && hasIsShieldingParam) return;

        Vector3 targetLocalPos = isShielding ? shieldBlockLocalPos : shieldRestLocalPos;
        Quaternion targetLocalRot = isShielding ? shieldBlockLocalRot : shieldRestLocalRot;

        // Smoothly blend local transform
        equippedShieldModel.transform.localPosition = Vector3.Lerp(equippedShieldModel.transform.localPosition, targetLocalPos, Time.deltaTime * shieldPoseLerpSpeed);
        equippedShieldModel.transform.localRotation = Quaternion.Slerp(equippedShieldModel.transform.localRotation, targetLocalRot, Time.deltaTime * shieldPoseLerpSpeed);
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
    if (item == null) return;
    
    if (item.itemType == ItemType.Weapon)
    {
        SwitchWeapon(itemID);
        SetWeaponVisible(true);
    }
    else if (item.itemType == ItemType.Armor)
    {
        EquipShield(itemID);
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
/// Switch armor/shield model (mirror SwitchWeapon)
/// </summary>
public void SwitchArmor(int id)
{
    equipedArmorID = id;
    
    if (armorModels != null)
    {
        foreach (PlayerEquipmentModel model in armorModels)
        {
            if (model == null || model.equipmentModel == null) continue;
            bool shouldActive = (model.equipmentID == id);
            model.equipmentModel.SetActive(shouldActive);
            
            if (shouldActive)
            {
                SetLayerRecursively(model.equipmentModel, 2); // IgnoreRaycast
                Debug.Log($"SwitchArmor: activated '{model.equipmentModel.name}'");
            }
        }
    }
    
    if (armorParent != null)
        armorParent.SetActive(id > 0);
    
    currentShieldHealth = maxShieldHealth;
}

/// <summary>
/// Handle shield input (dipanggil dari Update atau script lain)
/// </summary>
public void HandleShieldInput()
{
    if (equipedArmorID <= 0) return; // Ga ada shield
    
    if (Input.GetKey(KeyCode.Mouse1) && currentShieldHealth > 0)
    {
        if (!isShielding)
            ActivateShield();
    }
    else
    {
        if (isShielding)
            DeactivateShield();
    }
}

private void ActivateShield()
{
    isShielding = true;
    if (hasIsShieldingParam) animator.SetBool("isShielding", true);
    Debug.Log($"ActivateShield: hasIsShieldingParam={hasIsShieldingParam}, animatorPresent={(animator!=null)}, equippedShield={(equippedShieldModel!=null?equippedShieldModel.name:"<none>")}");
    // Visuals are handled by EquipShield (shield model remains in hand like a sword).
    // Here we only toggle the animator parameter (and could enable colliders/effects if needed).
}

private void DeactivateShield()
{
    isShielding = false;
    if (hasIsShieldingParam) animator.SetBool("isShielding", false);
    Debug.Log($"DeactivateShield: hasIsShieldingParam={hasIsShieldingParam}, animatorPresent={(animator!=null)}, equippedShield={(equippedShieldModel!=null?equippedShieldModel.name:"<none>")}");
    // Do not hide or destroy the equipped shield model here; it should remain in the character's hand until unequipped.
}

/// <summary>
/// Called when shield takes damage
/// </summary>
public void TakeShieldDamage(float damage)
{
    if (!isShielding) return;
    
    currentShieldHealth -= damage;
    lastShieldDamageTime = Time.time;
    
    if (currentShieldHealth <= 0)
    {
        currentShieldHealth = 0;
        DeactivateShield();
        Debug.Log("Shield Broken!");
    }
}

/// <summary>
/// Shield recharge over time
/// </summary>
public void HandleShieldRecharge()
{
    if (!isShielding && currentShieldHealth < maxShieldHealth && equipedArmorID > 0)
    {
        if (Time.time - lastShieldDamageTime >= shieldRechargeDelay)
        {
            currentShieldHealth += shieldRechargeRate * Time.deltaTime;
            currentShieldHealth = Mathf.Clamp(currentShieldHealth, 0, maxShieldHealth);
        }
    }
}

public float GetShieldPercentage()
{
    if (maxShieldHealth <= 0) return 0;
    return currentShieldHealth / maxShieldHealth;
}

/// <summary>
/// Equip shield dari inventory (dipanggil lewat pickup atau manual)
/// </summary>
public void EquipShield(int shieldID)
{
    // Apply armor/shield visuals and move/instantiate model to shieldHolder if available
    SwitchArmor(shieldID);

    bool foundStaticModel = false;
    if (shieldHolder != null && armorModels != null)
    {
        foreach (PlayerEquipmentModel model in armorModels)
        {
            if (model == null || model.equipmentModel == null) continue;
            if (model.equipmentID == shieldID)
            {
                // Diagnostics: before reparent
                Debug.Log($"EquipShield: preparing to move '{model.equipmentModel.name}' -> shieldHolder. model.activeSelf={model.equipmentModel.activeSelf}, model.activeInHierarchy={model.equipmentModel.activeInHierarchy}, model.localScale={model.equipmentModel.transform.localScale}, model.lossyScale={model.equipmentModel.transform.lossyScale}");
                if (shieldHolder != null)
                    Debug.Log($"shieldHolder activeInHierarchy={shieldHolder.gameObject.activeInHierarchy}, shieldHolder lossyScale={shieldHolder.lossyScale}");

                // preserve the model's current world scale, then compute a local scale appropriate for the new parent
                Vector3 desiredWorldScale = model.equipmentModel.transform.lossyScale;

                // If the actual bone is active, parent there; otherwise parent to a visual parent and snap to bone world transform
                if (shieldHolder != null && shieldHolder.gameObject.activeInHierarchy)
                {
                    model.equipmentModel.transform.SetParent(shieldHolder, false);
                    model.equipmentModel.transform.localPosition = Vector3.zero;
                    model.equipmentModel.transform.localRotation = Quaternion.identity;
                }
                else
                {
                    model.equipmentModel.transform.SetParent(shieldVisualParent, true);
                    // snap to bone world transform so it appears correctly
                    if (shieldHolder != null)
                    {
                        model.equipmentModel.transform.position = shieldHolder.position;
                        model.equipmentModel.transform.rotation = shieldHolder.rotation;
                    }
                }

                // Adjust localScale so the world scale approximates the original
                var parent = model.equipmentModel.transform.parent;
                Vector3 parentLossy = parent != null ? parent.lossyScale : Vector3.one;
                Vector3 newLocalScale = new Vector3(
                    parentLossy.x != 0 ? desiredWorldScale.x / parentLossy.x : desiredWorldScale.x,
                    parentLossy.y != 0 ? desiredWorldScale.y / parentLossy.y : desiredWorldScale.y,
                    parentLossy.z != 0 ? desiredWorldScale.z / parentLossy.z : desiredWorldScale.z
                );
                model.equipmentModel.transform.localScale = newLocalScale;

                model.equipmentModel.SetActive(true);
                EnableRenderersRecursively(model.equipmentModel, true);
                SetLayerRecursively(model.equipmentModel, 2);
                Debug.Log($"EquipShield: moved static shield model '{model.equipmentModel.name}' to shieldHolder");
                LogGameObjectInfo(model.equipmentModel);
                Debug.Log($"EquipShield diagnostics: active={model.equipmentModel.activeInHierarchy}, layer={model.equipmentModel.layer}, localPos={model.equipmentModel.transform.localPosition}, lossyScale={model.equipmentModel.transform.lossyScale}");
                if (Camera.main != null)
                    Debug.Log($"MainCamera cullingMask for layer 2 (IgnoreRaycast) = {((Camera.main.cullingMask & (1<<2))!=0)} (mask={Camera.main.cullingMask})");
                // remember reference so Activate/Deactivate can toggle visibility instead of instantiating duplicates
                equippedShieldModel = model.equipmentModel;
                // initialize manual pose data (account for parent lossy scale so offsets remain sane)
                var poseParent = equippedShieldModel.transform.parent;
                Vector3 parentLossyForPose = poseParent != null ? poseParent.lossyScale : Vector3.one;
                shieldRestLocalPos = equippedShieldModel.transform.localPosition;
                shieldRestLocalRot = equippedShieldModel.transform.localRotation;
                Vector3 scaledOffset = new Vector3(
                    parentLossyForPose.x != 0 ? shieldBlockLocalPosOffset.x / parentLossyForPose.x : shieldBlockLocalPosOffset.x,
                    parentLossyForPose.y != 0 ? shieldBlockLocalPosOffset.y / parentLossyForPose.y : shieldBlockLocalPosOffset.y,
                    parentLossyForPose.z != 0 ? shieldBlockLocalPosOffset.z / parentLossyForPose.z : shieldBlockLocalPosOffset.z
                );
                shieldBlockLocalPos = shieldRestLocalPos + scaledOffset;
                shieldBlockLocalRot = shieldRestLocalRot * Quaternion.Euler(shieldBlockLocalRotOffset);
                shieldPoseInitialized = true;
                foundStaticModel = true;
                break;
            }
        }
    }

    if (!foundStaticModel)
    {
        // try instantiate prefab from ItemConfig similar to weapon handling
        try
        {
            if (DataManager.Instance != null && DataManager.Instance.itemConfig != null)
            {
                Item it = DataManager.Instance.itemConfig.FindItemByID(shieldID);
                    if (it != null && it.itemPrefab != null && shieldHolder != null)
                {
                    GameObject runtimeShield = null;
                    // desired world scale based on prefab
                    Vector3 desiredWorldScale = it.itemPrefab.transform.lossyScale;

                    if (shieldHolder.gameObject.activeInHierarchy)
                    {
                        runtimeShield = Instantiate(it.itemPrefab, shieldHolder);
                        runtimeShield.transform.localPosition = it.itemPrefab.transform.localPosition;
                        runtimeShield.transform.localRotation = it.itemPrefab.transform.localRotation;
                        // adjust local scale to preserve desired world scale
                        var parentLossy = shieldHolder.lossyScale;
                        runtimeShield.transform.localScale = new Vector3(
                            parentLossy.x != 0 ? desiredWorldScale.x / parentLossy.x : desiredWorldScale.x,
                            parentLossy.y != 0 ? desiredWorldScale.y / parentLossy.y : desiredWorldScale.y,
                            parentLossy.z != 0 ? desiredWorldScale.z / parentLossy.z : desiredWorldScale.z
                        );
                    }
                    else
                    {
                        runtimeShield = Instantiate(it.itemPrefab, shieldVisualParent);
                        // snap to bone world transform if holder exists
                        if (shieldHolder != null)
                        {
                            runtimeShield.transform.position = shieldHolder.position;
                            runtimeShield.transform.rotation = shieldHolder.rotation;
                        }
                        var parentLossy = shieldVisualParent.lossyScale;
                        runtimeShield.transform.localScale = new Vector3(
                            parentLossy.x != 0 ? desiredWorldScale.x / parentLossy.x : desiredWorldScale.x,
                            parentLossy.y != 0 ? desiredWorldScale.y / parentLossy.y : desiredWorldScale.y,
                            parentLossy.z != 0 ? desiredWorldScale.z / parentLossy.z : desiredWorldScale.z
                        );
                    }
                    SetLayerRecursively(runtimeShield, 2);
                    EnableRenderersRecursively(runtimeShield, true);
                    Debug.Log($"EquipShield: instantiated runtime shield '{runtimeShield.name}' under {(runtimeShield.transform.parent==shieldHolder?"shieldHolder":"shieldVisualParent")}");
                    LogGameObjectInfo(runtimeShield);
                    if (Camera.main != null)
                        Debug.Log($"MainCamera cullingMask for layer 2 (IgnoreRaycast) = {((Camera.main.cullingMask & (1<<2))!=0)} (mask={Camera.main.cullingMask})");
                    equippedShieldModel = runtimeShield;
                    // initialize manual pose data for runtime instance (account for parent lossy scale)
                    var rposeParent = equippedShieldModel.transform.parent;
                    Vector3 rparentLossyForPose = rposeParent != null ? rposeParent.lossyScale : Vector3.one;
                    shieldRestLocalPos = equippedShieldModel.transform.localPosition;
                    shieldRestLocalRot = equippedShieldModel.transform.localRotation;
                    Vector3 rscaledOffset = new Vector3(
                        rparentLossyForPose.x != 0 ? shieldBlockLocalPosOffset.x / rparentLossyForPose.x : shieldBlockLocalPosOffset.x,
                        rparentLossyForPose.y != 0 ? shieldBlockLocalPosOffset.y / rparentLossyForPose.y : shieldBlockLocalPosOffset.y,
                        rparentLossyForPose.z != 0 ? shieldBlockLocalPosOffset.z / rparentLossyForPose.z : shieldBlockLocalPosOffset.z
                    );
                    shieldBlockLocalPos = shieldRestLocalPos + rscaledOffset;
                    shieldBlockLocalRot = shieldRestLocalRot * Quaternion.Euler(shieldBlockLocalRotOffset);
                    shieldPoseInitialized = true;
                }
            }
        }
        catch { }
    }
}

/// <summary>
/// Cek apakah shield bisa dipakai
/// </summary>
public bool IsShieldActive()
{
    return equipedArmorID > 0 && currentShieldHealth > 0;
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

    private void FindShieldHolderIfNull()
    {
        if (shieldHolder != null) return;
        // 1) If Animator is present and humanoid, prefer the built-in bone transforms
        if (animator != null)
        {
            try
            {
                if (animator.isHuman)
                {
                    HumanBodyBones[] candidates = new HumanBodyBones[] {
                        HumanBodyBones.LeftHand,
                        HumanBodyBones.LeftLowerArm,
                        HumanBodyBones.LeftUpperArm,
                        HumanBodyBones.LeftShoulder
                    };
                    foreach (var hb in candidates)
                    {
                        var b = animator.GetBoneTransform(hb);
                        if (b != null)
                        {
                            shieldHolder = b;
                            Debug.Log($"PlayerCombatController: found shieldHolder via Animator.GetBoneTransform -> {hb} ({b.name})");
                            return;
                        }
                    }
                }

                // 2) If not humanoid or not found, inspect SkinnedMeshRenderer bones for suitable names
                var skinned = GetComponentsInChildren<SkinnedMeshRenderer>(true);
                foreach (var smr in skinned)
                {
                    if (smr == null || smr.bones == null) continue;
                    foreach (var b in smr.bones)
                    {
                        if (b == null) continue;
                        string bn = b.name.ToLower();
                        if ((bn.Contains("hand") && bn.Contains("left")) || bn.EndsWith(".l") || bn.EndsWith("_l") || bn.Contains("hand_l") || bn.Contains("l_hand") || bn.Contains("clavicle_l") || bn.Contains("shoulder_l"))
                        {
                            shieldHolder = b;
                            Debug.Log($"PlayerCombatController: found shieldHolder via SkinnedMeshRenderer bones -> {b.name}");
                            return;
                        }
                    }
                }
            }
            catch { }
        }

        // 3) Try common child names as a fallback
        string[] names = { "ShieldHolder", "shieldHolder", "Shield_Holder", "LeftHand", "leftHand", "hand_L", "Hand.L", "Hand_L", "LeftHandTarget" };
        foreach (var n in names)
        {
            var t = transform.Find(n);
            if (t != null)
            {
                shieldHolder = t;
                Debug.Log($"PlayerCombatController: found shieldHolder by name '{n}'");
                return;
            }
        }

        // 4) Fallback: search children for likely left-hand bone names by simple name matching
        foreach (Transform t in transform.GetComponentsInChildren<Transform>(true))
        {
            string lower = t.name.ToLower();
            if ((lower.Contains("hand") && lower.Contains("left")) || lower.EndsWith(".l") || lower.EndsWith("_l") || lower.Contains("hand_l") || lower.Contains("l_hand") || lower.Contains("clavicle_l"))
            {
                shieldHolder = t;
                Debug.Log($"PlayerCombatController: auto-found shieldHolder '{t.name}' (name match)");
                return;
            }
        }

        Debug.LogWarning("PlayerCombatController: shieldHolder is null and couldn't be found automatically. Assign in Inspector.");
    }

    private bool HasAnimatorParameter(string paramName)
    {
        if (animator == null) return false;
        foreach (var p in animator.parameters)
            if (p.name == paramName) return true;
        return false;
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

    // (SwitchArmor is implemented earlier with full logic; duplicate removed.)

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

