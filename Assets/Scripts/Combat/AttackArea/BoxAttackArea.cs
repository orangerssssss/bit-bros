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
        Debug.Log($"{name} BoxAttackArea: AreaDamage called, testHits={objs.Length}, mask={OppositeLayerMask(combatCamp)}");
        for (int _i = 0; _i < objs.Length; _i++)
        {
            var h = objs[_i];
            Debug.Log($"{name} BoxAttackArea: Hit[{_i}] -> {h.transform.name} (layer={LayerMask.LayerToName(h.transform.gameObject.layer)})");
        }
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

        // Fallback: if no hits detected (often caused by layer mismatch), try an overlap check ignoring layer mask
        if (!hitTarget && objs.Length == 0)
        {
            Debug.Log($"{name} BoxAttackArea: No hits with layer mask - performing fallback OverlapBox to detect potential targets.");
            Collider[] cols = Physics.OverlapBox(box.transform.TransformPoint(box.center), box.size / 2, box.transform.rotation, ~0);
            Debug.Log($"{name} BoxAttackArea: OverlapBox fallback hits={cols.Length}");
            foreach (Collider col in cols)
            {
                CharacterAttributes ch = col.transform.GetComponent<CharacterAttributes>();
                if (ch != null && ch.combatCamp != this.combatCamp)
                {
                    Debug.Log($"{name} BoxAttackArea: Fallback hit -> {col.transform.name} (camp={ch.combatCamp})");
                    ch.GetAttack(attack, isPhysical);
                    hitTarget = true;
                }
            }
        }

        return hitTarget;
    }
}
