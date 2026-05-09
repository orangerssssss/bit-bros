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
    private int stamina = -1;// 当前显示体力值
    private int maxStamina = -1;// 当前显示最大体力值
    private Color staminaHighColor = new Color(0.20f, 0.78f, 0.29f);
    private Color staminaMidColor = new Color(0.92f, 0.74f, 0.16f);
    private Color staminaLowColor = new Color(0.86f, 0.23f, 0.19f);

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
        HealthUpdate();
        StaminaUpdate();
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
    /// 当体力值的显示值与实际值不符时, 更新显示
    /// </summary>
    private void StaminaUpdate()
    {
        if (playerAttributes == null || GameUIManager.Instance == null) return;
        if (stamina != playerAttributes.CurrentStaminaInt || maxStamina != playerAttributes.MaxStaminaInt)
        {
            stamina = playerAttributes.CurrentStaminaInt;
            maxStamina = playerAttributes.MaxStaminaInt;
            float staminaRatio = maxStamina > 0 ? (float)stamina / maxStamina : 0;
            if (GameUIManager.Instance.staminaBar != null) GameUIManager.Instance.staminaBar.value = staminaRatio;
            if (GameUIManager.Instance.staminaText != null) GameUIManager.Instance.staminaText.text = $"体力：{stamina} / {maxStamina}";
            GameUIManager.Instance.SetStaminaBarColor(GetStaminaColor(staminaRatio));
        }
    }

    private Color GetStaminaColor(float ratio)
    {
        if (ratio <= 0.25f) return staminaLowColor;
        if (ratio <= 0.55f) return staminaMidColor;
        return staminaHighColor;
    }
}
