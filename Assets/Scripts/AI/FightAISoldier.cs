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
    private Rigidbody rb;
    private CharacterController charController;
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
    [SerializeField]
    private float chaseSpeed = 3.5f; // 追击时的移动速度(m/s)
    [SerializeField, Tooltip("Jarak (meter) untuk NavMesh.SamplePosition ketika mencoba warp ke NavMesh pada OnEnable")]
    private float navMeshSampleDistance = 10.0f;
    [SerializeField, Tooltip("Jika true, coba ulang (retry) mencari NavMesh dan warp agent secara berkala ketika agent dinonaktifkan karena tidak berada di NavMesh")]
    private bool autoRetryNavMesh = true;
    [SerializeField, Tooltip("Interval (detik) antara percobaan sample NavMesh saat retry")]
    private float navRetryInterval = 0.5f;
    [SerializeField, Tooltip("Timeout (detik) untuk mencoba retry; 0 atau negatif berarti tanpa batas")]
    private float navRetryTimeout = 0.0f;

    // runtime state for navmesh retry
    private Coroutine navRetryCoroutine = null;
    private bool waitingForNavMesh = false;
    private Vector3 fallbackPatrolDest;
    private bool hasFallbackPatrolDest = false;

    private CharacterAttributes target;
    private string lastTargetName = null;

    protected override void InitFightAI()
    {
        base.InitFightAI();

        if (player == null) player = GameObject.FindGameObjectWithTag("Player").transform;

        // 设置 NavMeshAgent 的速度（如果 agent 可用）
        if (agent != null && agent.enabled)
        {
            agent.speed = chaseSpeed;
        }

        // 间隔产生小范围随机变化
        if (uncertainInterval)
        {
            fightStateInterval += Random.Range(-fightStateInterval / 8, fightStateInterval / 8);
            attackInterval += Random.Range(-attackInterval / 8, attackInterval / 8);
        }

        // Ensure animator does not apply root motion so script-controlled transform movement works
        if (animator != null)
        {
            if (animator.applyRootMotion)
            {
                animator.applyRootMotion = false;
                Debug.Log($"{name} FightAISoldier: Animator.applyRootMotion disabled to allow scripted movement fallback.");
            }
        }
        // cache physics/movement components
        rb = GetComponent<Rigidbody>();
        charController = GetComponent<CharacterController>();
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
            if (!agent.enabled) {
                agent.enabled = true;
                // If we re-enabled the agent, stop any pending navmesh retry attempts
                StopNavRetryCoroutine();
            }

            if (!agent.isOnNavMesh)
            {
                NavMeshHit hit;
                float sampleDist = navMeshSampleDistance;
                if (NavMesh.SamplePosition(transform.position, out hit, sampleDist, NavMesh.AllAreas))
                {
                    agent.Warp(hit.position);
                    Debug.Log($"{name} FightAISoldier: Warped to nearest NavMesh at {hit.position}");
                }
                else
                {
                    Debug.LogWarning($"{name} FightAISoldier: No NavMesh within {sampleDist}m of spawn position. Bake NavMesh or move spawn.");
                    // Disable agent to ensure fallback transform-based movement is used
                    if (agent.enabled)
                    {
                        agent.enabled = false;
                        Debug.Log($"{name} FightAISoldier: NavMeshAgent disabled (no NavMesh) to use fallback movement.");
                    }
                    // start retry coroutine to attempt to reattach to NavMesh later
                    if (autoRetryNavMesh)
                    {
                        waitingForNavMesh = true;
                        if (navRetryCoroutine == null)
                        {
                            navRetryCoroutine = StartCoroutine(NavMeshRetryCoroutine());
                            Debug.Log($"{name} FightAISoldier: started NavMesh retry coroutine (interval={navRetryInterval}s, timeout={navRetryTimeout}s)");
                        }
                    }
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
            // If agent is enabled but not on NavMesh, disable it so fallback movement isn't interfered with
            if (agent.enabled)
            {
                agent.enabled = false;
                Debug.Log($"{name} FightAISoldier: NavMeshAgent disabled during Update (no NavMesh) to use fallback movement.");
                if (autoRetryNavMesh)
                {
                    waitingForNavMesh = true;
                    if (navRetryCoroutine == null)
                    {
                        navRetryCoroutine = StartCoroutine(NavMeshRetryCoroutine());
                        Debug.Log($"{name} FightAISoldier: started NavMesh retry coroutine (interval={navRetryInterval}s, timeout={navRetryTimeout}s) from Update().");
                    }
                }
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

        // Log target changes (less spam): when target name changes, print details
        string currentTargetName = target != null ? target.gameObject.name : "null";
        if (currentTargetName != lastTargetName)
        {
            lastTargetName = currentTargetName;
            string tpos = target != null ? target.transform.position.ToString("F2") : "-";
            Debug.Log($"{name} FightAISoldier TARGET_CHANGED: target={currentTargetName}, targetPos={tpos}, agent.enabled={(agent != null ? agent.enabled : false)}, isOnNavMesh={(agent != null ? agent.isOnNavMesh : false)}, health={fightAttributes.health}");
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
                // Prefer CharacterController or Rigidbody movement if present
                Vector3 before = transform.position;
                if (charController != null)
                {
                    charController.Move(dir.normalized * speed * Time.deltaTime);
                }
                else if (rb != null && !rb.isKinematic)
                {
                    rb.MovePosition(transform.position + dir.normalized * speed * Time.deltaTime);
                }
                else
                {
                    transform.position += dir.normalized * speed * Time.deltaTime;
                }
                Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, Time.deltaTime * 4f);
                targetMoveSpeed = speed;
                if (before == transform.position)
                {
                    Debug.LogWarning($"{name} FightAISoldier: Patrol fallback movement did not change position (blocked?). rb={(rb != null)}, charController={(charController != null)}");
                }
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

        // Debug fallback movement decisions
        if (Time.time - lastDebugLogTime > debugLogCooldown)
        {
            Debug.Log($"{name} FightAISoldier: FallbackAction targetPos={targetPos}, selfPos={transform.position}, dis={dis:F2}, attackDistance={attackDistance}, alarmDistance={alarmDistance}");
            lastDebugLogTime = Time.time;
        }

        // Make sure animator is not applying root motion during scripted fallback movement
        if (animator != null && animator.applyRootMotion)
        {
            animator.applyRootMotion = false;
            Debug.Log($"{name} FightAISoldier: Disabled Animator.applyRootMotion in FallbackAction to allow transform movement.");
        }

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
                // Log movement intent
                Debug.Log($"{name} FightAISoldier: Fallback moving toward target (dir={dir.normalized}, speed={speed}, delta={Time.deltaTime:F3})");
                Vector3 before = transform.position;
                // compute desired move delta for this frame
                Vector3 moveDelta = dir.normalized * speed * Time.deltaTime;

                if (charController != null)
                {
                    // Use capsule cast to test collisions ahead of the CharacterController.
                    float ccHeight = Mathf.Max(0.01f, charController.height);
                    float ccRadius = Mathf.Max(0.01f, charController.radius);
                    Vector3 ccCenter = charController.center;
                    Vector3 worldCenter = transform.TransformPoint(ccCenter);
                    float halfHeight = Mathf.Max(0f, (ccHeight * 0.5f) - ccRadius);
                    Vector3 p0 = worldCenter + Vector3.up * halfHeight;
                    Vector3 p1 = worldCenter - Vector3.up * halfHeight;

                    RaycastHit hitInfo;
                    float checkDist = moveDelta.magnitude + 0.01f;
                    bool blocked = Physics.CapsuleCast(p0, p1, ccRadius, moveDelta.normalized, out hitInfo, checkDist, ~0, QueryTriggerInteraction.Ignore);

                    if (!blocked)
                    {
                        charController.Move(moveDelta);
                    }
                    else
                    {
                        // try sliding along the hit plane
                        Vector3 slide = Vector3.ProjectOnPlane(moveDelta, hitInfo.normal);
                        if (slide.sqrMagnitude > 0.0001f)
                        {
                            // check slide path
                            bool slideBlocked = Physics.CapsuleCast(p0, p1, ccRadius, slide.normalized, out hitInfo, slide.magnitude + 0.01f, ~0, QueryTriggerInteraction.Ignore);
                            if (!slideBlocked)
                                charController.Move(slide);
                            // else remain blocked this frame
                        }
                    }
                }
                else if (rb != null && !rb.isKinematic)
                {
                    // Use Rigidbody.SweepTest to detect collisions before moving
                    RaycastHit hitInfo;
                    if (!rb.SweepTest(moveDelta.normalized, out hitInfo, moveDelta.magnitude + 0.01f))
                    {
                        rb.MovePosition(transform.position + moveDelta);
                    }
                    else
                    {
                        // try slide
                        Vector3 slide = Vector3.ProjectOnPlane(moveDelta, hitInfo.normal);
                        if (slide.sqrMagnitude > 0.0001f && !rb.SweepTest(slide.normalized, out hitInfo, slide.magnitude + 0.01f))
                        {
                            rb.MovePosition(transform.position + slide);
                        }
                    }
                }
                else
                {
                    transform.position += moveDelta;
                }
                Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, Time.deltaTime * 12.0f);
                targetMoveSpeed = speed;
                if (before == transform.position)
                {
                    Debug.LogWarning($"{name} FightAISoldier: Fallback movement attempt did not change position (blocked). rb={(rb != null)}, charController={(charController != null)}, animator.applyRootMotion={(animator != null ? animator.applyRootMotion : false)}");
                }
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

        if (agent != null && agent.enabled)
        {
            agent.enabled = true;
            agent.speed = chaseSpeed;
            // If we reset AI and agent is enabled, cancel any navmesh retry attempts
            StopNavRetryCoroutine();
        }

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

            if (agent != null && agent.enabled)
            {
                agent.destination = targetPos;
                agent.speed = chaseSpeed;  // 设置追击速度
            }
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
                else
                {
                    // 追击过程中也朝向目标
                    Vector3 dirToTarget = targetPos - transform.position;
                    dirToTarget.y = 0;
                    if (dirToTarget.sqrMagnitude > 0.0001f)
                    {
                        Quaternion targetRot = Quaternion.LookRotation(dirToTarget, Vector3.up);
                        transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, Time.deltaTime * 8.0f);
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

    private void OnDisable()
    {
        StopNavRetryCoroutine();
    }

    private void StopNavRetryCoroutine()
    {
        waitingForNavMesh = false;
        if (navRetryCoroutine != null)
        {
            try { StopCoroutine(navRetryCoroutine); } catch { }
            navRetryCoroutine = null;
        }
    }

    private IEnumerator NavMeshRetryCoroutine()
    {
        float startTime = Time.time;
        while (true)
        {
            // stop if agent became null or was re-enabled
            if (agent == null)
            {
                navRetryCoroutine = null;
                waitingForNavMesh = false;
                yield break;
            }

            if (!agent.enabled)
            {
                NavMeshHit hit;
                if (NavMesh.SamplePosition(transform.position, out hit, navMeshSampleDistance, NavMesh.AllAreas))
                {
                    // found navmesh; enable agent and warp onto it
                    agent.enabled = true;
                    try { agent.Warp(hit.position); } catch { }
                    Debug.Log($"{name} FightAISoldier: NavMesh found during retry at {hit.position} - agent re-enabled and warped.");
                    waitingForNavMesh = false;
                    navRetryCoroutine = null;
                    yield break;
                }
            }
            else
            {
                // agent was enabled externally; stop retry
                waitingForNavMesh = false;
                navRetryCoroutine = null;
                yield break;
            }

            if (navRetryTimeout > 0f && Time.time - startTime >= navRetryTimeout)
            {
                Debug.LogWarning($"{name} FightAISoldier: NavMesh retry timed out after {navRetryTimeout} seconds.");
                waitingForNavMesh = false;
                navRetryCoroutine = null;
                yield break;
            }

            yield return new WaitForSeconds(navRetryInterval);
        }
    }
}
