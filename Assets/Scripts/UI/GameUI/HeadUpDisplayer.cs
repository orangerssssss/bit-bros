using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 玩家抬头显示, 包括玩家的等级、生命、经验属性
/// </summary>
public class HeadUpDisplayer : MonoBehaviour
{
    private PlayerAttributes playerAttributes;// 玩家属性组件
    private int level = -1;// 当前显示等级
    private int health = -1;// 当前显示生命值
    private int maxHealth = -1;// 当前显示最大生命值
    private int exp = -1;// 当前显示经验
    private int maxExp = -1;// 当前显示最大经验
    private int mana = -1;// 当前显示魔法值
    private int maxMana = -1;// 当前显示最大魔法值

    private void Awake()
    {
        playerAttributes = GetComponent<PlayerAttributes>();
        if (playerAttributes == null)
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) playerAttributes = p.GetComponent<PlayerAttributes>();
        }
    }

    private void Update()
    {
        LevelUpdate();
        HealthUpdate();
        ExpUpdate();
        ManaUpdate();
    }

    /// <summary>
    /// 当等级的显示值与实际值不符时, 更新显示
    /// </summary>
    private void LevelUpdate()
    {
        if (playerAttributes == null || GameUIManager.Instance == null) return;
        if (level != playerAttributes.level)
        {
            level = playerAttributes.level;
            if (GameUIManager.Instance.level != null) GameUIManager.Instance.level.text = "Lv." + level.ToString();
        }
    }

    /// <summary>
    /// 当生命的显示值与实际值不符时, 更新显示
    /// </summary>
    private void HealthUpdate()
    {
        if (playerAttributes == null || GameUIManager.Instance == null) return;

        if (health != playerAttributes.health || maxHealth != playerAttributes.MaxHealth)
        {
            health = playerAttributes.health;
            maxHealth = playerAttributes.MaxHealth;
            if (GameUIManager.Instance.healthBar != null) GameUIManager.Instance.healthBar.value = (float)health / maxHealth;
            if (GameUIManager.Instance.healthText != null) GameUIManager.Instance.healthText.text = $"生命：{health} / {maxHealth}";
        }

        if (GameUIManager.Instance.healthBarSlow != null)
            GameUIManager.Instance.healthBarSlow.value = Mathf.Lerp(GameUIManager.Instance.healthBarSlow.value, (float)health / maxHealth, 1.5f * Time.deltaTime);
    }

    /// <summary>
    /// 当经验的显示值与实际值不符时, 更新显示
    /// </summary>
    private void ExpUpdate()
    {
        if (playerAttributes == null || GameUIManager.Instance == null) return;
        if (exp != playerAttributes.experience || maxExp != playerAttributes.GetMaxExperience())
        {
            exp = playerAttributes.experience;
            maxExp = playerAttributes.GetMaxExperience();
            if (GameUIManager.Instance.expBar != null) GameUIManager.Instance.expBar.value = (float)exp / maxExp;
            if (GameUIManager.Instance.expText != null) GameUIManager.Instance.expText.text = $"经验：{exp} / {maxExp}";
        }
    }

    /// <summary>
    /// 当魔法值的显示值与实际值不符时, 更新显示
    /// </summary>
    private void ManaUpdate()
    {
        if (playerAttributes == null || GameUIManager.Instance == null) return;
        if (mana != playerAttributes.mana || maxMana != playerAttributes.MaxMana)
        {
            mana = playerAttributes.mana;
            maxMana = playerAttributes.MaxMana;
            if (GameUIManager.Instance.manaBar != null) GameUIManager.Instance.manaBar.value = (float)mana / maxMana;
            if (GameUIManager.Instance.manaText != null) GameUIManager.Instance.manaText.text = $"魔法：{mana} / {maxMana}";
        }
    }
}
