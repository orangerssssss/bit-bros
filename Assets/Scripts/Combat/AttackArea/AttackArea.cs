using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 攻击区域抽象类, 攻击区域用于生成碰撞检测并判断是否命中, 造成伤害
/// </summary>
public abstract class AttackArea : MonoBehaviour
{
    public CombatCamp combatCamp;// 此攻击区域所属的攻击来源

    /// <summary>
    /// 区域伤害判定
    /// </summary>
    /// <param name="attack">造成的伤害</param>
    /// <returns>是否有命中目标</returns>
    public abstract bool AreaDamage(int attack, bool isPhysical);

    /// <summary>
    /// 输入攻击来源, 返回此攻击能够攻击到的层
    /// </summary>
    /// <param name="attackSource">此攻击的来源</param>
    /// <returns>对立的层</returns>
    protected int OppositeLayerMask(CombatCamp camp)
    {
        if (camp == CombatCamp.Player)
            return 1 << 8;// 能够击中Enemy
        else if (camp == CombatCamp.Enemy)
            return 1 << 7;// 能够击中Player
        else
            return 1 << 7 | 1 << 8;// 能够击中Player及Enemy
    }
}
