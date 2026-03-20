using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 战斗属性类
/// </summary>
public class FightAttributes : CharacterAttributes
{
    private static PlayerAttributes playerAttributes;// 玩家的属性组件

    public string fightName;// 战斗名称

    private FightAI fightAI;// AI组件
    [HideInInspector]
    public EnemyHeadDisplayer headDisplayer;// 头顶显示组件

    private void OnEnable()
    {
        CombatCharacterManager.Instance.Register(this);
    }

    private void OnDisable()
    {
        if (CombatCharacterManager.Instance != null)
        {
            CombatCharacterManager.Instance.Unregister(this);
        }
    }

    /// <summary>
    /// 死亡
    /// </summary>
    protected override void DeathReact()
    {
        if (fightAI != null) fightAI.DiedReact();


        // 玩家获得经验值
        if (combatCamp == CombatCamp.None || combatCamp == CombatCamp.Enemy)
        {
            if (playerAttributes == null) playerAttributes = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerAttributes>();
            playerAttributes.AddExperience(experience);
        }

        // 生成掉落物
        GetComponent<DropItem>().Drop();
        headDisplayer.HideState();
        CombatCharacterManager.Instance.Unregister(this);
    }


    /// <summary>
    /// 受伤反馈
    /// </summary>
    protected override void DamageReact()
    {
        if (fightAI != null) fightAI.GetDamageReact();
        headDisplayer.ShowState((float)health / MaxHealth);
    }

    /// <summary>
    /// 治疗反馈
    /// </summary>
    protected override void HealReact()
    {
        headDisplayer.ShowState((float)health / MaxHealth);
    }

    /// <summary>
    /// 初始化
    /// </summary>
    protected override void Init()
    {
        fightAI = GetComponent<FightAI>();
        headDisplayer = GetComponent<EnemyHeadDisplayer>();

        InitAttributes();
    }

    /// <summary>
    /// 初始化角色数值
    /// </summary>
    public void InitAttributes()
    {
        health = MaxHealth;
        mana = MaxMana;
    }

    public void ChangeAttributes(int cons, int stre, int inte)
    {
        Constitution += cons;
        Strength += stre;
        Intelligence += inte;
    }

    /// <summary>
    /// 为NPC提供装备属性加成
    /// </summary>
    public void ImproveAttributesByEquipments(EquipmentItem _item)
    {
        Constitution += _item.constitution;
        Strength += _item.strength;
        Intelligence += _item.intelligence;
    }
}
