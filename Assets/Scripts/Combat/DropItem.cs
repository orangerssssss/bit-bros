using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 控制物品 金币的掉落, 包括掉落物品的ID及概率
/// </summary>
public class DropItem : MonoBehaviour
{
    [SerializeField]
    private Transform dropPoint; // 物品掉落位置
    [SerializeField]
    private int coin;// 掉落金币数
    [SerializeField]
    private List<DropData> drops; // 物品掉落列表

    /// <summary>
    /// 执行一次物品 金币掉落
    /// </summary>
    public void Drop()
    {
        // 掉落金币(直接添加进背包)
        if (coin > 0) InventoryManager.Instance.AddCoin(coin);

        // 根据ID及概率生成掉落物品
        foreach(DropData drop in drops)
        {
            if (Random.Range(0, 100) < drop.probabilityPercent)
                InstantiateItem(drop.id);
        }
    }

    /// <summary>
    /// 根据ID生成掉落物品
    /// </summary>
    /// <param name="id">掉落物品的ID</param>
    private void InstantiateItem(int id)
    {
        // 生成物品
        GameObject item = Instantiate(DataManager.Instance.itemConfig.FindItemByID(id).itemPrefab, dropPoint.transform.position, Random.rotation);

        // 开启物理效果, 添加初速度
        Rigidbody rig = item.GetComponent<Rigidbody>();
        rig.isKinematic = false;
        rig.velocity = new Vector3(Random.Range(-1.5f, 1.5f), 3.0f, Random.Range(-1.5f, 1.5f));

        // 激活物品的拾取组件, 并传递ID值
        PickableItem pickableItem = item.GetComponent<PickableItem>();
        pickableItem.enabled = true;
        pickableItem.itemID = id;
    }
}

/// <summary>
/// 掉落物品数据
/// </summary>
[System.Serializable]
public class DropData
{
    public int id;// 掉落物品的ID
    public int probabilityPercent;// 掉落物品的概率(100为必定掉落)
}
