using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 野猪AI, 包括行为及动画表现
/// </summary>
public class FightAIBoar : FightAI
{
    [SerializeField]
    private float runSeconds = 6.0f;// 受到伤害后的奔跑时间(sec)
    [SerializeField]
    private float idleRange = 10.0f;// 随机生成待机位置的范围(m)

    private List<Vector3> idlePoints;// 待机位置List, 在初始化时随机生成

    private float idleTimer;// 待机计时器
    private float runTimer;// 奔跑计时器

    protected override void InitFightAI()
    {
        base.InitFightAI();

        InitIdlePoints();
    }

    private void Update()
    {
        if (agent.enabled)
        {
            // 在非移动状态且非动画过渡状态停止寻路
            agent.isStopped = !animator.GetCurrentAnimatorStateInfo(0).IsName("Move") || animator.IsInTransition(0);

            Action();

            // 更新动画机中的移动速度为寻路的移动速度
            animator.SetFloat("MoveSpeed", agent.velocity.magnitude);
        }
    }

    /// <summary>
    /// 受伤
    /// </summary>
    public override void GetDamageReact()
    {
        base.GetDamageReact();

        // 触发受伤动画
        if (fightAttributes.health > 0)
        {
            animator.SetTrigger("Impact");
        }

        // 进入奔跑状态
        runTimer = runSeconds;
        agent.speed = 6.0f;
    }

    /// <summary>
    /// 初始化
    /// </summary>
    public override void ResetFightAI()
    {
        idleTimer = 0;
        runTimer = 0;

        animator.SetTrigger("Reset");
        enemyCollider.enabled = true;
        agent.enabled = true;
        InitIdlePoints();
    }

    /// <summary>
    /// 主行为逻辑, 在未受到攻击时在待机位置间来回走动, 在受到攻击后奔跑一段时间
    /// </summary>
    private void Action()
    {
        if (runTimer > 0)// 奔跑状态
        {
            // 在到达目的地后随机待机位置为新的目的地
            if (!agent.pathPending && agent.remainingDistance < 0.5f)
            {
                agent.destination = idlePoints[Random.Range(0, idlePoints.Count)];
            }
        }
        else// 一般待机状态
        {
            // 恢复移动速度
            if (agent.speed > 2)
            {
                agent.speed = 1.0f;
            }

            // 在到达目的地并且经过一定时间后后随机待机位置为新的目的地(注: idleTimer在移动时仍会计时,其随机值并不是到达目的地后站立的时间)
            if (!agent.pathPending && agent.remainingDistance < 0.5f && idleTimer <= 0)
            {
                agent.destination = idlePoints[Random.Range(0, idlePoints.Count)];
                idleTimer = Random.Range(15.0f, 30.0f);
            }
        }

        // 更新计时器
        runTimer -= Time.deltaTime;
        idleTimer -= Time.deltaTime;
    }

    /// <summary>
    /// 初始化待机位置List
    /// </summary>
    private void InitIdlePoints()
    {
        idlePoints = new List<Vector3>();
        idlePoints.Add(transform.position + new Vector3(Random.Range(idleRange / 2, idleRange), 0, Random.Range(idleRange / 2, idleRange)));
        idlePoints.Add(transform.position + new Vector3(Random.Range(-idleRange, -idleRange / 2), 0, Random.Range(idleRange / 2, idleRange)));
        idlePoints.Add(transform.position + new Vector3(Random.Range(-idleRange, -idleRange / 2), 0, Random.Range(-idleRange, -idleRange / 2)));
        idlePoints.Add(transform.position + new Vector3(Random.Range(idleRange / 2, idleRange), 0, Random.Range(-idleRange, -idleRange / 2)));
    }
}
