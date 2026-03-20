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
        animator.SetTrigger("Died");
        enemyCollider.enabled = false;
        agent.enabled = false;

        // 开始销毁协程
        if (gameObject.activeInHierarchy) StartCoroutine(EnemyDestroy());
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
    /// 初始化
    /// </summary>
    public abstract void ResetFightAI();

    /// <summary>
    /// 敌人销毁协程. 由生成点生成的敌人被初始化并隐藏, 等待重新生成; 不由生成点生成则直接销毁该物体.
    /// </summary>
    protected IEnumerator EnemyDestroy()
    {
        // 敌人缓慢下落
        yield return new WaitForSeconds(corpseDelay);
        float fallTimer = 3.0f;
        while (fallTimer > 0)
        {
            transform.Translate(-Vector3.up * Time.deltaTime * 0.5f);
            fallTimer -= Time.deltaTime;
            yield return null;
        }

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
