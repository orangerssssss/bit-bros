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
    }

    private void Update()
    {
        if (agent.enabled)
        {
            // 更新攻击计时器
            attackTimer += Time.deltaTime;

            // 在非移动状态停止寻路
            agent.isStopped = !animator.GetCurrentAnimatorStateInfo(0).IsTag("Move");

            // 寻找最近的目标
            if (fightAttributes.combatCamp == CombatCamp.Player)
                target = CombatCharacterManager.Instance.FindNearestEnemyCampCharacter(transform.position);
            else if (fightAttributes.combatCamp == CombatCamp.Enemy)
                target = CombatCharacterManager.Instance.FindNearestPlayerCampCharacter(transform.position);
            else
                target = null;

            // 行动
            if (target != null) Action(target.transform.position);

            // 插值动画机中的移动速度为目标移动速度
            animator.SetFloat("MoveSpeed", Mathf.Lerp(animator.GetFloat("MoveSpeed"), targetMoveSpeed, 0.1f));
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
        fightStateTimer = 0;
        attackTimer = 0;
        targetMoveSpeed = 0;
        moveForward = true;

        animator.SetTrigger("Reset");
        enemyCollider.enabled = true;
        agent.enabled = true;
    }

    /// <summary>
    /// 主行为逻辑, 在玩家靠近时接近玩家并进行攻击
    /// </summary>
    private void Action(Vector3 target)
    {
        float dis = Vector3.Distance(target, transform.position);

        if (dis > alarmDistance)// 待机状态(大于警戒距离)
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

        }
        else// 警戒状态(警戒距离内)
        {
            if (!animator.GetBool("Alarm"))
            {
                animator.SetBool("Alarm", true);
            }

            // TODO: 此处逐帧更新了寻路目的地, 需要修改
            agent.destination = target;
            targetMoveSpeed = 1.0f;

            // 更新战斗状态的行为
            FightStateUpdate(dis < fightDistance);

            if (moveForward)// 处于向前移动状态
            {
                if (dis < attackDistance)// 进入攻击范围
                {
                    // 停止移动
                    agent.velocity = Vector3.zero;
                    targetMoveSpeed = 0.0f;

                    // 攻击间隔低时会导致士兵持续攻击而无法转向
                    if (attackTimer > attackInterval)// 大于攻击间隔
                    {
                        // 攻击欲望判定
                        if (Random.Range(0.0f, 1.0f) < attackDesire)
                        {
                            // 触发攻击动画
                            animator.SetTrigger("Attack");
                            attackTimer = 0;
                        }
                        else
                        {
                            attackTimer = attackInterval;
                        }
                    }
                    else
                    {
                        Quaternion targetRot = Quaternion.LookRotation(new Vector3(target.x - transform.position.x, transform.position.y, target.z - transform.position.z), Vector3.up);
                        transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, Time.deltaTime * 24.0f);
                    }
                }
            }
            else// 不处于向前移动状态
            {
                agent.velocity = Vector3.zero;

                if (fightStateTimer < fightStateInterval * 0.7f && animator.GetCurrentAnimatorStateInfo(0).IsTag("Move"))
                {
                    // 前半段敌人后退
                    agent.Move(-transform.forward * Time.deltaTime * agent.speed);
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
        if (agent.enabled)
        {
            agent.Move(transform.forward * 0.01f);
            attackBox.AreaDamage(fightAttributes.PhysicalAttack, true);
        }
    }

    private void PlayAttackSFX()
    {
        attackAudioSource.Stop();
        attackAudioSource.Play();
    }
}
