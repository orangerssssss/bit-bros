using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 敌人的头部显示, 包括等级 名称 血条
/// </summary>
public class EnemyHeadDisplayer : MonoBehaviour
{
    private static Transform mainCamera;// 主相机
    private FightAttributes enemyAttributes;// 自身的属性组件
    [SerializeField]
    private float showTime = 6.0f;// 显示时间(sec, 在受到伤害后一定时间内显示)
    [SerializeField]
    private GameObject state;// 头部显示父物体
    [SerializeField]
    private Text stateText;// 头部显示文本
    [SerializeField]
    private Slider healthBar;// 快血条(立即变化)
    [SerializeField]
    private Slider healthBarSlow;// 慢血条(缓慢过渡)

    private float targetValue;// 血条目标值, 用于慢血条过渡至目标值
    private float showTimer;// 显示时间计时器

    private void Awake()
    {
        if (mainCamera == null) mainCamera = Camera.main.transform;
        enemyAttributes = GetComponent<FightAttributes>();
    }

    private void Start()
    {
        InitState();
    }

    private void Update()
    {
        if (state.activeSelf)
        {
            // 始终朝向相机
            state.transform.LookAt(mainCamera);

            // 慢血条缓慢过渡至目标值
            healthBarSlow.value = Mathf.Lerp(healthBarSlow.value, targetValue, 1.5f * Time.deltaTime);
        }

        if (showTimer < 0)
        {
            if (state.activeSelf) state.SetActive(false);
        }
        else
        {
            showTimer -= Time.deltaTime;
        }
    }

    /// <summary>
    /// 初始化头部显示数据
    /// </summary>
    public void InitState()
    {
        // 血条初始化
        healthBar.value = 1.0f;
        healthBarSlow.value = 1.0f;
        targetValue = 1.0f;

        // 文本初始化
        stateText.text = "Lv." + enemyAttributes.level + " " + enemyAttributes.fightName;
    }

    /// <summary>
    /// 激活头部显示
    /// </summary>
    /// <param name="value">血条的目标值(0~1)</param>
    public void ShowState(float value)
    {
        if (!state.activeSelf) state.SetActive(true);
        showTimer = showTime;

        healthBar.value = value;
        targetValue = value;
    }

    /// <summary>
    /// 隐藏头部显示
    /// </summary>
    public void HideState()
    {
        state.SetActive(false);
    }
}
