using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Box碰撞区域, 根据Box碰撞器参数生成Box碰撞检测区域, 对区域内的敌对层造成伤害
/// </summary>
public class BoxAttackArea : AttackArea
{
    private BoxCollider box;// Box碰撞器，该组件实际没有被启用, 用于方便在Unity中编辑碰撞区域

    private void Awake()
    {
        box = GetComponent<BoxCollider>();
    }

    /// <summary>
    /// Box区域伤害判定
    /// </summary>
    /// <param name="attack">造成的伤害</param>
    /// <returns>是否有命中目标</returns>
    public override bool AreaDamage(int attack, bool isPhysical)
    {
        bool hitTarget = false;

        // Box检测
        RaycastHit[] objs = Physics.BoxCastAll(box.transform.TransformPoint(box.center), box.size / 2, box.transform.forward, box.transform.rotation, 0, OppositeLayerMask(combatCamp));
        foreach (RaycastHit obj in objs)
        {
            // 如果有CharacterAttributes组件, 则造成伤害
            CharacterAttributes character = obj.transform.GetComponent<CharacterAttributes>();
            if (character != null)
            {
                character.GetAttack(attack, isPhysical);
                hitTarget = true;
            }
        }

        return hitTarget;
    }
}
