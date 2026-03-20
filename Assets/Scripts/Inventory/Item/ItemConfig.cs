using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 物品品质
/// </summary>
public enum ItemLevel
{
    Common,
    Uncommon,
    Rare,
    Epic
}

/// <summary>
/// 物品类型
/// </summary>
public enum ItemType
{
    Material,
    Usable,
    Armor,
    Weapon
}

/// <summary>
/// 物品配置表(ScriptableObject)
/// </summary>
[CreateAssetMenu(fileName = "ItemConfig", menuName = "Game Config/New ItemConfig")]
public class ItemConfig : ScriptableObject
{
    [SerializeField]
    private List<Item> materialList;// 材料配置表
    [SerializeField]
    private List<UsableItem> usableList;// 道具配置表
    [SerializeField]
    private List<EquipmentItem> weaponList;// 武器配置表
    [SerializeField]
    private List<EquipmentItem> armorList;// 防具配置表

    /// <summary>
    /// 通过ID查找对应物品
    /// </summary>
    /// <param name="id">物品ID</param>
    /// <returns>查找到的物品, 为查找到则返回空</returns>
    public Item FindItemByID(int id)
    {
        switch (id / 1000)
        {
            case 1:
                foreach (Item item in materialList)
                {
                    if (item.itemID == id)
                        return item;
                }
                break;
            case 2:
                foreach (Item item in usableList)
                {
                    if (item.itemID == id)
                        return item;
                }
                break;
            case 3:
                foreach (Item item in armorList)
                {
                    if (item.itemID == id)
                        return item;
                }
                break;
            case 4:
                foreach (Item item in weaponList)
                {
                    if (item.itemID == id)
                        return item;
                }
                break;
        }
        return null;
    }
}

/// <summary>
/// 基础物品属性
/// </summary>
[System.Serializable]
public class Item
{
    public string itemName;
    public int itemID;
    [TextArea]
    public string itemDescription;
    public ItemLevel itemLevel;
    public ItemType itemType;
    public int itemValue;
    public GameObject itemPrefab;
}

/// <summary>
/// 道具物品属性
/// </summary>
[System.Serializable]
public class UsableItem : Item
{
    public bool consumable;
    public string itemEvent;
}

/// <summary>
/// 装备物品属性
/// </summary>
[System.Serializable] 
public class EquipmentItem : Item
{
    public int constitution;
    public int strength;
    public int intelligence;

    public EquipmentItem()
    {
        itemID = -1;
        constitution = 0;
        strength = 0;
        intelligence = 0;
    }
}