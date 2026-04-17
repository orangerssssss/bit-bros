using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 士兵AI, 包括行为及动画表现
/// </summary>
public class FightAISoldier : FightAI
{
    private static Transform player;// 玩家
    private float debugLogCooldown = 1.0f;
    private float lastDebugLogTime = -10.0f;

    [SerializeField]
    private float patrolRadius;// 巡逻半径(m)
    [SerializeField]
    private float patrolIdleTime = 10.0f;// 巡逻时站立时间
    [SerializeField]
    protected float alarmDistance;// 警戒距离(m)
    [SerializeField]
    protected float fightDistance;// 战斗距离(m)
    [SerializeField]
    protected float attackDistance;// 攻击距离(m)
    [SerializeField, Range(0, 1)]
    private float attackDesire;// 攻击欲望, 敌人在战斗距离内向前移动的概率及能够攻击时进行攻击的概率
    [SerializeField]
    private bool uncertainInterval = true;// 是否启用随机间隔, 启用后间隔时间会产生波动, 用于让敌人的行为产生不统一感
    [SerializeField]
    private float fightStateInterval;// 战斗状态的行为间隔
    [SerializeField]
    private float attackInterval;// 攻击间隔
    [SerializeField]
    private BoxAttackArea attackBox;// 攻击碰撞盒
    [SerializeField]
    private AudioSource attackAudioSource;

    private float fightStateTimer;// 战斗状态行为计时器
    private float attackTimer;// 攻击计时器
    private float patrolTimer;// 巡逻计时器

    private float targetMoveSpeed;// 目标移动速度, 用于速度插值, 避免速度的突然变化
    private bool moveForward = true;// 是否向前移动(战斗状态)
    private Vector3 startPos;
    [SerializeField]
    private float fallbackMoveSpeed = 1.0f; // speed when NavMesh not available
    [SerializeField]
    private float fallbackRunSpeed = 3.5f;
    private Vector3 fallbackPatrolDest;
    private bool hasFallbackPatrolDest = false;

    private CharacterAttributes target;

    protected override void InitFightAI()
    {
        base.InitFightAI();

        if (player == null) player = GameObject.FindGameObjectWithTag("Player").transform;

        // 间隔产生小范围随机变化
        if (uncertainInterval)
        {
            fightStateInterval += Random.Range(-fightStateInterval / 8, fightStateInterval / 8);
            attackInterval += Random.Range(-attackInterval / 8, attackInterval / 8);
        }
    }

    private void OnEnable()
    {
        startPos = transform.position;

        // Log OnEnable state for diagnostics
        bool agentPresent = agent != null;
        bool agentEnabledState = agentPresent ? agent.enabled : false;
        bool isOnNav = agentPresent ? agent.isOnNavMesh : false;
        Debug.Log($"{name} FightAISoldier OnEnable: agentPresent={agentPresent}, agentEnabled={agentEnabledState}, isOnNavMesh={isOnNav}");

        // Ensure NavMeshAgent is enabled and on the NavMesh. If not on NavMesh, try to sample nearest NavMesh
        if (agent != null)
        {
            if (!agent.enabled) agent.enabled = true;

            if (!agent.isOnNavMesh)
            {
                NavMeshHit hit;
                float sampleDist = 10.0f;
                if (NavMesh.SamplePosition(transform.position, out hit, sampleDist, NavMesh.AllAreas))
                {
                    agent.Warp(hit.position);
                    Debug.Log($"{name} FightAISoldier: Warped to nearest NavMesh at {hit.position}");
                }
                else
                {
                    Debug.LogWarning($"{name} FightAISoldier: No NavMesh within {sampleDist}m of spawn position. Bake NavMesh or move spawn.");
                }
            }
        }
    }

    private void Update()
    {
        // Guard against missing components or agents not placed on NavMesh
        if (agent == null || animator == null || fightAttributes == null)
        {
            if (Time.time - lastDebugLogTime > debugLogCooldown)
            {
                if (agent == null) Debug.LogWarning($"{name} FightAISoldier: NavMeshAgent (agent) is null");
                if (animator == null) Debug.LogWarning($"{name} FightAISoldier: Animator is null");
                if (fightAttributes == null) Debug.LogWarning($"{name} FightAISoldier: FightAttributes is null");
                lastDebugLogTime = Time.time;
            }
            return;
        }
        if (!agent.enabled)
        {
            if (Time.time - lastDebugLogTime > debugLogCooldown)
            {
                Debug.LogWarning($"{name} FightAISoldier: agent.enabled == false (will attempt fallback movement)");
                lastDebugLogTime = Time.time;
            }
            // do not return; allow fallback transform-based movement below when agent is disabled
        }
        // avoid calling NavMeshAgent operations when agent not on a NavMesh, but allow fallback movement
        if (!agent.isOnNavMesh)
        {
            if (Time.time - lastDebugLogTime > debugLogCooldown)
            {
                Debug.LogWarning($"{name} FightAISoldier: agent.isOnNavMesh == false (enemy not on NavMesh)");
                lastDebugLogTime = Time.time;
            }
            // DO NOT return; fallback movement handled below
        }

        // 更新攻击计时器
        attackTimer += Time.deltaTime;

        // 在非移动状态停止寻路 (仅当 agent 在 NavMesh 可用时)
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = !animator.GetCurrentAnimatorStateInfo(0).IsTag("Move");
        }

        // 寻找最近的目标 (安全调用, cek null untuk CombatCharacterManager)
        var ccm = CombatCharacterManager.Instance;
        if (ccm != null)
        {
            if (fightAttributes.combatCamp == CombatCamp.Player)
                target = ccm.FindNearestEnemyCampCharacter(transform.position);
            else if (fightAttributes.combatCamp == CombatCamp.Enemy)
                target = ccm.FindNearestPlayerCampCharacter(transform.position);
            else
                target = null;
        }
        else
        {
            target = null;
            if (Time.time - lastDebugLogTime > debugLogCooldown)
            {
                Debug.LogWarning($"{name} FightAISoldier: CombatCharacterManager.Instance is null");
                lastDebugLogTime = Time.time;
            }
        }

        if (Time.time - lastDebugLogTime > debugLogCooldown)
        {
            string tname = target != null ? target.gameObject.name : "null";
            Debug.Log($"{name} FightAISoldier DEBUG: target={tname}, agent.enabled={agent.enabled}, isOnNavMesh={agent.isOnNavMesh}, health={fightAttributes.health}");
            lastDebugLogTime = Time.time;
        }

        // 行动 (gunakan fallback jika NavMesh tidak tersedia)
        if (target != null)
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh)
                Action(target.transform.position);
            else
                FallbackAction(target.transform.position);
        }
        else
        {
            PatrolWithoutTarget();
        }

        // 插值动画机中的移动速度为目标移动速度
        animator.SetFloat("MoveSpeed", Mathf.Lerp(animator.GetFloat("MoveSpeed"), targetMoveSpeed, 0.1f));
    }

    /// <summary>
    /// Patrol fallback when there is no detected target.
    /// </summary>
    private void PatrolWithoutTarget()
    {
        // If NavMesh agent available and on mesh, use agent movement
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            if (animator.GetBool("Alarm"))
            {
                animator.SetBool("Alarm", false);
                agent.destination = transform.position;
                patrolTimer = 0.8f;
            }

            targetMoveSpeed = agent.velocity.magnitude;

            if (patrolRadius > 0)
            {
                patrolTimer -= Time.deltaTime;
                if (patrolTimer < 0 && !agent.pathPending && agent.remainingDistance < 0.5f)
                {
                    Vector2 randomPos = Random.insideUnitCircle * Random.Range(0, patrolRadius);
                    agent.destination = startPos + new Vector3(randomPos.x, 0, randomPos.y);
                    patrolTimer = patrolIdleTime;
                }
            }

            return;
        }

        // Fallback transform-based patrol when NavMesh not available
        if (animator.GetBool("Alarm"))
        {
            animator.SetBool("Alarm", false);
            patrolTimer = 0.8f;
        }

        targetMoveSpeed = 0f;

        if (patrolRadius > 0)
        {
            patrolTimer -= Time.deltaTime;
            if (!hasFallbackPatrolDest || patrolTimer < 0)
            {
                Vector2 randomPos = Random.insideUnitCircle * Random.Range(0, patrolRadius);
                fallbackPatrolDest = startPos + new Vector3(randomPos.x, 0, randomPos.y);
                hasFallbackPatrolDest = true;
                patrolTimer = patrolIdleTime;
            }

            Vector3 dir = fallbackPatrolDest - transform.position;
            dir.y = 0;
            if (dir.magnitude > 0.2f)
            {
                float speed = fallbackMoveSpeed;
                transform.position += dir.normalized * speed * Time.deltaTime;
                Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, Time.deltaTime * 4f);
                targetMoveSpeed = speed;
            }
            else
            {
                hasFallbackPatrolDest = false;
            }
        }
    }

    /// <summary>
    /// Fallback chase/attack when NavMesh is not available (uses transform movement)
    /// </summary>
    private void FallbackAction(Vector3 targetPos)
    {
        float dis = Vector3.Distance(targetPos, transform.position);

        if (dis > alarmDistance) // idle/patrol
        {
            if (animator.GetBool("Alarm")) animator.SetBool("Alarm", false);
            PatrolWithoutTarget();
            return;
        }

        if (!animator.GetBool("Alarm")) animator.SetBool("Alarm", true);

        // chase
        if (moveForward)
        {
            if (dis > attackDistance)
            {
                Vector3 dir = targetPos - transform.position;
                dir.y = 0;
                float speed = fallbackMoveSpeed;
                transform.position += dir.normalized * speed * Time.deltaTime;
                Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, Time.deltaTime * 12.0f);
                targetMoveSpeed = speed;
            }
            else
            {
                targetMoveSpeed = 0.0f;
                if (attackTimer > attackInterval)
                {
                    Debug.Log($"{name} FightAISoldier: Fallback attack check (dis={dis:F2}) attackTimer={attackTimer:F2} attackInterval={attackInterval} attackDesire={attackDesire}");
                    if (Random.Range(0f, 1f) < attackDesire)
                    {
                        Debug.Log($"{name} FightAISoldier: Triggering Attack (in FallbackAction)");
                        animator.SetTrigger("Attack");
                        attackTimer = 0f;
                    }
                    else
                    {
                        attackTimer = attackInterval;
                    }
                }
            }
        }
        else
        {
            // move backward a bit or stand still
            targetMoveSpeed = 0f;
        }
    }

    /// <summary>
    /// 受伤
    /// </summary>
    public override void GetDamageReact()
    {
        base.GetDamageReact();

        // 触发受伤动画
        if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Attack") && !animator.GetNextAnimatorStateInfo(0).IsName("Attack") && fightAttributes.health > 0)
        {
            animator.SetTrigger("Impact");
        }
    }

    /// <summary>
    /// 初始化
    /// </summary>
    public override void ResetFightAI()
    {
        // reset timers and movement state
        fightStateTimer = 0f;
        attackTimer = attackInterval;
        patrolTimer = 0f;
        hasFallbackPatrolDest = false;
        targetMoveSpeed = 0f;
        moveForward = true;

        if (agent != null) agent.enabled = true;

        if (animator != null)
        {
            animator.ResetTrigger("Attack");
            animator.ResetTrigger("Impact");
            animator.SetBool("Alarm", false);
            animator.SetFloat("MoveSpeed", 0f);
        }
    }

    /// <summary>
    /// 主行为逻辑, 在玩家靠近时接近玩家并进行攻击
    /// </summary>
    private void Action(Vector3 targetPos)
    {
        float dis = Vector3.Distance(targetPos, transform.position);

        if (dis > alarmDistance) // 待机状态(大于警戒距离)
        {
            if (animator.GetBool("Alarm"))
            {
                animator.SetBool("Alarm", false);
                if (agent != null) agent.destination = transform.position;
                patrolTimer = 0.8f;
            }

            targetMoveSpeed = agent != null ? agent.velocity.magnitude : 0f;
            if (patrolRadius > 0)
            {
                patrolTimer -= Time.deltaTime;
                if (patrolTimer < 0 && agent != null && !agent.pathPending && agent.remainingDistance < 0.5f)
                {
                    Vector2 randomPos = Random.insideUnitCircle * Random.Range(0, patrolRadius);
                    if (agent != null) agent.destination = startPos + new Vector3(randomPos.x, 0, randomPos.y);
                    patrolTimer = patrolIdleTime;
                }
            }
        }
        else // 警戒状态(警戒距离内)
        {
            if (!animator.GetBool("Alarm")) animator.SetBool("Alarm", true);

            if (agent != null && agent.enabled) agent.destination = targetPos;
            targetMoveSpeed = 1.0f;

            // 更新战斗状态的行为
            FightStateUpdate(dis < fightDistance);

            if (moveForward)
            {
                if (dis < attackDistance)
                {
                    // 停止移动
                    if (agent != null) agent.velocity = Vector3.zero;
                    targetMoveSpeed = 0.0f;

                    // 攻击间隔判断
                    if (attackTimer > attackInterval)
                    {
                        Debug.Log($"{name} FightAISoldier: Attack check (dis={dis:F2}) attackTimer={attackTimer:F2} attackInterval={attackInterval} attackDesire={attackDesire}");
                        if (Random.Range(0f, 1f) < attackDesire)
                        {
                            Debug.Log($"{name} FightAISoldier: Attempting Trigger Attack (in Action)");
                            if (!animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack") && !animator.GetNextAnimatorStateInfo(0).IsTag("Attack"))
                            {
                                Debug.Log($"{name} FightAISoldier: Triggering Attack (in Action)");
                                animator.SetTrigger("Attack");
                                attackTimer = 0f;
                            }
                            else
                            {
                                attackTimer = attackInterval;
                            }
                        }
                        else
                        {
                            attackTimer = attackInterval;
                        }
                    }
                    else
                    {
                        Vector3 dirToTarget = targetPos - transform.position;
                        dirToTarget.y = 0;
                        if (dirToTarget.sqrMagnitude > 0.0001f)
                        {
                            Quaternion targetRot = Quaternion.LookRotation(dirToTarget, Vector3.up);
                            transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, Time.deltaTime * 24.0f);
                        }
                    }
                }
            }
            else
            {
                if (agent != null) agent.velocity = Vector3.zero;

                if (fightStateTimer < fightStateInterval * 0.7f && animator.GetCurrentAnimatorStateInfo(0).IsTag("Move"))
                {
                    // 前半段敌人后退
                    if (agent != null) agent.Move(-transform.forward * Time.deltaTime * agent.speed);
                    targetMoveSpeed = -1.0f;
                }
                else
                {
                    // 后半段敌人站立
                    targetMoveSpeed = 0.0f;
                }
            }
        }
    }

    /// <summary>
    /// 更新战斗状态的行为, 在战斗范围内则根据攻击欲望判断是否向前移动, 在战斗范围外则向前移动
    /// </summary>
    /// <param name="inFightRange">是否处于战斗范围内</param>
    private void FightStateUpdate(bool inFightRange)
    {
        fightStateTimer += Time.deltaTime;

        if (fightStateTimer > fightStateInterval)
        {
            fightStateTimer = 0;
            if (inFightRange)
            {
                // 攻击欲望判定
                if (Random.Range(0.0f, 1.0f) < attackDesire)
                {
                    moveForward = true;
                }
                else
                {
                    moveForward = false;
                }
            }
            else
            {
                moveForward = true;
            }
        }
    }

    /// <summary>
    /// 攻击动画的事件, 向前移动并进行伤害判定(该事件被直接配置在攻击动画文件中)
    /// </summary>
    private void AttackEvent()
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.Move(transform.forward * 0.01f);
        }
        Debug.Log($"{name} FightAISoldier: AttackEvent invoked, performing AreaDamage (atk={fightAttributes.PhysicalAttack})");
        attackBox.AreaDamage(fightAttributes.PhysicalAttack, true);
    }

    private void PlayAttackSFX()
    {
        attackAudioSource.Stop();
        attackAudioSource.Play();
    }
}
