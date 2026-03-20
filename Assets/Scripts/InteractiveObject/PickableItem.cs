using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 可交互拾取物, 玩家能够拾取该物品
/// </summary>
public class PickableItem : InteractiveObject
{
    [SerializeField]
    private ParticleSystem sparks;// 物品的粒子效果(拾取物提示)
    
    public int itemID;// 物品的ID

    private Rigidbody rig;// 刚体组件
    private float stopTimer = 0.5f;// 关闭刚体的计时器, 在物体速度小于一定值一段时间后关闭物理模拟(用于减少性能消耗)

    private void Start()
    {
        rig = GetComponent<Rigidbody>();
        SparkColor();
    }

    private void Update()
    {
        // 在物体速度小于一定值一段时间后关闭物理模拟
        if (!rig.isKinematic)
        {
            float velocity = rig.velocity.magnitude;
            if (velocity > 1e-5)
            {
                stopTimer = 0.5f;
                if (rig.velocity.magnitude > 3)
                {
                    rig.velocity = rig.velocity.normalized * 3;
                }
            }
            else
            {
                stopTimer -= Time.deltaTime;
                if (stopTimer <= 0)
                {
                    rig.isKinematic = true;
                }
            }
        }
    }

    /// <summary>
    /// 获得物品并销毁该物体
    /// </summary>
    public override void Interact()
    {
        if (interactable && InventoryManager.Instance.AddItem(DataManager.Instance.itemConfig.FindItemByID(itemID)))
        {
            GameEventManager.Instance.pickUpItemEvent.Invoke(itemID);
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 根据物品的品质显示不同的粒子效果颜色
    /// </summary>
    private void SparkColor()
    {
        if (sparks)
        {
            var sparksMain = sparks.main;
            ItemLevel level = DataManager.Instance.itemConfig.FindItemByID(itemID).itemLevel;

            if (level == ItemLevel.Common)
            {
                sparksMain.startColor = new Color(1, 1, 1, 1);
            }
            else if (level == ItemLevel.Uncommon)
            {
                sparksMain.startColor = new Color(0.4f, 1, 0.5f, 1);
            }
            else if (level == ItemLevel.Rare)
            {
                sparksMain.startColor = new Color(0.7f, 0.7f, 1, 1);
            }
            else if (level == ItemLevel.Epic)
            {
                sparksMain.startColor = new Color(1, 0.6f, 0.4f, 1);
            }

            sparks.Play();
        }
    }
}
