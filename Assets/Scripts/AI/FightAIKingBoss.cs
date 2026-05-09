using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// King Boss AI：包含巡逻、索敌、绕侧、突进、连击、二阶段狂暴等行为。
/// </summary>
public class FightAIKingBoss : FightAI
{
    private enum BossState
    {
        Idle,
        Chase,
        Strafe,
        Retreat,
        Lunge,
        Combo,
        Recover,
        PhaseShift
    }

    private static Transform cachedPlayer;

    [Header("Boss Movement")]
    [SerializeField] private float patrolRadius = 4.0f;
    [SerializeField] private float alarmDistance = 20.0f;
    [SerializeField] private float leashDistance = 30.0f;
    [SerializeField] private float meleeDistance = 2.6f;
    [SerializeField] private float preferredDistance = 3.6f;
    [SerializeField] private float chaseSpeed = 4.1f;
    [SerializeField] private float strafeSpeed = 3.0f;
    [SerializeField] private float retreatSpeed = 3.4f;
    [SerializeField] private float lungeSpeed = 6.8f;
    [SerializeField] private float turnSpeed = 10.0f;

    [Header("Boss Rhythm")]
    [SerializeField] private float baseAttackCooldown = 1.5f;
    [SerializeField] private float actionDecisionInterval = 0.45f;
    [SerializeField] private float strafeDuration = 1.3f;
    [SerializeField] private float retreatDuration = 0.7f;
    [SerializeField] private float lungeDuration = 0.75f;
    [SerializeField] private float comboBuffer = 0.22f;
    [SerializeField] private float blockedStaggerDuration = 0.95f;

    [Header("Boss Phase 2")]
    [SerializeField, Range(0.1f, 0.95f)] private float phaseTwoHealthRatio = 0.5f;
    [SerializeField] private float phaseShiftDuration = 1.1f;
    [SerializeField] private float phaseTwoSpeedBonus = 1.2f;
    [SerializeField] private float phaseTwoCooldownMultiplier = 0.72f;
    [SerializeField] private float phaseTwoDamageMultiplier = 1.35f;

    [Header("Boss Combat")]
    [SerializeField] private BoxAttackArea attackBox;
    [SerializeField] private AudioSource attackAudioSource;
    [SerializeField] private float comboFinisherDamageMultiplier = 1.65f;
    [SerializeField] private float lungeDamageMultiplier = 1.45f;

    private CharacterAttributes target;
    private Vector3 startPosition;
    private Vector3 patrolDestination;
    private BossState state = BossState.Idle;
    private float actionTimer;
    private float attackCooldownTimer;
    private float stateTimer;
    private float staggerTimer;
    private float targetMoveSpeed;
    private float currentDamageMultiplier = 1.0f;
    private bool hasPatrolDestination;
    private bool phaseTwo;
    private bool strafeLeft;
    private int queuedComboHits;

    protected override void InitFightAI()
    {
        base.InitFightAI();

        startPosition = transform.position;
        patrolDestination = transform.position;

        if (cachedPlayer == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) cachedPlayer = player.transform;
        }

        if (animator != null)
        {
            animator.applyRootMotion = false;
            animator.SetFloat("MoveSpeed", 0f);
            animator.SetBool("Alarm", false);
        }

        if (agent != null)
        {
            agent.speed = chaseSpeed;
            agent.angularSpeed = 720f;
            agent.acceleration = 60f;
        }
    }

    private void OnEnable()
    {
        startPosition = transform.position;
        ResetFightAI();
    }

    private void Update()
    {
        if (isDead || animator == null || fightAttributes == null)
        {
            return;
        }

        attackCooldownTimer += Time.deltaTime;
        actionTimer += Time.deltaTime;

        if (staggerTimer > 0f)
        {
            staggerTimer -= Time.deltaTime;
            StopMotion();
            UpdateMoveSpeed(0f);
            return;
        }

        if (ShouldEnterPhaseTwo())
        {
            EnterPhaseTwo();
        }

        AcquireTarget();
        ProcessQueuedCombo();

        if (state == BossState.PhaseShift)
        {
            stateTimer -= Time.deltaTime;
            StopMotion();
            UpdateMoveSpeed(0f);
            if (stateTimer <= 0f)
            {
                state = BossState.Chase;
            }
            return;
        }

        if (target == null || target.health <= 0)
        {
            RunIdlePatrol();
            return;
        }

        float distance = Vector3.Distance(transform.position, target.transform.position);
        if (distance > leashDistance)
        {
            target = null;
            state = BossState.Idle;
            RunIdlePatrol();
            return;
        }

        FaceTarget(target.transform.position, turnSpeed * 1.2f);

        if (state == BossState.Lunge)
        {
            TickLunge();
            return;
        }

        if (state == BossState.Strafe)
        {
            TickStrafe(distance);
            return;
        }

        if (state == BossState.Retreat)
        {
            TickRetreat();
            return;
        }

        if (state == BossState.Recover)
        {
            TickRecover(distance);
            return;
        }

        if (IsInAttackAnimation())
        {
            StopMotion();
            UpdateMoveSpeed(0f);
            return;
        }

        if (actionTimer >= actionDecisionInterval)
        {
            actionTimer = 0f;
            DecideNextAction(distance);
        }

        TickChase(distance);
    }

    public override void GetDamageReact()
    {
        base.GetDamageReact();

        if (animator != null && fightAttributes.health > 0 && !IsInAttackAnimation())
        {
            animator.SetTrigger("Impact");
        }
    }

    public override void ResetFightAI()
    {
        isDead = false;
        phaseTwo = false;
        state = BossState.Idle;
        target = null;
        staggerTimer = 0f;
        stateTimer = 0f;
        actionTimer = 0f;
        attackCooldownTimer = CurrentAttackCooldown();
        queuedComboHits = 0;
        currentDamageMultiplier = 1f;
        hasPatrolDestination = false;
        strafeLeft = false;

        if (enemyCollider != null) enemyCollider.enabled = true;

        if (agent != null)
        {
            agent.enabled = true;
            agent.isStopped = false;
            agent.speed = chaseSpeed;
            agent.ResetPath();
        }

        if (animator != null)
        {
            animator.ResetTrigger("Attack");
            animator.ResetTrigger("Impact");
            animator.ResetTrigger("Died");
            animator.SetBool("Alarm", false);
            animator.SetFloat("MoveSpeed", 0f);
        }

        startPosition = transform.position;
    }

    public override void OnShieldBlocked(float staggerDuration)
    {
        if (isDead)
        {
            return;
        }

        queuedComboHits = 0;
        currentDamageMultiplier = 1f;
        staggerTimer = Mathf.Max(staggerTimer, staggerDuration > 0f ? staggerDuration : blockedStaggerDuration);
        state = BossState.Retreat;
        stateTimer = retreatDuration + 0.2f;

        if (animator != null)
        {
            animator.SetTrigger("Impact");
        }
    }

    private void AcquireTarget()
    {
        var manager = CombatCharacterManager.Instance;
        if (fightAttributes.combatCamp == CombatCamp.Enemy && manager != null)
        {
            target = manager.FindNearestPlayerCampCharacter(transform.position);
        }
        else if (fightAttributes.combatCamp == CombatCamp.Player && manager != null)
        {
            target = manager.FindNearestEnemyCampCharacter(transform.position);
        }

        if (target == null && cachedPlayer != null)
        {
            target = cachedPlayer.GetComponent<CharacterAttributes>();
        }

        if (target == null && cachedPlayer == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                cachedPlayer = player.transform;
                target = player.GetComponent<CharacterAttributes>();
            }
        }
    }

    private void RunIdlePatrol()
    {
        if (animator != null) animator.SetBool("Alarm", false);

        if (Vector3.Distance(transform.position, startPosition) > patrolRadius * 1.5f)
        {
            MoveTo(startPosition, chaseSpeed * 0.9f);
            return;
        }

        if (!hasPatrolDestination || Vector3.Distance(transform.position, patrolDestination) < 0.6f)
        {
            Vector2 offset = Random.insideUnitCircle * patrolRadius;
            patrolDestination = startPosition + new Vector3(offset.x, 0f, offset.y);
            hasPatrolDestination = true;
        }

        MoveTo(patrolDestination, strafeSpeed * 0.75f);
    }

    private void TickChase(float distance)
    {
        if (distance > alarmDistance)
        {
            RunIdlePatrol();
            return;
        }

        if (animator != null) animator.SetBool("Alarm", true);

        Vector3 targetPosition = target.transform.position;
        Vector3 direction = (targetPosition - transform.position).normalized;
        Vector3 desiredPoint = targetPosition - direction * preferredDistance;

        if (distance > preferredDistance + 0.4f)
        {
            MoveTo(desiredPoint, CurrentChaseSpeed());
        }
        else if (distance < meleeDistance * 0.8f)
        {
            MoveDirect(-transform.forward, retreatSpeed * 0.7f);
        }
        else
        {
            StopMotion();
            UpdateMoveSpeed(0f);
        }
    }

    private void DecideNextAction(float distance)
    {
        if (distance > alarmDistance)
        {
            return;
        }

        if (!CanAttack())
        {
            if (distance <= preferredDistance + 1.0f && Random.value < 0.45f)
            {
                BeginStrafe();
            }
            return;
        }

        if (distance <= meleeDistance)
        {
            float roll = Random.value;
            if (phaseTwo && roll < 0.25f)
            {
                BeginLunge();
            }
            else if (roll < 0.7f)
            {
                BeginCombo();
            }
            else
            {
                BeginStrafe();
            }
            return;
        }

        if (distance <= preferredDistance + 2.5f)
        {
            if (Random.value < (phaseTwo ? 0.6f : 0.35f))
            {
                BeginLunge();
            }
            else
            {
                BeginStrafe();
            }
        }
    }

    private void BeginCombo()
    {
        state = BossState.Combo;
        StopMotion();
        queuedComboHits = phaseTwo ? Random.Range(2, 4) : Random.Range(1, 3);
        currentDamageMultiplier = queuedComboHits >= 3 ? comboFinisherDamageMultiplier : 1f;
        TriggerAttack();
    }

    private void BeginStrafe()
    {
        state = BossState.Strafe;
        stateTimer = strafeDuration * Random.Range(0.85f, 1.15f);
        strafeLeft = Random.value > 0.5f;
    }

    private void BeginLunge()
    {
        state = BossState.Lunge;
        stateTimer = lungeDuration;
        queuedComboHits = phaseTwo ? 1 : 0;
        currentDamageMultiplier = lungeDamageMultiplier;
        TriggerAttack();
    }

    private void TickStrafe(float distance)
    {
        stateTimer -= Time.deltaTime;
        if (target == null)
        {
            state = BossState.Idle;
            return;
        }

        Vector3 toTarget = target.transform.position - transform.position;
        toTarget.y = 0f;
        Vector3 side = strafeLeft ? -transform.right : transform.right;
        Vector3 orbitTarget = target.transform.position - toTarget.normalized * preferredDistance + side * 1.5f;
        MoveTo(orbitTarget, strafeSpeed);

        if (distance <= meleeDistance && CanAttack() && Random.value < 0.3f)
        {
            BeginCombo();
            return;
        }

        if (stateTimer <= 0f)
        {
            state = BossState.Chase;
        }
    }

    private void TickRetreat()
    {
        stateTimer -= Time.deltaTime;
        MoveDirect(-transform.forward, retreatSpeed);
        if (stateTimer <= 0f)
        {
            state = BossState.Chase;
        }
    }

    private void TickLunge()
    {
        stateTimer -= Time.deltaTime;
        MoveDirect(transform.forward, lungeSpeed);
        if (stateTimer <= 0f)
        {
            state = BossState.Recover;
            stateTimer = comboBuffer;
            currentDamageMultiplier = 1f;
        }
    }

    private void TickRecover(float distance)
    {
        stateTimer -= Time.deltaTime;
        StopMotion();
        UpdateMoveSpeed(0f);
        if (stateTimer <= 0f)
        {
            state = distance <= meleeDistance ? BossState.Retreat : BossState.Chase;
            stateTimer = retreatDuration * 0.7f;
        }
    }

    private void ProcessQueuedCombo()
    {
        if (queuedComboHits <= 0 || IsInAttackAnimation())
        {
            return;
        }

        if (state != BossState.Combo && state != BossState.Lunge)
        {
            return;
        }

        stateTimer -= Time.deltaTime;
        if (stateTimer > 0f || !CanAttack())
        {
            return;
        }

        if (target == null)
        {
            queuedComboHits = 0;
            state = BossState.Chase;
            currentDamageMultiplier = 1f;
            return;
        }

        float distance = Vector3.Distance(transform.position, target.transform.position);
        if (distance > meleeDistance + 1.1f)
        {
            queuedComboHits = 0;
            state = BossState.Chase;
            currentDamageMultiplier = 1f;
            return;
        }

        currentDamageMultiplier = queuedComboHits == 1 ? comboFinisherDamageMultiplier : 1f;
        TriggerAttack();
    }

    private void TriggerAttack()
    {
        if (animator == null)
        {
            return;
        }

        attackCooldownTimer = 0f;
        stateTimer = comboBuffer;
        if (queuedComboHits > 0)
        {
            queuedComboHits--;
        }
        animator.SetTrigger("Attack");
    }

    private void AttackEvent()
    {
        if (attackBox == null || fightAttributes == null)
        {
            return;
        }

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.Move(transform.forward * 0.08f);
        }

        float damageMultiplier = phaseTwo ? phaseTwoDamageMultiplier : 1f;
        int damage = Mathf.RoundToInt(fightAttributes.PhysicalAttack * damageMultiplier * currentDamageMultiplier);
        attackBox.AreaDamage(Mathf.Max(1, damage), true);
    }

    private void PlayAttackSFX()
    {
        if (attackAudioSource == null)
        {
            return;
        }

        attackAudioSource.Stop();
        attackAudioSource.Play();
    }

    private void MoveTo(Vector3 destination, float speed)
    {
        destination.y = transform.position.y;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.speed = speed;
            agent.SetDestination(destination);
            UpdateMoveSpeed(agent.velocity.magnitude);
        }
        else
        {
            Vector3 direction = destination - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
            {
                MoveDirect(direction.normalized, speed);
            }
            else
            {
                UpdateMoveSpeed(0f);
            }
        }
    }

    private void MoveDirect(Vector3 direction, float speed)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.001f)
        {
            UpdateMoveSpeed(0f);
            return;
        }

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        transform.position += direction.normalized * speed * Time.deltaTime;
        FaceTarget(transform.position + direction, turnSpeed);
        UpdateMoveSpeed(speed);
    }

    private void StopMotion()
    {
        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            agent.ResetPath();
        }
    }

    private void UpdateMoveSpeed(float speed)
    {
        targetMoveSpeed = speed;
        animator.SetFloat("MoveSpeed", Mathf.Lerp(animator.GetFloat("MoveSpeed"), targetMoveSpeed, 0.15f));
    }

    private void FaceTarget(Vector3 position, float speed)
    {
        Vector3 direction = position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * speed);
    }

    private bool CanAttack()
    {
        return attackCooldownTimer >= CurrentAttackCooldown() && !IsInAttackAnimation();
    }

    private float CurrentAttackCooldown()
    {
        return phaseTwo ? baseAttackCooldown * phaseTwoCooldownMultiplier : baseAttackCooldown;
    }

    private float CurrentChaseSpeed()
    {
        return phaseTwo ? chaseSpeed * phaseTwoSpeedBonus : chaseSpeed;
    }

    private bool ShouldEnterPhaseTwo()
    {
        return !phaseTwo
            && fightAttributes.MaxHealth > 0
            && fightAttributes.health > 0
            && (float)fightAttributes.health / fightAttributes.MaxHealth <= phaseTwoHealthRatio;
    }

    private void EnterPhaseTwo()
    {
        phaseTwo = true;
        state = BossState.PhaseShift;
        stateTimer = phaseShiftDuration;
        queuedComboHits = 0;
        currentDamageMultiplier = 1f;
        staggerTimer = 0f;

        if (animator != null)
        {
            animator.SetTrigger("Impact");
        }
    }

    private bool IsInAttackAnimation()
    {
        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
        AnimatorStateInfo nextState = animator.GetNextAnimatorStateInfo(0);
        return currentState.IsTag("Attack") || nextState.IsTag("Attack");
    }
}
