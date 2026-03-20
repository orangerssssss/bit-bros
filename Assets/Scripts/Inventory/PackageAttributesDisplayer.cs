using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 在背包界面显示玩家当前的属性
/// </summary>
public class PackageAttributesDisplayer : MonoBehaviour
{
    private PlayerAttributes playerAttributes;// 玩家属性组件

    [SerializeField]
    private Text careerText;// 职业
    [SerializeField]
    private Text constitutionText;// 体质
    [SerializeField]
    private Text strengthText;// 力量
    [SerializeField]
    private Text intelligenceText;// 智力
    [SerializeField]
    private Text maxHealthText;// 生命
    [SerializeField]
    private Text physicalAttackText;// 物理攻击
    [SerializeField]
    private Text physicalDefenceText;// 物理防御
    [SerializeField]
    private Text magicAttackText;// 魔法攻击
    [SerializeField]
    private Text magicDefenceText;// 魔法防御
    [SerializeField]
    private Text manaText;// 魔法值

    [SerializeField]
    private Button constitutionAdd;
    [SerializeField]
    private Button constitutionReduce;
    [SerializeField]
    private Button strengthAdd;
    [SerializeField]
    private Button strengthReduce;
    [SerializeField]
    private Button intelligenceAdd;
    [SerializeField]
    private Button intelligenceReduce;

    private int constitution;
    private int strength;
    private int intelligence;

    private void Awake()
    {
        playerAttributes = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerAttributes>();
        // 添加UI面板交互按钮执行事件
        constitutionAdd.onClick.AddListener(() => AllocatePointToConstitution(true));
        constitutionReduce.onClick.AddListener(() => AllocatePointToConstitution(false));
        strengthAdd.onClick.AddListener(() => AllocatePointToStrength(true));
        strengthReduce.onClick.AddListener(() => AllocatePointToStrength(false));
        intelligenceAdd.onClick.AddListener(() => AllocatePointToIntelligence(true));
        intelligenceReduce.onClick.AddListener(() => AllocatePointToIntelligence(false));
    }

    private void Update()
    {
        if (constitution != playerAttributes.Constitution || strength != playerAttributes.Strength || intelligence != playerAttributes.Intelligence)
        {
            UpdateAttributesText();
        }
    }

    private void OnEnable()
    {
        UpdateAttributesText();
        UpdateAllocateButton();
    }

    /// <summary>
    /// 更新UI面板上的玩家属性
    /// </summary>
    private void UpdateAttributesText()
    {
        careerText.text = "职业: " + playerAttributes.career;
        constitutionText.text = "体质: " + playerAttributes.Constitution;
        strengthText.text = "力量: " + playerAttributes.Strength;
        intelligenceText.text = "智力: " + playerAttributes.Intelligence;
        maxHealthText.text = "生命: " + playerAttributes.MaxHealth;
        physicalAttackText.text = "物理攻击: " + playerAttributes.PhysicalAttack;
        physicalDefenceText.text = "物理防御: " + playerAttributes.PhysicalDefence;
        magicAttackText.text = "魔法攻击: " + playerAttributes.MagicAttack;
        magicDefenceText.text = "魔法防御: " + playerAttributes.MagicDefence;
        manaText.text = "魔法值: " + playerAttributes.MaxMana;

        constitution = playerAttributes.Constitution;
        strength = playerAttributes.Strength;
        intelligence = playerAttributes.Intelligence;
    }

    /// <summary>
    /// 更新UI面板上按钮的交互状态
    /// </summary>
    private void UpdateAllocateButton()
    {
        bool addInteractable = playerAttributes.AttributePoints > 0;
        constitutionAdd.interactable = addInteractable;
        strengthAdd.interactable = addInteractable;
        intelligenceAdd.interactable = addInteractable;

        constitutionReduce.interactable = playerAttributes.HasAddedConstitution();
        strengthReduce.interactable = playerAttributes.HasAddedStrength();
        intelligenceReduce.interactable = playerAttributes.HasAddedIntelligence();
    }

    /// <summary>
    /// 分配点数至Constitution属性
    /// </summary>
    /// <param name="add">true为正值</param>
    public void AllocatePointToConstitution(bool add)
    {
        playerAttributes.AllocatePointToConstitution(add);

        UpdateAttributesText();
        UpdateAllocateButton();
    }

    /// <summary>
    /// 分配点数至Strength属性
    /// </summary>
    /// <param name="add">true为正值</param>
    public void AllocatePointToStrength(bool add)
    {
        playerAttributes.AllocatePointToStrength(add);

        UpdateAttributesText();
        UpdateAllocateButton();
    }

    /// <summary>
    /// 分配点数至Intelligence属性
    /// </summary>
    /// <param name="add">true为正值</param>
    public void AllocatePointToIntelligence(bool add)
    {
        playerAttributes.AllocatePointToIntelligence(add);

        UpdateAttributesText();
        UpdateAllocateButton();
    }
}
