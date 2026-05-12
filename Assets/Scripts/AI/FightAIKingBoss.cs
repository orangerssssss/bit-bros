using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 最终 Boss 专用战斗 AI。
/// 只保留稳定的追击、转向、攻击、受击僵直逻辑，避免复杂状态切换导致动画鬼畜。
/// </summary>
public class FightAIKingBoss : FightAI
{
    private const string DefaultPhase2EnemyPrefabPath = "Assets/Prefabs/Characters/Enemy_Soldier.prefab";
    private const string DefaultPhase2FireballPrefabPath = "Assets/AssetPackages/PolygonParticleFX/Prefabs/FX_Fireball_01.prefab";
    private const string DefaultPhase2FireballMaterialPath = "Assets/AssetPackages/PolygonParticleFX/Materials/PolygonParticleFX_Emissive_Fire.mat";
    private const string DefaultPhase2WarningMaterialPath = "Assets/AssetPackages/PolygonParticleFX/Materials/PolygonParticleFX_Ring_01.mat";
    private const int WarningCircleSegmentCount = 64;

    public event System.Action<FightAIKingBoss> Phase2MinionWaveCleared;

    public enum BossPhase
    {
        Phase1,
        Phase2
    }

    private static Transform cachedPlayer;

    [Header("感知与移动")]
    [SerializeField] private float alarmDistance = 24.0f;
    [SerializeField] private float leashDistance = 36.0f;
    [SerializeField] private float meleeDistance = 3.2f;
    [SerializeField] private float stopBuffer = 0.25f;
    [SerializeField] private float chaseSpeed = 5.8f;
    [SerializeField] private float turnSpeed = 12.0f;
    [SerializeField] private float navMeshSampleDistance = 10.0f;

    [Header("攻击节奏")]
    [SerializeField] private float baseAttackCooldown = 1.9f;
    [SerializeField] private float attackCommitTime = 1.45f;
    [SerializeField] private float blockedStaggerDuration = 0.8f;
    [SerializeField, Range(1f, 2f)] private float animationSpeedMultiplier = 0.82f;

    [Header("攻击配置")]
    [SerializeField] private BoxAttackArea attackBox;
    [SerializeField] private AudioSource attackAudioSource;
    [SerializeField, Range(0.8f, 2f)] private float damageMultiplier = 1.2f;

    [Header("二阶段预留")]
    [SerializeField] private bool enablePhase2 = true;
    [SerializeField, Range(0.1f, 0.95f)] private float phase2HealthRatio = 0.45f;
    [SerializeField] private Transform phase2HoverPoint;
    [SerializeField] private float phase2HoverHeight = 6.0f;
    [SerializeField] private float phase2LiftDuration = 1.2f;
    [SerializeField] private float phase2LandDuration = 1.0f;
    [SerializeField] private GameObject phase2EnemyPrefab;
    [SerializeField] private Transform[] phase2EnemySpawnPoints;
    [SerializeField] private bool phase2LockFacingWhileHovering = true;
    [SerializeField] private float phase2GroundSnapDistance = 12.0f;
    [SerializeField] private float phase2SpawnWarningDuration = 1.1f;
    [SerializeField] private float phase2WarningRadius = 1.5f;
    [SerializeField] private Color phase2WarningColor = new Color(0.95f, 0.55f, 0.12f, 0.72f);
    [SerializeField] private float phase2MoveSpeedBonus = 1.05f;
    [SerializeField] private float phase2AttackCooldownMultiplier = 0.9f;
    [SerializeField] private float phase2DamageMultiplier = 1.15f;
    [SerializeField] private float phase2MinionDamageMultiplier = 0.55f;
    [SerializeField] private float phase2MinionAttackIntervalMultiplier = 1.35f;
    [SerializeField] private float phase2FireballInterval = 2.4f;
    [SerializeField] private float phase2FireballWarningDuration = 1.0f;
    [SerializeField] private float phase2FireballRadius = 1.2f;
    [SerializeField] private float phase2FireballSpawnHeight = 9.0f;
    [SerializeField] private float phase2FireballFallSpeed = 10.0f;
    [SerializeField, Tooltip("Phase 2 fireball damage")]
    private int phase2FireballDamage = 480;
    [SerializeField] private float phase2FireballRandomOffset = 2.5f;
    [SerializeField] private int phase2FireballVolleyCount = 3;
    [SerializeField] private bool waitForStorySignalBeforeLanding = false;
    [SerializeField] private GameObject phase2FireballVisualPrefab;
    [SerializeField] private Material phase2FireballMaterial;
    [SerializeField] private Material phase2WarningMaterial;
    [SerializeField] private float phase2WarningRingWidth = 0.18f;

    private CharacterAttributes target;
    private Vector3 startPosition;
    private float attackCooldownTimer;
    private float attackCommitTimer;
    private float staggerTimer;
    private bool damageResolvedThisSwing;
    private float smoothedMoveSpeed;
    private BossPhase currentPhase = BossPhase.Phase1;
    private bool phase2Triggered;
    private bool phase2SequenceRunning;
    private bool phase2Airborne;
    private bool phase2MinionWaveActive;
    private Vector3 groundedPosition;
    private Quaternion groundedRotation;
    private readonly List<GameObject> phase2SpawnedEnemies = new List<GameObject>();
    private readonly List<GameObject> phase2WarningMarkers = new List<GameObject>();
    private Quaternion hoverLockedRotation;
    private Coroutine phase2FireballCoroutine;
    private bool waitingForLandingSignal;
    private bool phase2FireballActive;

    protected override void InitFightAI()
    {
        base.InitFightAI();

        startPosition = transform.position;
        groundedPosition = transform.position;
        groundedRotation = transform.rotation;
        hoverLockedRotation = transform.rotation;
        NormalizeBossTuning();
        RestorePhase2VisualDefaultsInEditor();

        if (cachedPlayer == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) cachedPlayer = player.transform;
        }

        if (animator != null)
        {
            animator.applyRootMotion = false;
            animator.speed = animationSpeedMultiplier;
            SetAnimatorBoolIfExists("Alarm", false);
            SetAnimatorFloatIfExists("MoveSpeed", 0f);
        }

        if (agent != null)
        {
            agent.updatePosition = true;
            agent.updateRotation = true;
            agent.speed = chaseSpeed;
            agent.angularSpeed = 720f;
            agent.acceleration = 80f;
            agent.stoppingDistance = Mathf.Max(0.1f, meleeDistance - stopBuffer);
        }

        if (attackBox == null)
        {
            attackBox = GetComponentInChildren<BoxAttackArea>(true);
        }

        if (attackBox != null && fightAttributes != null)
        {
            attackBox.combatCamp = fightAttributes.combatCamp;
        }

        if (attackAudioSource == null)
        {
            attackAudioSource = GetComponentInChildren<AudioSource>(true);
        }

        SpawnPortalOnDeath bossPortalSpawner = GetComponent<SpawnPortalOnDeath>();
        if (bossPortalSpawner == null) bossPortalSpawner = GetComponentInChildren<SpawnPortalOnDeath>(true);
        if (bossPortalSpawner != null)
        {
            bossPortalSpawner.enabled = false;
        }
    }

    private void OnEnable()
    {
        startPosition = transform.position;
        ResetFightAI();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        NormalizeBossTuning();
        RestorePhase2VisualDefaultsInEditor();
        phase2WarningRingWidth = Mathf.Clamp(phase2WarningRingWidth, 0.04f, 0.6f);
    }
#endif

    private void Update()
    {
        if (isDead || animator == null || fightAttributes == null)
        {
            return;
        }

        if (phase2SequenceRunning)
        {
            StopMotion();
            UpdateMoveSpeed(0f);
            return;
        }

        attackCooldownTimer += Time.deltaTime;

        if (ShouldEnterPhase2())
        {
            StartCoroutine(Phase2Sequence());
            return;
        }

        if (staggerTimer > 0f)
        {
            staggerTimer -= Time.deltaTime;
            StopMotion();
            SetAnimatorBoolIfExists("Alarm", target != null);
            UpdateMoveSpeed(0f);
            return;
        }

        AcquireTarget();
        bool hasValidTarget = target != null && target.health > 0;
        SetAnimatorBoolIfExists("Alarm", hasValidTarget);

        if (!hasValidTarget)
        {
            ReturnToAnchor();
            return;
        }

        float distance = Vector3.Distance(transform.position, target.transform.position);
        if (distance > leashDistance)
        {
            target = null;
            ReturnToAnchor();
            return;
        }

        FaceTarget(target.transform.position, turnSpeed);

        if (attackLocked || attackCommitTimer > 0f || IsInAttackAnimation())
        {
            TickAttackLock();
            return;
        }

        if (distance <= meleeDistance && attackCooldownTimer >= baseAttackCooldown)
        {
            BeginMeleeAttack();
            return;
        }

        if (distance <= alarmDistance)
        {
            ChaseTarget(target.transform.position);
        }
        else
        {
            ReturnToAnchor();
        }
    }

    public override void GetDamageReact()
    {
        base.GetDamageReact();

        if (!isDead && animator != null && !IsInAttackAnimation())
        {
            animator.SetTrigger("Impact");
        }
    }

    public override void OnShieldBlocked(float staggerDuration)
    {
        staggerTimer = Mathf.Max(staggerTimer, Mathf.Max(blockedStaggerDuration, staggerDuration));
        attackCommitTimer = 0f;
        damageResolvedThisSwing = true;
        EndAttackLock();
        StopMotion();
        UpdateMoveSpeed(0f);
    }

    public override void ResetFightAI()
    {
        isDead = false;
        target = null;
        attackCooldownTimer = baseAttackCooldown;
        attackCommitTimer = 0f;
        staggerTimer = 0f;
        damageResolvedThisSwing = false;
        smoothedMoveSpeed = 0f;
        currentPhase = BossPhase.Phase1;
        phase2Triggered = false;
        phase2SequenceRunning = false;
        phase2Airborne = false;
        phase2MinionWaveActive = false;
        phase2FireballActive = false;
        waitingForLandingSignal = false;
        phase2SpawnedEnemies.Clear();
        ClearPhase2WarningMarkers();
        if (phase2FireballCoroutine != null)
        {
            StopCoroutine(phase2FireballCoroutine);
            phase2FireballCoroutine = null;
        }

        if (agent != null)
        {
            EnsureAgentReady();
        }

        if (enemyCollider != null)
        {
            enemyCollider.enabled = true;
        }

        if (animator != null)
        {
            animator.applyRootMotion = false;
            animator.speed = animationSpeedMultiplier;
            SetAnimatorBoolIfExists("Alarm", false);
            SetAnimatorFloatIfExists("MoveSpeed", 0f);
        }
    }

    public void AttackEvent()
    {
        attackEventFired = true;
        ResolveAttackDamage();
    }

    public void PlayAttackSFX()
    {
        if (attackAudioSource == null)
        {
            return;
        }

        attackAudioSource.Stop();
        attackAudioSource.Play();
    }

    public override void DiedReact()
    {
        attackCommitTimer = 0f;
        damageResolvedThisSwing = true;
        base.DiedReact();
    }

    private void NormalizeBossTuning()
    {
        meleeDistance = Mathf.Clamp(meleeDistance, 2.2f, 3.6f);
        alarmDistance = Mathf.Max(alarmDistance, meleeDistance + 8f);
        leashDistance = Mathf.Max(leashDistance, alarmDistance + 6f);
        chaseSpeed = Mathf.Max(chaseSpeed, 5.8f);
        turnSpeed = Mathf.Max(turnSpeed, 12.0f);
        baseAttackCooldown = Mathf.Clamp(baseAttackCooldown, 1.7f, 2.2f);
        attackCommitTime = Mathf.Clamp(attackCommitTime, 1.25f, 1.65f);
        attackFallbackDelaySeconds = Mathf.Clamp(attackFallbackDelaySeconds, 0.45f, attackCommitTime);
        animationSpeedMultiplier = Mathf.Clamp(animationSpeedMultiplier, 0.75f, 0.9f);
        stopBuffer = Mathf.Clamp(stopBuffer, 0.1f, 1.2f);
        damageMultiplier = Mathf.Clamp(damageMultiplier, 0.8f, 1.2f);
        phase2HealthRatio = Mathf.Clamp(phase2HealthRatio, 0.1f, 0.95f);
    }

    private void AcquireTarget()
    {
        if (cachedPlayer == null || !cachedPlayer.gameObject.activeInHierarchy)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                cachedPlayer = player.transform;
            }
        }

        if (cachedPlayer == null)
        {
            target = null;
            return;
        }

        CharacterAttributes playerAttributes = cachedPlayer.GetComponent<CharacterAttributes>();
        if (playerAttributes == null)
        {
            playerAttributes = cachedPlayer.GetComponentInParent<CharacterAttributes>();
        }
        if (playerAttributes == null)
        {
            playerAttributes = cachedPlayer.GetComponentInChildren<CharacterAttributes>();
        }

        if (playerAttributes != null && playerAttributes.health > 0)
        {
            float distance = Vector3.Distance(transform.position, cachedPlayer.position);
            target = distance <= alarmDistance ? playerAttributes : null;
        }
        else
        {
            target = null;
        }
    }

    private void ChaseTarget(Vector3 targetPosition)
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.speed = chaseSpeed;
            agent.SetDestination(targetPosition);
            UpdateMoveSpeed(agent.velocity.magnitude / Mathf.Max(0.01f, chaseSpeed));
        }
        else
        {
            Vector3 flatDir = targetPosition - transform.position;
            flatDir.y = 0f;
            if (flatDir.sqrMagnitude > 0.01f)
            {
                transform.position += flatDir.normalized * chaseSpeed * Time.deltaTime;
            }
            UpdateMoveSpeed(1f);
        }
    }

    private void ReturnToAnchor()
    {
        Vector3 flatDelta = startPosition - transform.position;
        flatDelta.y = 0f;

        if (flatDelta.sqrMagnitude <= 0.2f * 0.2f)
        {
            StopMotion();
            UpdateMoveSpeed(0f);
            return;
        }

        FaceTarget(startPosition, turnSpeed * 0.85f);

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.speed = chaseSpeed * 0.8f;
            agent.SetDestination(startPosition);
            UpdateMoveSpeed(Mathf.Clamp01(agent.velocity.magnitude / Mathf.Max(0.01f, chaseSpeed)));
        }
        else
        {
            transform.position += flatDelta.normalized * chaseSpeed * 0.8f * Time.deltaTime;
            UpdateMoveSpeed(0.7f);
        }
    }

    private void BeginMeleeAttack()
    {
        StopMotion();
        FaceTarget(target.transform.position, turnSpeed * 1.5f);

        attackCooldownTimer = 0f;
        attackCommitTimer = attackCommitTime;
        damageResolvedThisSwing = false;
        attackEventFired = false;

        TriggerAttackParameter();
        ScheduleAttackFallback(attackFallbackDelaySeconds, ResolveAttackDamage);
        PlayAttackSFX();
        UpdateMoveSpeed(0f);
    }

    private void TickAttackLock()
    {
        if (attackCommitTimer > 0f)
        {
            attackCommitTimer -= Time.deltaTime;
        }

        StopMotion();
        UpdateMoveSpeed(0f);

        if (target != null)
        {
            FaceTarget(target.transform.position, turnSpeed * 1.3f);
        }

        if (attackCommitTimer <= 0f && !attackLocked && !IsInAttackAnimation())
        {
        }
    }

    private void ResolveAttackDamage()
    {
        if (damageResolvedThisSwing)
        {
            return;
        }

        damageResolvedThisSwing = true;

        if (attackBox == null || fightAttributes == null)
        {
            return;
        }

        float phaseDamageMultiplier = currentPhase == BossPhase.Phase2 ? phase2DamageMultiplier : 1f;
        int damage = Mathf.Max(1, Mathf.RoundToInt(fightAttributes.PhysicalAttack * damageMultiplier * phaseDamageMultiplier));
        bool hitTarget = attackBox.AreaDamage(damage, true);

        if (!hitTarget && target != null)
        {
            Vector3 flatDelta = target.transform.position - transform.position;
            flatDelta.y = 0f;
            if (flatDelta.magnitude <= meleeDistance + 0.2f)
            {
                target.GetAttack(damage, true);
            }
        }
    }

    private bool ShouldEnterPhase2()
    {
        if (!enablePhase2 || phase2Triggered || phase2SequenceRunning || fightAttributes == null || fightAttributes.MaxHealth <= 0)
        {
            return false;
        }

        return fightAttributes.health > 0
            && (float)fightAttributes.health / fightAttributes.MaxHealth <= phase2HealthRatio;
    }

    private void EnterPhase2()
    {
        phase2Triggered = true;
        currentPhase = BossPhase.Phase2;

        chaseSpeed *= phase2MoveSpeedBonus;
        baseAttackCooldown *= phase2AttackCooldownMultiplier;
        attackCooldownTimer = Mathf.Min(attackCooldownTimer, baseAttackCooldown);

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.speed = chaseSpeed;
        }

        if (animator != null && AnimatorHasParameter("Impact"))
        {
            animator.SetTrigger("Impact");
        }
    }

    private IEnumerator Phase2Sequence()
    {
        phase2SequenceRunning = true;
        phase2MinionWaveActive = true;
        phase2FireballActive = true;
        EnterPhase2();
        StopMotion();
        attackCommitTimer = 0f;
        damageResolvedThisSwing = true;

        groundedPosition = transform.position;
        groundedRotation = transform.rotation;
        hoverLockedRotation = transform.rotation;

        if (enemyCollider != null)
        {
            enemyCollider.enabled = false;
        }

        if (agent != null && agent.enabled)
        {
            agent.enabled = false;
        }

        yield return MoveBossToHoverPosition();

        SpawnPhase2Warnings();
        yield return new WaitForSeconds(phase2SpawnWarningDuration);

        SpawnPhase2Enemies();
        if (phase2FireballCoroutine != null)
        {
            StopCoroutine(phase2FireballCoroutine);
        }
        phase2FireballCoroutine = StartCoroutine(Phase2FireballRoutine());

        while (HasAlivePhase2Enemies())
        {
            KeepBossHovering();
            yield return null;
        }

        phase2MinionWaveActive = false;
        if (waitForStorySignalBeforeLanding)
        {
            waitingForLandingSignal = true;
            Phase2MinionWaveCleared?.Invoke(this);
            while (waitingForLandingSignal && !isDead)
            {
                KeepBossHovering();
                yield return null;
            }
        }

        yield return LandFromPhase2();

        if (enemyCollider != null)
        {
            enemyCollider.enabled = true;
        }

        EnsureAgentReady();
        phase2Airborne = false;
        phase2SequenceRunning = false;
    }

    private IEnumerator MoveBossToHoverPosition()
    {
        phase2Airborne = true;
        Vector3 origin = transform.position;
        Quaternion originRotation = transform.rotation;
        Vector3 targetPos = phase2HoverPoint != null
            ? phase2HoverPoint.position
            : groundedPosition + Vector3.up * phase2HoverHeight;
        Quaternion targetRot = phase2LockFacingWhileHovering ? groundedRotation : (phase2HoverPoint != null ? phase2HoverPoint.rotation : originRotation);

        float duration = Mathf.Max(0.01f, phase2LiftDuration);
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            transform.position = Vector3.Lerp(origin, targetPos, eased);
            transform.rotation = Quaternion.Slerp(originRotation, targetRot, eased);
            UpdateMoveSpeed(0f);
            yield return null;
        }

        transform.position = targetPos;
        transform.rotation = targetRot;
        hoverLockedRotation = targetRot;
        UpdateMoveSpeed(0f);
    }

    private IEnumerator LandFromPhase2()
    {
        Vector3 origin = transform.position;
        Quaternion originRotation = transform.rotation;
        float duration = Mathf.Max(0.01f, phase2LandDuration);
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            transform.position = Vector3.Lerp(origin, groundedPosition, eased);
            transform.rotation = Quaternion.Slerp(originRotation, groundedRotation, eased);
            UpdateMoveSpeed(0f);
            yield return null;
        }

        transform.position = groundedPosition;
        transform.rotation = groundedRotation;
        UpdateMoveSpeed(0f);
    }

    private void KeepBossHovering()
    {
        if (!phase2Airborne)
        {
            return;
        }

        Vector3 hoverPos = phase2HoverPoint != null
            ? phase2HoverPoint.position
            : groundedPosition + Vector3.up * phase2HoverHeight;
        transform.position = hoverPos;
        if (phase2LockFacingWhileHovering)
        {
            transform.rotation = hoverLockedRotation;
        }
        UpdateMoveSpeed(0f);
    }

    private void SpawnPhase2Warnings()
    {
        ClearPhase2WarningMarkers();

        if (phase2EnemySpawnPoints == null)
        {
            return;
        }

        foreach (Transform spawnPoint in phase2EnemySpawnPoints)
        {
            if (spawnPoint == null) continue;
            phase2WarningMarkers.Add(CreateWarningMarker(spawnPoint.position, phase2WarningRadius));
        }
    }

    private void SpawnPhase2Enemies()
    {
        ClearPhase2WarningMarkers();
        phase2SpawnedEnemies.Clear();

        GameObject spawnPrefab = ResolvePhase2EnemyPrefab();
        if (spawnPrefab == null || phase2EnemySpawnPoints == null)
        {
            Debug.LogWarning($"{name} FightAIKingBoss: phase2EnemyPrefab or phase2EnemySpawnPoints is missing. Phase2 wave cannot spawn.");
            return;
        }

        foreach (Transform spawnPoint in phase2EnemySpawnPoints)
        {
            if (spawnPoint == null) continue;

            Vector3 spawnPosition = ResolveGroundedSpawnPosition(spawnPoint.position);
            GameObject enemy = Instantiate(spawnPrefab, spawnPosition, spawnPoint.rotation);
            if (!enemy.activeSelf)
            {
                enemy.SetActive(true);
            }
            PreparePhase2Enemy(enemy);
            phase2SpawnedEnemies.Add(enemy);
        }

        Debug.Log($"{name} FightAIKingBoss: spawned phase2 enemies count = {phase2SpawnedEnemies.Count}");
    }

    private GameObject ResolvePhase2EnemyPrefab()
    {
        if (phase2EnemyPrefab != null)
        {
            return phase2EnemyPrefab;
        }

#if UNITY_EDITOR
        GameObject fallbackPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultPhase2EnemyPrefabPath);
        if (fallbackPrefab != null)
        {
            phase2EnemyPrefab = fallbackPrefab;
            Debug.LogWarning($"{name} FightAIKingBoss: phase2EnemyPrefab was missing, auto-restored from {DefaultPhase2EnemyPrefabPath}.");
            return phase2EnemyPrefab;
        }
#endif

        return null;
    }

    private void PreparePhase2Enemy(GameObject enemy)
    {
        if (enemy == null)
        {
            return;
        }

        FightAI enemyAi = enemy.GetComponent<FightAI>();
        if (enemyAi == null) enemyAi = enemy.GetComponentInChildren<FightAI>();
        if (enemyAi != null)
        {
            enemyAi.summonPoint = null;
        }

        FightAttributes enemyAttributes = enemy.GetComponent<FightAttributes>();
        if (enemyAttributes == null) enemyAttributes = enemy.GetComponentInChildren<FightAttributes>();
        if (enemyAttributes != null)
        {
            enemyAttributes.combatCamp = CombatCamp.Enemy;
            enemyAttributes.InitAttributes();
        }

        SpawnPortalOnDeath portalSpawner = enemy.GetComponent<SpawnPortalOnDeath>();
        if (portalSpawner == null) portalSpawner = enemy.GetComponentInChildren<SpawnPortalOnDeath>();
        if (portalSpawner != null)
        {
            portalSpawner.enabled = false;
        }

        if (enemyAi != null)
        {
            FightAISoldier soldierAi = enemyAi as FightAISoldier;
            if (soldierAi != null)
            {
                soldierAi.ApplyFinalSceneMinionTuning(phase2MinionDamageMultiplier, phase2MinionAttackIntervalMultiplier);
            }
            enemyAi.ResetFightAI();
        }
    }

    private IEnumerator Phase2FireballRoutine()
    {
        while (phase2FireballActive && !isDead && currentPhase == BossPhase.Phase2)
        {
            if (target != null)
            {
                List<Vector3> volleyPositions = new List<Vector3>();
                int volleyCount = Mathf.Max(1, phase2FireballVolleyCount);
                for (int i = 0; i < volleyCount; i++)
                {
                    Vector3 targetPosition = target.transform.position;
                    Vector2 randomOffset = Random.insideUnitCircle * phase2FireballRandomOffset;
                    targetPosition.x += randomOffset.x;
                    targetPosition.z += randomOffset.y;
                    volleyPositions.Add(ResolveGroundedSpawnPosition(targetPosition));
                }

                List<GameObject> warningMarkers = new List<GameObject>();
                for (int i = 0; i < volleyPositions.Count; i++)
                {
                    warningMarkers.Add(CreateWarningMarker(volleyPositions[i], phase2FireballRadius));
                }

                yield return new WaitForSeconds(phase2FireballWarningDuration);

                for (int i = 0; i < warningMarkers.Count; i++)
                {
                    if (warningMarkers[i] != null)
                    {
                        Destroy(warningMarkers[i]);
                    }
                }

                for (int i = 0; i < volleyPositions.Count; i++)
                {
                    SpawnFireball(volleyPositions[i]);
                }
            }

            float waitTime = Mathf.Max(0.4f, phase2FireballInterval);
            float timer = 0f;
            while (timer < waitTime && phase2FireballActive && !isDead && currentPhase == BossPhase.Phase2)
            {
                timer += Time.deltaTime;
                yield return null;
            }
        }

        phase2FireballCoroutine = null;
    }

    private void SpawnFireball(Vector3 groundPosition)
    {
        GameObject fireballPrefab = ResolvePhase2FireballVisualPrefab();
        GameObject fireball = fireballPrefab != null
            ? Instantiate(fireballPrefab)
            : GameObject.CreatePrimitive(PrimitiveType.Sphere);
        fireball.name = "BossPhase2Fireball";
        fireball.transform.position = groundPosition + Vector3.up * phase2FireballSpawnHeight;
        fireball.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);

        Collider collider = fireball.GetComponent<Collider>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }

        Material fireballMaterial = ResolvePhase2FireballMaterial();
        if (fireballMaterial != null)
        {
            ApplyMaterialToRenderers(fireball, fireballMaterial);
        }
        else
        {
            Renderer renderer = fireball.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material material = new Material(Shader.Find("Standard"));
                material.color = new Color(0.18f, 0.06f, 0.04f, 1f);
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", new Color(1.3f, 0.42f, 0.08f) * 2.0f);
                material.SetFloat("_Glossiness", 0.1f);
                renderer.material = material;
            }
        }

        BossPhase2Fireball fireballLogic = fireball.AddComponent<BossPhase2Fireball>();
        fireballLogic.Init(Mathf.Max(1, phase2FireballDamage), phase2FireballFallSpeed, phase2FireballRadius, groundPosition.y + 0.1f);
    }

    private Vector3 ResolveGroundedSpawnPosition(Vector3 desiredPosition)
    {
        NavMeshHit navHit;
        if (NavMesh.SamplePosition(desiredPosition, out navHit, phase2GroundSnapDistance, NavMesh.AllAreas))
        {
            return navHit.position;
        }

        RaycastHit rayHit;
        if (Physics.Raycast(desiredPosition + Vector3.up * phase2GroundSnapDistance * 0.5f, Vector3.down, out rayHit, phase2GroundSnapDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            return rayHit.point;
        }

        return desiredPosition;
    }

    private bool HasAlivePhase2Enemies()
    {
        for (int i = phase2SpawnedEnemies.Count - 1; i >= 0; i--)
        {
            GameObject enemy = phase2SpawnedEnemies[i];
            if (enemy == null)
            {
                phase2SpawnedEnemies.RemoveAt(i);
                continue;
            }

            CharacterAttributes attributes = enemy.GetComponent<CharacterAttributes>();
            if (attributes == null) attributes = enemy.GetComponentInChildren<CharacterAttributes>();
            if (attributes != null && attributes.health > 0 && enemy.activeInHierarchy)
            {
                return true;
            }
        }

        return false;
    }

    private GameObject CreateWarningMarker(Vector3 worldPosition, float radius)
    {
        GameObject marker = new GameObject("BossPhase2Warning");
        marker.transform.position = worldPosition + Vector3.up * 0.03f;
        marker.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        LineRenderer lineRenderer = marker.AddComponent<LineRenderer>();
        lineRenderer.loop = true;
        lineRenderer.useWorldSpace = false;
        lineRenderer.alignment = LineAlignment.TransformZ;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        lineRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        lineRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        lineRenderer.positionCount = WarningCircleSegmentCount;
        lineRenderer.widthMultiplier = Mathf.Max(0.04f, phase2WarningRingWidth);
        lineRenderer.numCornerVertices = 4;
        lineRenderer.numCapVertices = 4;
        lineRenderer.textureMode = LineTextureMode.Tile;
        lineRenderer.startColor = phase2WarningColor;
        lineRenderer.endColor = phase2WarningColor;

        float clampedRadius = Mathf.Max(0.1f, radius);
        for (int i = 0; i < WarningCircleSegmentCount; i++)
        {
            float angle = (Mathf.PI * 2f * i) / WarningCircleSegmentCount;
            lineRenderer.SetPosition(i, new Vector3(Mathf.Cos(angle) * clampedRadius, Mathf.Sin(angle) * clampedRadius, 0f));
        }

        Material warningMaterial = ResolvePhase2WarningMaterial();
        if (warningMaterial != null)
        {
            lineRenderer.material = warningMaterial;
        }
        else
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                Material material = new Material(shader);
                material.color = phase2WarningColor;
                lineRenderer.material = material;
            }
        }

        if (lineRenderer.material != null)
        {
            lineRenderer.material.color = phase2WarningColor;
        }

        return marker;
    }

    private GameObject ResolvePhase2FireballVisualPrefab()
    {
        if (phase2FireballVisualPrefab != null)
        {
            return phase2FireballVisualPrefab;
        }

#if UNITY_EDITOR
        phase2FireballVisualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultPhase2FireballPrefabPath);
#endif

        return phase2FireballVisualPrefab;
    }

    private Material ResolvePhase2FireballMaterial()
    {
        if (phase2FireballMaterial != null)
        {
            return phase2FireballMaterial;
        }

#if UNITY_EDITOR
        phase2FireballMaterial = AssetDatabase.LoadAssetAtPath<Material>(DefaultPhase2FireballMaterialPath);
#endif

        return phase2FireballMaterial;
    }

    private Material ResolvePhase2WarningMaterial()
    {
        if (phase2WarningMaterial != null)
        {
            return phase2WarningMaterial;
        }

#if UNITY_EDITOR
        phase2WarningMaterial = AssetDatabase.LoadAssetAtPath<Material>(DefaultPhase2WarningMaterialPath);
#endif

        return phase2WarningMaterial;
    }

    private void ApplyMaterialToRenderers(GameObject root, Material material)
    {
        if (root == null || material == null)
        {
            return;
        }

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].material = material;
        }
    }

    private void RestorePhase2VisualDefaultsInEditor()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            ResolvePhase2FireballVisualPrefab();
            ResolvePhase2FireballMaterial();
            ResolvePhase2WarningMaterial();
        }
#endif
    }

    private void ClearPhase2WarningMarkers()
    {
        for (int i = 0; i < phase2WarningMarkers.Count; i++)
        {
            if (phase2WarningMarkers[i] != null)
            {
                Destroy(phase2WarningMarkers[i]);
            }
        }
        phase2WarningMarkers.Clear();
    }

    public void ForcePhase2()
    {
        if (!phase2Triggered)
        {
            EnterPhase2();
        }
    }

    public BossPhase GetCurrentPhase()
    {
        return currentPhase;
    }

    public void ResumeFromPhase2StoryHold()
    {
        waitingForLandingSignal = false;
    }

    public bool IsPhase2Airborne()
    {
        return phase2Airborne;
    }

    public bool IsPhase2MinionWaveActive()
    {
        return phase2MinionWaveActive;
    }

    public void SetWaitForStorySignalBeforeLanding(bool value)
    {
        waitForStorySignalBeforeLanding = value;
        if (!value)
        {
            waitingForLandingSignal = false;
        }
    }

    private bool IsInAttackAnimation()
    {
        if (animator == null)
        {
            return false;
        }

        AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);
        AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(0);
        return current.IsTag("Attack")
            || next.IsTag("Attack")
            || current.IsName("Attack")
            || next.IsName("Attack")
            || current.IsName("Slash")
            || next.IsName("Slash");
    }

    private void StopMotion()
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }
    }

    private void FaceTarget(Vector3 position, float speed)
    {
        Vector3 direction = position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * speed);
    }

    private void UpdateMoveSpeed(float normalizedSpeed)
    {
        smoothedMoveSpeed = Mathf.Lerp(smoothedMoveSpeed, Mathf.Clamp01(normalizedSpeed), Time.deltaTime * 8f);
        SetAnimatorFloatIfExists("MoveSpeed", smoothedMoveSpeed);
    }

    private void SetAnimatorFloatIfExists(string parameterName, float value)
    {
        if (animator != null && AnimatorHasParameter(parameterName))
        {
            animator.SetFloat(parameterName, value);
        }
    }

    private void SetAnimatorBoolIfExists(string parameterName, bool value)
    {
        if (animator != null && AnimatorHasParameter(parameterName))
        {
            animator.SetBool(parameterName, value);
        }
    }

    private void EnsureAgentReady()
    {
        if (agent == null)
        {
            return;
        }

        Vector3 desiredPosition = transform.position;
        NavMeshHit hit;
        bool hasNavMesh = NavMesh.SamplePosition(desiredPosition, out hit, navMeshSampleDistance, NavMesh.AllAreas);
        if (!hasNavMesh)
        {
            hasNavMesh = NavMesh.SamplePosition(groundedPosition, out hit, navMeshSampleDistance, NavMesh.AllAreas);
        }

        if (!hasNavMesh)
        {
            Vector3 groundedFallback = ResolveGroundedSpawnPosition(groundedPosition);
            hasNavMesh = NavMesh.SamplePosition(groundedFallback, out hit, navMeshSampleDistance, NavMesh.AllAreas);
        }

        if (!hasNavMesh)
        {
            if (agent.enabled)
            {
                agent.enabled = false;
            }
            Debug.LogWarning($"{name} FightAIKingBoss: no NavMesh found near boss spawn, keeping NavMeshAgent disabled.");
            return;
        }

        transform.position = hit.position;
        groundedPosition = hit.position;

        if (!agent.enabled)
        {
            agent.enabled = true;
        }

        if (!agent.isOnNavMesh)
        {
            agent.Warp(hit.position);
        }

        agent.isStopped = false;
        agent.speed = chaseSpeed;
        agent.angularSpeed = 720f;
        agent.acceleration = 80f;
        agent.stoppingDistance = Mathf.Max(0.1f, meleeDistance - stopBuffer);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, meleeDistance);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, alarmDistance);
    }
}
