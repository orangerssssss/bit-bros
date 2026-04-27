using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 角色属性抽象类, 包含基本的角色属性及方法
/// </summary>
public abstract class CharacterAttributes : MonoBehaviour
{
    public enum CharacterCareer
    {
        无,
        骑士,
        法师,
        村民
    }

    public int health;// 当前生命
    public int mana;// 当前魔法

    public int level = 0;// 等级
    public int experience;// 经验

    public CharacterCareer career;

    [SerializeField]
    private int constitution;
    [SerializeField]
    private int strength;
    [SerializeField]
    private int intelligence;
    public CombatCamp combatCamp;

    public int MaxHealth { get; private set; }// 最大生命
    public int PhysicalAttack { get; private set; }// 物理攻击
    public int PhysicalDefence { get; private set; }// 物理防御
    public int MagicAttack { get; private set; }// 魔法攻击
    public int MagicDefence { get; private set; }// 魔法防御
    public int MaxMana { get; private set; }// 魔法值

    public int Constitution
    {
        get
        {
            return constitution;
        }
        set
        {
            constitution = Mathf.Max(0, value);
            RecalculateAttributes();
        }
    }

    public int Strength
    {
        get
        {
            return strength;
        }
        set
        {
            strength = Mathf.Max(0, value);
            RecalculateAttributes();
        }
    }

    public int Intelligence
    {
        get
        {
            return intelligence;
        }
        set
        {
            intelligence = Mathf.Max(0, value);
            RecalculateAttributes();
        }
    }

    protected bool protect = false;

    private void OnValidate()
    {
        //RecalculateAttributes();
    }

    /// <summary>
    /// 死亡
    /// </summary>
    protected abstract void DeathReact();

    /// <summary>
    /// 受伤反馈
    /// </summary>
    protected abstract void DamageReact();

    /// <summary>
    /// 治疗反馈
    /// </summary>
    protected abstract void HealReact();

    protected virtual void MissReact()
    {

    }

    /// <summary>
    /// 初始化
    /// </summary>
    protected abstract void Init();

    private void Awake()
    {
        RecalculateAttributes();

        Init();
    }

    public void Heal(int value)
    {
        if (value <= 0 || health <= 0) return;

        health = Mathf.Clamp(health + value, 0, MaxHealth);

        HealReact();

    }

    public void GetAttack(int attack, bool isPhysical)
    {
        if (attack <= 0 || health <= 0) return;
        if (protect) return;

        // Parry / block integration: if this GameObject has PlayerBlock, check parry window or hold-block
        var playerBlock = GetComponent<PlayerBlock>();
        if (playerBlock != null)
        {
            if (playerBlock.IsInParryWindow())
            {
                Debug.Log($"{gameObject.name} CharacterAttributes: Parried attack.");
                // Parry — treat as miss/counter; do not apply damage
                // notify combat controller to play shield effects
                var pcc = GetComponent<PlayerCombatController>();
                if (pcc != null) pcc.OnShieldBlocked();
                MissReact();
                return;
            }

            if (playerBlock.IsBlocking())
            {
                Debug.Log($"{gameObject.name} CharacterAttributes: Blocked attack (holding).");
                // Holding block — treat as blocked (no damage). Optionally trigger block reaction.
                var pcc2 = GetComponent<PlayerCombatController>();
                if (pcc2 != null) pcc2.OnShieldBlocked();
                MissReact();
                return;
            }
        }

        int damage = ResistDamage(attack, isPhysical);
        health = Mathf.Clamp(health - damage, 0, MaxHealth);

        Debug.Log($"{gameObject.name} CharacterAttributes: Received attack={attack}, damageApplied={damage}, newHealth={health}");

        DamageReact();

        if (health <= 0)
        {
                // Invoke before-death event if available; otherwise fallback to local spawners
                Debug.Log($"CharacterAttributes: {gameObject.name} died; preparing death events (health={health}).");
                var gem = GameEventManager.Instance;
                if (gem != null && gem.characterBeforeDeathEvent != null)
                {
                    Debug.Log($"CharacterAttributes: Invoking GameEventManager.characterBeforeDeathEvent for {gameObject.name}.");
                    gem.characterBeforeDeathEvent.Invoke(this);
                }
                else
                {
                    Debug.Log($"CharacterAttributes: GameEventManager or characterBeforeDeathEvent missing for {gameObject.name}; using SpawnPortalOnDeath fallback (if any).");
                    // fallback: if this object has SpawnPortalOnDeath, trigger immediate spawn
                    var spawner = GetComponent<SpawnPortalOnDeath>();
                    if (spawner != null)
                    {
                        spawner.TriggerPortalSpawnImmediate();
                    }
                }

            if (health <= 0)
            {
                DeathReact();
                var ccm = CombatCharacterManager.Instance;
                if (ccm != null)
                {
                    ccm.Unregister(this);
                }
            }
        }
    }

    /// <summary>
    /// 防御减伤计算
    /// </summary>
    /// <param name="value">伤害</param>
    /// <returns>减伤后的值</returns>
    private int ResistDamage(int value, bool isPhysical)
    {
        int defence = isPhysical ? PhysicalDefence : MagicDefence;
        value = value * value / (value + defence);
        return value;
    }

    public void RecalculateAttributes()
    {
        MaxHealth = Constitution * 16 + Strength * 2;
        if (MaxHealth <= 0) MaxHealth = 1;
        if (health > MaxHealth) health = MaxHealth;
        PhysicalAttack = Strength * 3;
        PhysicalDefence = (int)(Constitution * 0.6 + Strength * 0.4);
        MagicAttack = (int)(Intelligence * 3.3);
        MagicDefence = (int)(Intelligence * 0.45 + Constitution * 0.6);
        MaxMana = Intelligence;
        if (mana > MaxMana) mana = MaxMana;
    }
}
