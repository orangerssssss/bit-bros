using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 战斗AI的抽象类, 控制npc的战斗行为. 包含了AI基本的属性及功能, 及子类必须实现的抽象方法
/// </summary>
public abstract class FightAI : MonoBehaviour
{

    protected Animator animator;// 动画组件
    protected Collider enemyCollider;// 碰撞器组件
    protected NavMeshAgent agent;// 寻路代理组件
    protected FightAttributes fightAttributes;// 属性组件

    [SerializeField]
    protected float corpseDelay = 3.0f;
    [SerializeField]
    protected ParticleSystem damageParticle;// 受伤粒子效果
    [SerializeField]
    protected AudioSource damageSFX;

    [HideInInspector]
    public EnemySummonPoint summonPoint;// 生成点. 如果敌人由生成点生成, 则此值不为空, 在销毁时有不同行为

    // state flag to indicate the character is dead so derived Update loops can stop acting
    protected bool isDead = false;

    private void Awake()
    {
        InitFightAI();
    }

    protected virtual void InitFightAI()
    {
        animator = GetComponent<Animator>();
        enemyCollider = GetComponent<Collider>();
        agent = GetComponent<NavMeshAgent>();
        fightAttributes = GetComponent<FightAttributes>();

    }

    /// <summary>
    /// 死亡
    /// </summary>
    public virtual void DiedReact()
    {
        // mark dead early so Update loops stop performing movement
        isDead = true;

        // disable any CharacterController to avoid it driving the transform after death
        try
        {
            var cc = GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
        }
        catch { }

        // keep colliders enabled so the corpse rests on ground (do not disable colliders)

        // Ensure animator doesn't move root (avoid animation pushing corpse into air)
        if (animator != null && animator.applyRootMotion)
        {
            animator.applyRootMotion = false;
            Debug.Log($"{name} FightAI: Disabled animator.applyRootMotion on death to avoid root motion lifting the corpse.");
        }

        // Trigger death animation (animator now not applying root motion)
        if (animator != null) animator.SetTrigger("Died");

        // Try to snap visual root to ground to avoid hovering corpses
        TrySnapToGround();

        // disable agent so AI stops moving; keep colliders so the corpse stays on ground
        if (agent != null) agent.enabled = false;

        // If there is a Rigidbody present, ensure it is kinematic so physics doesn't push the corpse through geometry.
        try
        {
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true; // keep kinematic so animation pose is preserved
                rb.useGravity = false;
                Debug.Log($"{name} FightAI: Set Rigidbody kinematic on death (isKinematic=true).");
            }
        }
        catch { }

        // 开始销毁协程
        if (gameObject.activeInHierarchy) StartCoroutine(EnemyDestroy());
    }

    /// <summary>
    /// Attempt to snap the corpse visually to the ground surface beneath it.
    /// Uses renderer bounds to compute lowest point and raycasts down to find ground.
    /// This helps avoid cases where death animation or disabled colliders leave the model floating.
    /// </summary>
    private void TrySnapToGround()
    {
        try
        {
            var rends = GetComponentsInChildren<Renderer>(true);
            if (rends == null || rends.Length == 0) return;

            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++)
            {
                if (rends[i] != null) b.Encapsulate(rends[i].bounds);
            }

            float lowest = b.min.y; // world y of lowest renderer vertex
            float pivotY = transform.position.y;
            float lowestDelta = lowest - pivotY; // how far below pivot the visual sits

            // Raycast down a reasonable distance to find ground
            RaycastHit hit;
            Vector3 rayStart = transform.position + Vector3.up * 0.5f;
            if (Physics.Raycast(rayStart, Vector3.down, out hit, 10f, ~0, QueryTriggerInteraction.Ignore))
            {
                float newY = hit.point.y - lowestDelta;
                // Avoid snapping upwards unexpectedly; only move down or small adjustments
                if (newY <= transform.position.y + 0.05f)
                {
                    transform.position = new Vector3(transform.position.x, newY, transform.position.z);
                    Debug.Log($"{name} FightAI: Snapped corpse to ground at {hit.point.y:F2} (lowestDelta={lowestDelta:F2}).");
                }
                else
                {
                    Debug.Log($"{name} FightAI: Snap would move up (newY={newY:F2}), skipping.");
                }
            }
            else
            {
                Debug.Log($"{name} FightAI: No ground detected below to snap to.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"FightAI: TrySnapToGround failed: {e.Message}");
        }
    }

    /// <summary>
    /// 受伤
    /// </summary>
    public virtual void GetDamageReact()
    {
        // 播放受伤粒子效果
        damageParticle.Play();
        damageSFX.Stop();
        damageSFX.Play();
    }

    /// <summary>
    /// 当攻击被玩家盾挡/招架时，攻击者的反应（默认无动作，子类可实现僵直）
    /// </summary>
    public virtual void OnShieldBlocked(float staggerDuration)
    {
    }

    /// <summary>
    /// 初始化
    /// </summary>
    public abstract void ResetFightAI();

    /// <summary>
    /// 敌人销毁协程. 由生成点生成的敌人被初始化并隐藏, 等待重新生成; 不由生成点生成则直接销毁该物体.
    /// </summary>
    protected IEnumerator EnemyDestroy()
    {
        // Wait for death animation / corpseDelay then remove the corpse (no physics-driven sinking)
        yield return new WaitForSeconds(corpseDelay);

        // 销毁
        if (!summonPoint)
        {
            Destroy(gameObject);
        }
        else
        {
            summonPoint.EnemyDied();

            ResetFightAI();
            fightAttributes.InitAttributes();
            fightAttributes.headDisplayer.InitState();

            gameObject.SetActive(false);
        }
    }
}
