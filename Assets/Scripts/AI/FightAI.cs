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
    [Header("Attack Options")]
    [Tooltip("Chance to use 'Slash' trigger when available (0..1).")]
    [Range(0f, 1f)]
    [SerializeField]
    protected float slashChance = 0.5f;
    [Tooltip("If true, this instance is allowed to use the 'Slash' trigger when available. Set true for boss prefab only.")]
    [SerializeField]
    protected bool allowSlash = false;
    [Tooltip("If true, this instance will allow Animator root motion to drive movement. Set true for boss prefabs that require root motion.")]
    [SerializeField]
    protected bool allowRootMotion = false;
    [Tooltip("Minimum time the AI will remain locked in an attack state after triggering an attack (seconds).")]
    [SerializeField]
    protected float minAttackLockTime = 0.25f;
    [Tooltip("Maximum time to force-release an attack lock if animation events or states are missing (seconds).")]
    [SerializeField]
    protected float maxAttackLockTime = 2.5f;
    [Tooltip("Fallback delay (seconds) to apply damage if the animation clip does not contain an AttackEvent."
        + " Set higher for longer attack animations.")]
    [SerializeField]
    protected float fallbackAttackDelay = 0.35f;

    // runtime attack event tracking for fallback
    protected bool attackEventFired = false;
    private Coroutine attackFallbackCoroutine = null;
    // runtime attack lock to prevent AI from cancelling an attack animation early
    protected bool attackLocked = false;
    private Coroutine attackLockCoroutine = null;

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
        // If this instance uses root motion, prevent the NavMeshAgent from directly
        // applying transform changes so Animator-driven motion can take effect.
        if (agent != null && allowRootMotion)
        {
            try
            {
                agent.updatePosition = false;
                agent.updateRotation = false;
                Debug.Log($"{name} FightAI: NavMeshAgent.updatePosition/updateRotation disabled to allow root motion.");
            }
            catch { }
        }

        // If using root motion and we have a Rigidbody, configure it to reduce
        // tunnelling and improve collision response while animator moves the body.
        try
        {
            var rb = GetComponent<Rigidbody>();
            if (rb != null && allowRootMotion)
            {
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                Debug.Log($"{name} FightAI: configured Rigidbody collisionDetection={rb.collisionDetectionMode} interpolation={rb.interpolation} for root motion.");
            }
        }
        catch { }

    }

    /// <summary>
    /// Helper: check whether the Animator has a parameter with given name.
    /// </summary>
    /// <param name="paramName"></param>
    /// <returns></returns>
    protected bool AnimatorHasParameter(string paramName)
    {
        if (animator == null) return false;
        foreach (var p in animator.parameters)
        {
            if (p.name == paramName) return true;
        }
        return false;
    }

    /// <summary>
    /// Trigger the preferred attack parameter if it exists (e.g. "Slash"),
    /// otherwise fallback to the legacy "Attack" trigger.
    /// </summary>
    protected void TriggerAttackParameter()
    {
        if (animator == null) return;
        // Only attempt to use 'Slash' when this instance is explicitly allowed
        if (allowSlash && AnimatorHasParameter("Slash"))
        {
            if (Random.value < slashChance)
                animator.SetTrigger("Slash");
            else
                animator.SetTrigger("Attack");
        }
        else
        {
            animator.SetTrigger("Attack");
        }
        // Ensure AI remains locked in attack state until animation completes or timeout
        BeginAttackLock(minAttackLockTime);
    }

    /// <summary>
    /// Schedule a fallback attack callback to run after <see cref="fallbackAttackDelay"/>
    /// if the animation's AttackEvent was not invoked.
    /// </summary>
    /// <param name="delay">Delay in seconds to wait before fallback action</param>
    /// <param name="fallbackAction">Action to perform if no AttackEvent fired (e.g. call AreaDamage)</param>
    protected void ScheduleAttackFallback(float delay, System.Action fallbackAction)
    {
        if (attackFallbackCoroutine != null)
        {
            try { StopCoroutine(attackFallbackCoroutine); } catch { }
            attackFallbackCoroutine = null;
        }
        attackEventFired = false;
        attackFallbackCoroutine = StartCoroutine(AttackFallbackCoroutine(delay, fallbackAction));
    }

    private IEnumerator AttackFallbackCoroutine(float delay, System.Action fallbackAction)
    {
        yield return new WaitForSeconds(delay);
        if (!attackEventFired)
        {
            Debug.Log($"{name} FightAI: Fallback attack triggered after {delay:F2}s because no AnimationEvent fired.");
            try { fallbackAction?.Invoke(); } catch (System.Exception e) { Debug.LogWarning($"{name} FightAI: fallbackAction exception: {e.Message}"); }
        }
        attackFallbackCoroutine = null;
        attackEventFired = false;
    }

    /// <summary>
    /// Begin a short 'attack lock' to prevent AI from cancelling or moving away
    /// until the animation has a chance to play through. The lock will be
    /// released when the animator leaves the attack state or when a timeout
    /// elapses to avoid permanent locking when clips/events are missing.
    /// </summary>
    /// <param name="minLock">minimum seconds to keep locked (if &gt;0)</param>
    protected void BeginAttackLock(float minLock = -1f)
    {
        if (attackLockCoroutine != null)
        {
            try { StopCoroutine(attackLockCoroutine); } catch { }
            attackLockCoroutine = null;
        }
        Debug.Log($"{name} FightAI: BeginAttackLock(minLock={minLock:F3})");
        attackLockCoroutine = StartCoroutine(AttackLockCoroutine(minLock));
    }

    protected void EndAttackLock()
    {
        if (attackLockCoroutine != null)
        {
            try { StopCoroutine(attackLockCoroutine); } catch { }
            attackLockCoroutine = null;
        }
        attackLocked = false;
        Debug.Log($"{name} FightAI: EndAttackLock()");
    }

    private IEnumerator AttackLockCoroutine(float minLock)
    {
        attackLocked = true;
        float start = Time.time;
        float minT = minLock > 0f ? minLock : minAttackLockTime;

        while (true)
        {
            // If animator not present, just wait minT then release
            if (animator == null)
            {
                if (Time.time - start >= minT) break;
            }
            else
            {
                AnimatorStateInfo cur = animator.GetCurrentAnimatorStateInfo(0);
                AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(0);
                bool inAttack = cur.IsTag("Attack") || next.IsTag("Attack") || cur.IsName("Attack") || next.IsName("Attack") || cur.IsName("Slash") || next.IsName("Slash");
                // release only after the animator is no longer in the attack state and minimum elapsed
                if (!inAttack && Time.time - start >= minT) break;
            }

            if (Time.time - start >= maxAttackLockTime)
            {
                Debug.LogWarning($"{name} FightAI: Attack lock timeout reached ({maxAttackLockTime}s), releasing lock to avoid hang.");
                break;
            }

            yield return null;
        }

        attackLocked = false;
        attackLockCoroutine = null;
    }

    /// <summary>
    /// AnimationEvent hook: place this near the end of the attack clip to
    /// explicitly release the attack lock. Editor tool can add this event.
    /// </summary>
    public void AttackEndEvent()
    {
        attackEventFired = false;
        Debug.Log($"{name} FightAI: AttackEndEvent called -> releasing attack lock");
        EndAttackLock();
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

        // also ensure our runtime root-motion handling is disabled so OnAnimatorMove
        // doesn't apply any residual deltaPosition after death.
        try
        {
            allowRootMotion = false;
        }
        catch { }

        // Trigger death animation (animator now not applying root motion)
        if (animator != null) animator.SetTrigger("Died");

        // Try to snap visual root to ground to avoid hovering corpses
        TrySnapToGround();

        // disable agent so AI stops moving; keep colliders so the corpse stays on ground
        if (agent != null) agent.enabled = false;

        // If there is a Rigidbody present, ensure it is kinematic so physics doesn't push the corpse through geometry.
        try
        {
            // set all rigidbodies in hierarchy to kinematic to prevent physics after death
            var rbs = GetComponentsInChildren<Rigidbody>(true);
            foreach (var rb in rbs)
            {
                if (rb == null) continue;
                try
                {
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.isKinematic = true; // keep kinematic so animation pose is preserved
                    rb.useGravity = false;
                }
                catch { }
            }
            if (rbs != null && rbs.Length > 0)
            {
                Debug.Log($"{name} FightAI: Set {rbs.Length} Rigidbody(s) kinematic on death.");
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

    // When using root motion with a NavMeshAgent, keep the agent's nextPosition
    // in sync with the animated transform so pathfinding remains meaningful.
    private void OnAnimatorMove()
    {
        if (!allowRootMotion || animator == null) return;

        // deltaPosition/rootRotation computed by the Animator for this frame
        Vector3 delta = animator.deltaPosition;
        Quaternion rot = animator.rootRotation;

        // Warn if parent scale is not uniform which commonly distorts root motion
        if (transform.lossyScale != Vector3.one)
        {
            Debug.LogWarning($"{name} FightAI: Non-unit lossyScale={transform.lossyScale}. Non-uniform scaling on parents can distort root motion.");
        }

        var rb = GetComponent<Rigidbody>();
        // If the NavMeshAgent is configured to NOT update the transform, let
        // the Animator move the transform and keep the agent.nextPosition in sync.
        if (agent != null && !agent.updatePosition)
        {
            if (rb != null && !rb.isKinematic)
            {
                // Use SweepTest to avoid moving through colliders when root motion
                // produces large frame deltas (reduces tunnelling).
                float moveDist = delta.magnitude;
                if (moveDist > 0.0001f)
                {
                    Vector3 dir = delta / moveDist;
                    RaycastHit hit;
                    if (rb.SweepTest(dir, out hit, moveDist + 0.01f, QueryTriggerInteraction.Ignore))
                    {
                        // move up to the contact point minus small epsilon
                        float safeDist = Mathf.Max(0f, hit.distance - 0.01f);
                        rb.MovePosition(rb.position + dir * safeDist);
                    }
                    else
                    {
                        rb.MovePosition(rb.position + delta);
                    }
                }
                rb.MoveRotation(rot);
            }
            else if (rb != null)
            {
                // kinematic Rigidbody or other: fall back to transform update
                transform.position += delta;
                transform.rotation = rot;
            }
            else
            {
                transform.position += delta;
                transform.rotation = rot;
            }

            try { agent.nextPosition = transform.position; } catch { }
        }
        else if (agent != null)
        {
            // Agent is driving position; avoid applying delta to prevent double-movement.
            transform.rotation = rot;
        }
        else
        {
            // No NavMeshAgent: apply root motion directly (or via Rigidbody if present)
            if (rb != null && !rb.isKinematic)
            {
                float moveDist = delta.magnitude;
                if (moveDist > 0.0001f)
                {
                    Vector3 dir = delta / moveDist;
                    RaycastHit hit;
                    if (rb.SweepTest(dir, out hit, moveDist + 0.01f, QueryTriggerInteraction.Ignore))
                    {
                        float safeDist = Mathf.Max(0f, hit.distance - 0.01f);
                        rb.MovePosition(rb.position + dir * safeDist);
                    }
                    else
                    {
                        rb.MovePosition(rb.position + delta);
                    }
                }
                rb.MoveRotation(rot);
            }
            else if (rb != null)
            {
                transform.position += delta;
                transform.rotation = rot;
            }
            else
            {
                transform.position += delta;
                transform.rotation = rot;
            }
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
