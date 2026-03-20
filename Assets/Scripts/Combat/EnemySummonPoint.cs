using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敌人生成点, 生成敌人并在敌人死后一段时间后再次生成, 不断循环(仅在玩家靠近到一定距离时才会生成)
/// </summary>
public class EnemySummonPoint : MonoBehaviour
{
    private static Transform player;// 玩家

    [SerializeField]
    private GameObject enemyPrefab;// 敌人prefab, 用于指定生成哪种敌人
    [SerializeField]
    private float summonMinutes = 10.0f;// 生成间隔时间(min)
    [SerializeField]
    private float summonDistance = 50.0f;// 生成距离(m)

    private bool isAlive = false;// 生成的敌人是否存活
    private GameObject enemy;// 实例化的敌人物体
    private float summonTimer;// 生成间隔计时器

    private void Start()
    {
        if (!player) player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    private void Update()
    {
        if (!isAlive)
        {
            // 同时满足间隔和距离则生成敌人
            if (summonTimer <= 0 && Vector3.Distance(player.position, transform.position) < summonDistance)
            {
                Summon();
                summonTimer = summonMinutes * 60.0f;
            }

            summonTimer -= Time.deltaTime;
        }
    }

    /// <summary>
    /// 生成敌人, 第一次生成时实例化物体, 之后则重新激活该物体
    /// </summary>
    private void Summon()
    {
        if (!enemy)
        {
            // 实例化
            enemy = Instantiate(enemyPrefab, transform);
            enemy.GetComponent<FightAI>().summonPoint = this;
        }
        else
        {
            // 重新激活
            enemy.transform.SetPositionAndRotation(transform.position, transform.rotation);
            enemy.SetActive(true);
        }
        isAlive = true;
    }

    /// <summary>
    /// 生成的敌人死亡时调用该函数, 通知该生成点自己已经死亡
    /// </summary>
    public void EnemyDied()
    {
        isAlive = false;
    }
}
