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
        // Box检测 (包容 child/parent 的 CharacterAttributes)
        Vector3 center = box.transform.TransformPoint(box.center);
        Vector3 halfExtents = box.size / 2f;
        RaycastHit[] objs = Physics.BoxCastAll(center, halfExtents, box.transform.forward, box.transform.rotation, 0f, OppositeLayerMask(combatCamp));
        Debug.Log($"{name} BoxAttackArea: AreaDamage called, testHits={objs.Length}, mask={OppositeLayerMask(combatCamp)}");
        for (int _i = 0; _i < objs.Length; _i++)
        {
            var h = objs[_i];
            Debug.Log($"{name} BoxAttackArea: Hit[{_i}] -> {h.transform.name} (layer={LayerMask.LayerToName(h.transform.gameObject.layer)})");
        }

        // 首先尝试从命中点获取 CharacterAttributes，支持在 collider 的 parent/child 上寻找组件
        foreach (RaycastHit obj in objs)
        {
            Transform t = obj.transform;
            CharacterAttributes character = t.GetComponent<CharacterAttributes>();
            if (character == null) character = t.GetComponentInParent<CharacterAttributes>();
            if (character == null) character = t.GetComponentInChildren<CharacterAttributes>();
            if (character != null)
            {
                character.GetAttack(attack, isPhysical);
                hitTarget = true;
            }
        }

        // 如果没有命中目标（即使有命中 collider 但找不到 CharacterAttributes），进行覆盖检测作为回退
        if (!hitTarget)
        {
            Debug.Log($"{name} BoxAttackArea: No CharacterAttributes hit - performing fallback OverlapBox to detect potential targets.");
            Collider[] cols = Physics.OverlapBox(center, halfExtents, box.transform.rotation, ~0);
            Debug.Log($"{name} BoxAttackArea: OverlapBox fallback hits={cols.Length}");
            foreach (Collider col in cols)
            {
                Transform t = col.transform;
                CharacterAttributes ch = t.GetComponent<CharacterAttributes>();
                if (ch == null) ch = t.GetComponentInParent<CharacterAttributes>();
                if (ch == null) ch = t.GetComponentInChildren<CharacterAttributes>();
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
