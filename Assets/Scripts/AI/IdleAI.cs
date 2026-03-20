using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 待机AI的控制, 包括行为及动画表现
/// </summary>
public class IdleAI : MonoBehaviour
{
    private static Transform player;// 玩家

    private Animator animator;// 动画组件
    private NavMeshAgent agent;// 寻路代理组件

    [SerializeField] private List<NPCIdlePoint> idlePoints;// NPC待机点List, 如果List不为空, NPC会在待机位置间循环走动并播放对应动画

    private int idlePointsIndex = 0;// 当前待机点List的索引
    private float idleTimer;// 待机计时器

    private Quaternion targetRotation;// 目标旋转方向, 用于插值改变NPC的朝向
    private Quaternion startRotation;// 初始旋转方向, 用于原地站立的NPC恢复原朝向
    private bool isChat = false;// 是否处于对话状态
    private bool isAnim = false;// 是否处于待机动画状态

    private void Awake()
    {
        if (player == null) player = GameObject.FindGameObjectWithTag("Player").transform;
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();

        //  记录初始旋转
        if (idlePoints == null || idlePoints.Count == 0) startRotation = transform.rotation;
    }

    /// <summary>
    /// 在被激活时初始化
    /// </summary>
    private void OnEnable()
    {
        isChat = false;
        isAnim = false;
        agent.isStopped = false;
        idlePointsIndex = 0;

        if (idlePoints != null && idlePoints.Count > 0)
        {
            transform.position = idlePoints[idlePointsIndex].point.position;
            agent.destination = idlePoints[idlePointsIndex].point.position;
            idleTimer = idlePoints[idlePointsIndex].idleTime;
        }
    }

    private void Update()
    {
        if (agent.isStopped)
        {
            // 对话状态，NPC朝向目标方向
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * 3.5f);
        }
        else
        {
            // 待机状态，执行待机逻辑
            Action();
        }

        // 更新动画机中的移动速度为寻路的移动速度
        if (idlePoints != null && idlePoints.Count > 0) animator.SetFloat("MoveSpeed", agent.velocity.magnitude);
    }

    /// <summary>
    /// 主行为逻辑, idlePoints不为空时NPC循环走动并播放动画
    /// </summary>
    private void Action()
    {
        // idlePoints不为空检测
        if (idlePoints != null && idlePoints.Count > 0)
        {
            // 到达目的地检测
            if (!agent.pathPending && agent.remainingDistance < 0.5f)
            {
                // 判断是否需要播放动画
                if (idlePoints[idlePointsIndex].idleClip != "")
                {
                    // 待机动画协程
                    StopCoroutine("IdleAnim");
                    StartCoroutine("IdleAnim");
                }
                else
                {
                    if (idleTimer <= 0)
                    {
                        // 更新待机点
                        idlePointsIndex = (idlePointsIndex + 1) % idlePoints.Count;
                        agent.destination = idlePoints[idlePointsIndex].point.position;

                        // 重置计时器
                        idleTimer = idlePoints[idlePointsIndex].idleTime;
                    }
                    else
                    {
                        idleTimer -= Time.deltaTime;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 开始NPC对话行为, 不处于待机动画状态则停止寻路代理并转向玩家
    /// </summary>
    public void StartDialog()
    {
        isChat = true;
        if (!isAnim)
        {
            StopCoroutine("StartAgent");
            agent.isStopped = true;

            Vector3 dir = player.position - transform.position;
            dir.y = 0;
            targetRotation.SetLookRotation(dir, Vector3.up);
        }
    }

    /// <summary>
    /// 停止NPC对话行为
    /// </summary>
    public void StopDialog()
    {
        isChat = false;
        if (!isAnim) StartCoroutine("StartAgent");
    }

    /// <summary>
    /// 重新启动寻路代理
    /// </summary>
    private IEnumerator StartAgent()
    {
        if (idlePoints == null || idlePoints.Count == 0)
        {
            // 原地站立的NPC恢复原朝向
            targetRotation = startRotation;
            yield return new WaitForSeconds(2.0f);
        }
        else
        {
            yield return new WaitForSeconds(2.0f);
        }
        agent.isStopped = false;
    }

    /// <summary>
    /// NPC播放待机动画
    /// </summary>
    private IEnumerator IdleAnim()
    {
        if (!isChat)
        {
            isAnim = true;
            agent.isStopped = true;
            agent.velocity = Vector3.zero;

            // 将NPC朝向设置为待机点的朝向
            targetRotation = idlePoints[idlePointsIndex].point.rotation;

            // 播放对应的待机动画
            AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
            foreach (AnimationClip clip in clips)
            {
                if (clip.name == idlePoints[idlePointsIndex].idleClip)
                {
                    animator.CrossFadeInFixedTime(idlePoints[idlePointsIndex].idleClip, 0.5f);
                    // 等待动画播放完毕及额外等待时间(idleTime)
                    yield return new WaitForSeconds(clip.length + 1.0f + idlePoints[idlePointsIndex].idleTime);
                }
            }

            isAnim = false;
            agent.isStopped = false;
        }

        // 更新待机点
        idlePointsIndex = (idlePointsIndex + 1) % idlePoints.Count;
        agent.destination = idlePoints[idlePointsIndex].point.position;
        idleTimer = idlePoints[idlePointsIndex].idleTime;

        if (isChat) StartDialog();
    }
}

/// <summary>
/// NPC待机位置类
/// </summary>
[System.Serializable]
public struct NPCIdlePoint
{
    public float idleTime;// 额外停留时间
    public Transform point;// 位置
    public string idleClip;// 待机动画
}
