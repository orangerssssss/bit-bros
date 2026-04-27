using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 野兔AI, 包括行为及动画表现
/// </summary>
public class FightAIHare : FightAI
{
    private static Transform player;// 玩家

    [SerializeField]
    private float idleRange = 12.0f;// 随机生成待机位置的范围(m)

    private List<Vector3> idlePoints;// 待机位置List, 在初始化时随机生成

    private float idleTimer;// 待机计时器

    protected override void InitFightAI()
    {
        base.InitFightAI();

        if (player == null) player = GameObject.FindGameObjectWithTag("Player").transform;

        InitIdlePoints();
    }

    private void Update()
    {
        if (isDead) return;

        if (agent.enabled)
        {
            // 在非移动状态且非动画过渡状态停止寻路
            agent.isStopped = !animator.GetCurrentAnimatorStateInfo(0).IsName("Move") || animator.IsInTransition(0);

            Action(player.position);

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
    }

    /// <summary>
    /// 初始化
    /// </summary>
    public override void ResetFightAI()
    {
        idleTimer = 0;

        animator.SetTrigger("Reset");
        enemyCollider.enabled = true;
        agent.enabled = true;
        InitIdlePoints();

        // clear dead flag and re-enable child colliders/character controller if any
        isDead = false;
        try
        {
            var cc = GetComponent<CharacterController>();
            if (cc != null) cc.enabled = true;
            var cols = GetComponentsInChildren<Collider>(true);
            foreach (var c in cols) c.enabled = true;
        }
        catch { }

        // Restore Rigidbody to kinematic state when resetting AI (for pooled enemies)
        try
        {
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }
        catch { }
    }

    /// <summary>
    /// 主行为逻辑, 在待机位置间来回走动
    /// </summary>
    private void Action(Vector3 target)
    {

        if (Vector3.Distance(target, transform.position) < 8.0f)
        {
            idleTimer = 0;
        }

        // 在到达目的地并且经过一定时间后后随机待机位置为新的目的地(注: idleTimer在移动时仍会计时,其随机值并不是到达目的地后站立的时间)
        if (!agent.pathPending && agent.remainingDistance < 0.5f && idleTimer <= 0)
        {
            agent.destination = idlePoints[Random.Range(0, idlePoints.Count)];
            idleTimer = Random.Range(25.0f, 45.0f);
        }

        // 更新计时器
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
