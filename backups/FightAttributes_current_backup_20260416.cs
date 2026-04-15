using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
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
        Debug.Log("=== DeathReact() DIPANGGIL ===");
        Debug.Log($"[DeathReact] headDisplayer is {(headDisplayer == null ? "NULL" : "NOT NULL")}");
        
        if (fightAI != null) fightAI.DiedReact();

        // 玩家获得经验值
        if (combatCamp == CombatCamp.None || combatCamp == CombatCamp.Enemy)
        {
            if (playerAttributes == null) playerAttributes = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerAttributes>();
            playerAttributes.AddExperience(experience);
        }

        // 生成掉落物
        GetComponent<DropItem>().Drop();
        
        // ========== PANGGIL PORTAL ==========
        Debug.Log("[DeathReact] STEP 1: Sebelum if (headDisplayer != null)");
        
            if (headDisplayer != null)
            {
                Debug.Log("[DeathReact] STEP 2: Masuk ke dalam if, akan panggil headDisplayer.OnNPCDead()");

                try
                {
                    Debug.Log("[DeathReact] STEP 3: Memanggil headDisplayer.OnNPCDead()");
                    headDisplayer.OnNPCDead();
                    Debug.Log("[DeathReact] STEP 4: Selesai panggil OnNPCDead()");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[DeathReact] ERROR saat panggil OnNPCDead(): {e.Message}\n{e.StackTrace}");
                }
            }
        else
        {
            Debug.LogError("headDisplayer is NULL! Tidak bisa spawn portal.");
        }
        
        Debug.Log("[DeathReact] STEP 5: Sebelum headDisplayer?.HideState()");
        headDisplayer?.HideState();
        Debug.Log("[DeathReact] STEP 6: Sebelum CombatCharacterManager.Unregister");
        CombatCharacterManager.Instance.Unregister(this);
        Debug.Log("[DeathReact] STEP 7: Selesai");
    }

        /// <summary>
        /// 受伤反馈
        /// </summary>
        protected override void DamageReact()
        {
            if (fightAI != null) fightAI.GetDamageReact();
            headDisplayer.ShowState((float)health / MaxHealth);

            if (health <= 0)
        {
            Debug.Log("[DamageReact] Health <= 0, panggil DeathReact langsung");
            DeathReact();
        }
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

            Debug.Log($"[Init] fightAI = {(fightAI == null ? "NULL" : "OK")}, headDisplayer = {(headDisplayer == null ? "NULL" : "OK")}");

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

        // Fallback coroutine: if no portal appeared after death, try to instantiate
        // the portal using the assigned prefab on the EnemyHeadDisplayer (via reflection).
        private IEnumerator EnsurePortalSpawnCoroutine()
        {
            // wait a bit longer than typical death-delay to let headDisplayer spawn it
            yield return new WaitForSeconds(1.0f);

            // Quick check: if any GameObject exists whose name contains "PortalToScene",
            // assume portal is present.
            bool portalExists = false;
            foreach (var go in GameObject.FindObjectsOfType<GameObject>())
            {
                if (go.name.IndexOf("PortalToScene", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    portalExists = true;
                    break;
                }
            }

            if (portalExists)
            {
                Debug.Log("EnsurePortalSpawn: portal already present in scene, skipping fallback.");
                yield break;
            }

            if (headDisplayer == null)
            {
                Debug.LogWarning("EnsurePortalSpawn: headDisplayer is null, cannot find portal prefab.");
                yield break;
            }

            // Try to get the private serialized field 'portalPrefab' from EnemyHeadDisplayer
            var hdType = headDisplayer.GetType();
            FieldInfo f = hdType.GetField("portalPrefab", BindingFlags.NonPublic | BindingFlags.Instance);
            GameObject prefab = null;
            if (f != null)
            {
                prefab = f.GetValue(headDisplayer) as GameObject;
            }

            if (prefab == null)
            {
                Debug.LogWarning("EnsurePortalSpawn: could not read portalPrefab from headDisplayer.");
                yield break;
            }

            Vector3 spawnPos = transform.position + Vector3.up * 1.0f;
            Debug.Log($"EnsurePortalSpawn: instantiating fallback portal at {spawnPos}");
            var inst = Instantiate(prefab, spawnPos, Quaternion.identity) as GameObject;
            if (inst != null && !inst.activeSelf) inst.SetActive(true);

            // Try to attach PortalTrigger to the instantiated portal (if the type exists).
            System.Type ptType = null;
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var candidate = asm.GetType("PortalTrigger");
                    if (candidate != null)
                    {
                        ptType = candidate;
                        break;
                    }
                    foreach (var t in asm.GetTypes())
                    {
                        if (t.Name == "PortalTrigger")
                        {
                            ptType = t;
                            break;
                        }
                    }
                    if (ptType != null) break;
                }
                catch { }
            }

            if (inst != null && ptType != null)
            {
                Component ptComp = inst.GetComponent(ptType);
                if (ptComp == null) ptComp = inst.AddComponent(ptType);

                if (ptComp != null)
                {
                    var field = ptType.GetField("sceneName", BindingFlags.Public | BindingFlags.Instance) ?? ptType.GetField("sceneName", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (field != null)
                    {
                        field.SetValue(ptComp, "Scene3");
                    }
                    else
                    {
                        var prop = ptType.GetProperty("sceneName", BindingFlags.Public | BindingFlags.Instance) ?? ptType.GetProperty("sceneName", BindingFlags.NonPublic | BindingFlags.Instance);
                        if (prop != null) prop.SetValue(ptComp, "Scene3", null);
                    }
                    Debug.Log("EnsurePortalSpawn: PortalTrigger attached to fallback portal and sceneName set.");
                }
            }
            else if (ptType == null)
            {
                Debug.LogWarning("EnsurePortalSpawn: PortalTrigger type not found; instantiated portal will not auto-load scene.");
            }
        }

        // Fast, synchronous spawn attempt used immediately in DeathReact so portal
        // appears as soon as possible without waiting for coroutines.
        private void SpawnPortalImmediate()
        {
            try
            {
                // Quick guard: if there's already a portal in scene, skip
                foreach (var go in GameObject.FindObjectsOfType<GameObject>())
                {
                    if (go.name.IndexOf("PortalToScene", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        Debug.Log("SpawnPortalImmediate: portal already present, skipping.");
                        return;
                    }
                }

                if (headDisplayer == null)
                {
                    Debug.LogWarning("SpawnPortalImmediate: headDisplayer is null, cannot spawn portal.");
                    return;
                }

                var hdType = headDisplayer.GetType();
                var prefabField = hdType.GetField("portalPrefab", BindingFlags.NonPublic | BindingFlags.Instance);
                GameObject prefab = prefabField != null ? prefabField.GetValue(headDisplayer) as GameObject : null;

                // Attempt to read spawn offset from headDisplayer if present
                Vector3 offset = Vector3.up;
                var offField = hdType.GetField("portalSpawnOffset", BindingFlags.NonPublic | BindingFlags.Instance);
                if (offField != null)
                {
                    var val = offField.GetValue(headDisplayer);
                    if (val is Vector3) offset = (Vector3)val;
                }

                if (prefab == null)
                {
                    Debug.LogWarning("SpawnPortalImmediate: portalPrefab not assigned on headDisplayer.");
                    return;
                }

                Vector3 spawnPos = transform.position + offset;
                Debug.Log($"SpawnPortalImmediate: instantiating portal at {spawnPos}");
                var inst = Instantiate(prefab, spawnPos, Quaternion.identity) as GameObject;
                if (inst != null && !inst.activeSelf) inst.SetActive(true);

                // Attach PortalTrigger if available
                System.Type ptType = null;
                foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        var candidate = asm.GetType("PortalTrigger");
                        if (candidate != null) { ptType = candidate; break; }
                        foreach (var t in asm.GetTypes())
                        {
                            if (t.Name == "PortalTrigger") { ptType = t; break; }
                        }
                        if (ptType != null) break;
                    }
                    catch { }
                }

                if (inst != null && ptType != null)
                {
                    var ptComp = inst.GetComponent(ptType) ?? inst.AddComponent(ptType);
                    if (ptComp != null)
                    {
                        var field = ptType.GetField("sceneName", BindingFlags.Public | BindingFlags.Instance) ?? ptType.GetField("sceneName", BindingFlags.NonPublic | BindingFlags.Instance);
                        if (field != null) field.SetValue(ptComp, "Scene3");
                        else
                        {
                            var prop = ptType.GetProperty("sceneName", BindingFlags.Public | BindingFlags.Instance) ?? ptType.GetProperty("sceneName", BindingFlags.NonPublic | BindingFlags.Instance);
                            if (prop != null) prop.SetValue(ptComp, "Scene3", null);
                        }
                        Debug.Log("SpawnPortalImmediate: PortalTrigger attached and configured on spawned portal.");
                    }
                }
                else if (ptType == null)
                {
                    Debug.LogWarning("SpawnPortalImmediate: PortalTrigger type not found; spawned portal won't auto-load scene.");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError("SpawnPortalImmediate: exception while spawning portal: " + ex.Message);
            }
        }
        