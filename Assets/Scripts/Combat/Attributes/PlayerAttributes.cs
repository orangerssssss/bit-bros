using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 玩家属性类
/// </summary>
public class PlayerAttributes : CharacterAttributes
{
    private const int TestMinimumMaxHealth = 400;

    private Animator animator;// 动画组件
    private bool animatorLoggedMissing = false;
    private bool animatorControllerLoggedMissing = false;

    public int pointsPerLevel = 2;
    public float testDamageMultiplier = 2.5f;
    public float moveSpeedMultiplier = 10.0f;// 移动速度
    public float commonAttackSpeed = 2.45f;// 攻击速度
    public float sprintStaminaCostPerSecond = 7.2f;
    public float dodgeStaminaCost = 22.4f;
    public float commonAttackStaminaCost = 9.6f;
    public float staminaRecoverPerSecond = 30.0f;
    public float staminaRecoverDelay = 0.75f;
    public float shieldBlockStaminaRestore = 6.0f;

    public ParticleSystem fx_levelUp;
    public Transform respawnPoint;

    [HideInInspector]
    public int[] attributesAddedPoints = new int[10] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};
    [SerializeField]
    private List<int> levelUpExperience = new List<int>() { 15, 20, 30, 45, 99, 999 };

    private float protectTimer;
    private float stamina;
    private float maxStamina;
    private float staminaRecoverCooldown;
    private float staminaTipCooldown;

    public int AttributePoints { get; private set; }
    public int CurrentStaminaInt => Mathf.RoundToInt(stamina);
    public int MaxStaminaInt => Mathf.RoundToInt(maxStamina);

    private void Update()
    {
        // 同步速度到动画机, 保证动画播放速度一致
        if (animator != null)
        {
            if (animator.runtimeAnimatorController != null)
            {
                animator.SetFloat("MoveSpeedMultiplier", moveSpeedMultiplier);
                animator.SetFloat("CommonAttackSpeed", commonAttackSpeed);
            }
            else
            {
                if (!animatorControllerLoggedMissing)
                {
                    Debug.LogWarning($"{gameObject.name} PlayerAttributes: Animator has no controller assigned. Assign an AnimatorController to enable animation parameters.");
                    animatorControllerLoggedMissing = true;
                }
            }
        }
        else
        {
            if (!animatorLoggedMissing)
            {
                Debug.LogWarning($"{gameObject.name} PlayerAttributes: Animator component missing. Add an Animator to enable player animations.");
                animatorLoggedMissing = true;
            }
        }

        if (protectTimer > 0)
        {
            protectTimer -= Time.deltaTime;
            if (protectTimer <= 0) protect = false;
        }

        if (staminaTipCooldown > 0)
        {
            staminaTipCooldown -= Time.deltaTime;
        }

        UpdateStamina(Time.deltaTime);

        if (Input.GetKeyDown(KeyCode.O))
        {
            GetAttack(100, true);
        }
    }
    
    /// <summary>
    /// 为玩家增加经验, 并在超过最大经验值时升级
    /// </summary>
    /// <param name="exp">增加的经验值</param>
    public void AddExperience(int exp)
    {
        experience += exp;

        // 升级判定
        while (experience >= GetMaxExperience())
        {
            experience -= GetMaxExperience();
            LevelUp();
        }
    }

    /// <summary>
    /// 获取当前等级升级所需总经验值
    /// </summary>
    /// <returns>当前等级升级所需总经验值</returns>
    public int GetMaxExperience()
    {
        if (levelUpExperience != null && levelUpExperience.Count > level)
            return levelUpExperience[level];
        else
            return 999999999;
    }

    /// <summary>
    /// 升级, 并回复血量至最大值
    /// </summary>
    private void LevelUp()
    {
        level++;
        AttributePoints += pointsPerLevel;
        fx_levelUp.Play();
        
        health = MaxHealth;
        mana = MaxMana;
        stamina = maxStamina;

        GameUIManager.Instance.messageTip.ShowTip("你升级了");
    }

    /// <summary>
    /// 死亡
    /// </summary>
    protected override void DeathReact()
    {
        // 关闭所有界面
        var gui = GameUIManager.Instance;
        if (gui != null) gui.CloseAllWindow();

        // 触发死亡角色动画
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetTrigger("Died");
        }

        // 关闭输入
        var pim = PlayerInputManager.Instance;
        if (pim != null) pim.CloseAllInput(true);

        // 显示死亡UI
        if (gui != null && gui.diedUI != null)
        {
            gui.diedUI.Died();
        }

        //if (diedEvent == "")
        //{
            
        //}
        //else
        //{
        //    health = 1;
        //    SpecialEvent.instance.SpecialInvoke(diedEvent);
        //    diedEvent = "";
        //    protect = true;
        //    protectTimer = 1.0f;
        //}
    }

    public void AvoidDeath()
    {
        health = 1;
        protect = true;
        protectTimer = 1.0f;
    }

    /// <summary>
    /// 受伤反馈
    /// </summary>
    protected override void DamageReact()
    {
        var gui = GameUIManager.Instance;
        if (gui != null && gui.damagedUI != null)
        {
            if ((float)health / MaxHealth > 0.35f)
            {
                gui.damagedUI.Damaged(true);
            }
            else
            {
                gui.damagedUI.Damaged(false);
            }
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} PlayerAttributes: GameUIManager or damagedUI missing, skipping DamageReact UI update.");
        }
        // Trigger player hit reaction animation if available and parameter exists
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            if (AnimatorHasParameter("Impact"))
            {
                animator.SetTrigger("Impact");
            }
            else
            {
                Debug.LogWarning($"{gameObject.name} PlayerAttributes: Animator missing 'Impact' parameter - skipping trigger.");
            }
        }
    }

    private bool AnimatorHasParameter(string paramName)
    {
        if (animator == null) return false;
        var ps = animator.parameters;
        for (int i = 0; i < ps.Length; i++)
        {
            if (ps[i].name == paramName) return true;
        }
        return false;
    }

    /// <summary>
    /// 治疗反馈
    /// </summary>
    protected override void HealReact()
    {
        
    }

    /// <summary>
    /// 初始化
    /// </summary>
    protected override void Init()
    {
        animator = GetComponent<Animator>();

        if (DataManager.Instance.loadSave)
        {
            attributesAddedPoints[0] = DataManager.Instance.saveData.playerSaveData.addedConstitution;
            Constitution += attributesAddedPoints[0];
            attributesAddedPoints[1] = DataManager.Instance.saveData.playerSaveData.addedStrength;
            Strength += attributesAddedPoints[1];
            attributesAddedPoints[2] = DataManager.Instance.saveData.playerSaveData.addedIntelligence;
            Intelligence += attributesAddedPoints[2];
            AttributePoints = DataManager.Instance.saveData.playerSaveData.attributePoints;

            level = DataManager.Instance.saveData.playerSaveData.level;
            experience = DataManager.Instance.saveData.playerSaveData.experience;

            //RecalculateAttributes();
        }

        EnsureMinimumTestHealth();
        InitAttributes();
        
        // 注册场景加载事件，进入新场景时恢复血量满
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void EnsureMinimumTestHealth()
    {
        if (MaxHealth >= TestMinimumMaxHealth)
        {
            return;
        }

        int missingHealth = TestMinimumMaxHealth - MaxHealth;
        int needConstitution = Mathf.CeilToInt(missingHealth / 16.0f);
        if (needConstitution > 0)
        {
            Constitution += needConstitution;
        }
    }

    /// <summary>
    /// 场景加载时恢复血量为满
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RecalculateAttributes();
        health = MaxHealth;
        mana = MaxMana;
        stamina = maxStamina;
        staminaRecoverCooldown = 0;
        Debug.Log($"PlayerAttributes: Scene loaded, restored health to {health}/{MaxHealth}");
    }

    /// <summary>
    /// 初始化角色数值
    /// </summary>
    public void InitAttributes()
    {
        health = MaxHealth;
        mana = MaxMana;
        stamina = maxStamina;
        staminaRecoverCooldown = 0;
        var ccm = CombatCharacterManager.Instance;
        if (ccm != null)
            ccm.Register(this);
        else
            Debug.LogWarning($"{gameObject.name} PlayerAttributes: CombatCharacterManager not found during InitAttributes, registration skipped.");
    }

    private void OnEnable()
    {
        // Ensure the player is registered with CombatCharacterManager when enabled (handles DontDestroyOnLoad scene switches)
        var ccm = CombatCharacterManager.Instance;
        if (ccm != null)
            ccm.Register(this);
        else
            Debug.LogWarning($"{gameObject.name} PlayerAttributes: CombatCharacterManager not found in OnEnable, registration skipped.");
    }

    private void OnDisable()
    {
        if (CombatCharacterManager.Instance != null)
            CombatCharacterManager.Instance.Unregister(this);
    }

    /// <summary>
    /// 玩家重生
    /// </summary>
    public void PlayerRespawn()
    {
        // 回到重生点
        if (respawnPoint)
        {
            PlayerInputManager.Instance.moveController.SetPositionAndRotation(respawnPoint);
        }
        else
        {
            throw new System.Exception("重生点配置错误");
        }

        // 初始化所有数据
        InitAttributes();
        GetComponent<PlayerMoveController>().InitMove(respawnPoint.rotation);
        GetComponent<ViewController>().InitCamera();
        GetComponent<PlayerCombatController>().InitCombat();
        // 重置角色动画
        animator.SetTrigger("Reset");
        // 打开输入
        PlayerInputManager.Instance.OpenAllInput();
    }

    public void RecalculateStamina()
    {
        maxStamina = (60.0f + Constitution * 4.0f) * 1.5f;
        if (maxStamina <= 0) maxStamina = 1.0f;
        stamina = Mathf.Clamp(stamina, 0, maxStamina);
    }

    public bool TryConsumeStamina(float amount)
    {
        if (amount <= 0) return true;
        if (stamina <= 0)
        {
            ShowLackOfStaminaTip();
            return false;
        }

        stamina = Mathf.Max(0, stamina - amount);
        staminaRecoverCooldown = staminaRecoverDelay;
        return true;
    }

    public bool HasEnoughStamina(float amount)
    {
        return stamina >= amount;
    }

    public void RestoreStamina(float amount)
    {
        if (amount <= 0) return;
        stamina = Mathf.Min(maxStamina, stamina + amount);
    }

    public void SetInvulnerable(float seconds)
    {
        if (seconds <= 0) return;

        protect = true;
        protectTimer = Mathf.Max(protectTimer, seconds);
    }

    private void UpdateStamina(float deltaTime)
    {
        if (health <= 0) return;

        if (staminaRecoverCooldown > 0)
        {
            staminaRecoverCooldown -= deltaTime;
            return;
        }

        if (stamina < maxStamina)
        {
            stamina = Mathf.Min(maxStamina, stamina + staminaRecoverPerSecond * deltaTime);
        }
    }

    private void ShowLackOfStaminaTip()
    {
        if (staminaTipCooldown > 0) return;

        staminaTipCooldown = 0.5f;
        if (GameUIManager.Instance != null && GameUIManager.Instance.messageTip != null)
        {
            GameUIManager.Instance.messageTip.ShowTip("体力不足");
        }
    }

    public void AllocatePointToConstitution(bool add)
    {
        if (add)
        {
            if (AttributePoints > 0)
            {
                Constitution++;
                attributesAddedPoints[0]++;
                AttributePoints--;
            }
        }
        else
        {
            if (attributesAddedPoints[0] > 0)
            {
                Constitution--;
                attributesAddedPoints[0]--;
                AttributePoints++;
            }
        }

    }

    public void AllocatePointToStrength(bool add)
    {
        if (add)
        {
            if (AttributePoints > 0)
            {
                Strength++;
                attributesAddedPoints[1]++;
                AttributePoints--;
            }
        }
        else
        {
            if (attributesAddedPoints[1] > 0)
            {
                Strength--;
                attributesAddedPoints[1]--;
                AttributePoints++;
            }
        }
    }

    public void AllocatePointToIntelligence(bool add)
    {
        if (add)
        {
            if (AttributePoints > 0)
            {
                Intelligence++;
                attributesAddedPoints[2]++;
                AttributePoints--;
            }
        }
        else
        {
            if (attributesAddedPoints[2] > 0)
            {
                Intelligence--;
                attributesAddedPoints[2]--;
                AttributePoints++;
            }
        }
    }

    public bool HasAddedConstitution()
    {
        return attributesAddedPoints[0] > 0;
    }

    public bool HasAddedStrength()
    {
        return attributesAddedPoints[1] > 0;
    }

    public bool HasAddedIntelligence()
    {
        return attributesAddedPoints[2] > 0;
    }
}
