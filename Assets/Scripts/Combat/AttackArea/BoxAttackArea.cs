using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Box碰撞区域, 根据Box碰撞器参数生成Box碰撞检测区域, 对区域内的敌对层造成伤害
/// </summary>
public class BoxAttackArea : AttackArea
{
    private BoxCollider box;// Box碰撞器，该组件实际没有被启用, 用于方便在Unity中编辑碰撞区域
    [SerializeField] private float shieldBlockStaggerTime = 0.65f;

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
        FightAI attackerAI = GetComponentInParent<FightAI>();
        HashSet<CharacterAttributes> processedTargets = new HashSet<CharacterAttributes>();
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
            if (character != null && character.combatCamp != this.combatCamp && processedTargets.Add(character))
            {
                bool blockedByShield = false;
                if (character is PlayerAttributes)
                {
                    var pb = character.GetComponent<PlayerBlock>();
                    if (pb == null) pb = character.GetComponentInParent<PlayerBlock>();
                    if (pb == null) pb = character.GetComponentInChildren<PlayerBlock>();
                    if (pb != null)
                    {
                        // 仅在“弹反窗口”内才给予敌人僵直奖励
                        blockedByShield = pb.IsInParryWindow();
                    }
                }

                character.GetAttack(attack, isPhysical);

                // 玩家成功举盾挡住这一击后，让攻击者进入短僵直
                if (blockedByShield && attackerAI != null)
                {
                    attackerAI.OnShieldBlocked(shieldBlockStaggerTime);
                }
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
                if (ch != null && ch.combatCamp != this.combatCamp && processedTargets.Add(ch))
                {
                    Debug.Log($"{name} BoxAttackArea: Fallback hit -> {col.transform.name} (camp={ch.combatCamp})");

                    bool blockedByShield = false;
                    if (ch is PlayerAttributes)
                    {
                        var pb = ch.GetComponent<PlayerBlock>();
                        if (pb == null) pb = ch.GetComponentInParent<PlayerBlock>();
                        if (pb == null) pb = ch.GetComponentInChildren<PlayerBlock>();
                        if (pb != null)
                        {
                            // 仅在“弹反窗口”内才给予敌人僵直奖励
                            blockedByShield = pb.IsInParryWindow();
                        }
                    }

                    ch.GetAttack(attack, isPhysical);

                    if (blockedByShield && attackerAI != null)
                    {
                        attackerAI.OnShieldBlocked(shieldBlockStaggerTime);
                    }
                    hitTarget = true;
                }
            }
        }

        return hitTarget;
    }
}
