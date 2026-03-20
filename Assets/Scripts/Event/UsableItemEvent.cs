using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// 使用物品事件, 物品使用后调用的方法写在此位置
/// </summary>
public class UsableItemEvent : MonoBehaviour
{
    public static UsableItemEvent instance;// 单例

    private void Awake()
    {
        instance = this;
    }

    /// <summary>
    /// 触发事件
    /// </summary>
    /// <param name="methodName">方法名</param>
    /// <returns>是否使用成功</returns>
    public bool UsableItemInvoke(string methodName)
    {
        if (methodName == "") 
            return false;

        MethodInfo methodInfo = GetType().GetMethod(methodName);
        return (bool)methodInfo.Invoke(this, null);
    }

    /// <summary>
    /// 小治疗效果, 回复40HP
    /// </summary>
    /// <returns>是否使用成功</returns>
    public bool SmallHeal()
    {
        PlayerAttributes playerAttributes = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerAttributes>();
        if (playerAttributes)
        {
            if (playerAttributes.health > 0 && playerAttributes.health < playerAttributes.MaxHealth)
            {
                playerAttributes.Heal(40);
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            throw new System.Exception("未找到玩家.");
        }
    }

    /// <summary>
    /// 中治疗效果, 回复最大生命值的20%
    /// </summary>
    /// <returns>是否使用成功</returns>
    public bool MiddleHeal()
    {
        PlayerAttributes playerAttributes = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerAttributes>();
        if (playerAttributes)
        {
            if (playerAttributes.health > 0 && playerAttributes.health < playerAttributes.MaxHealth)
            {
                playerAttributes.Heal((int)(playerAttributes.MaxHealth * 0.2f));
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            throw new System.Exception("未找到玩家.");
        }
    }
}
