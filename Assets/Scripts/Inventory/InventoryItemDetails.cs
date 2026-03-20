using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 物品详细信息UI, 用于鼠标悬浮在物品上时显示物品详细信息
/// </summary>
public class InventoryItemDetails : MonoBehaviour
{
    [Header("通用面板")]
    [SerializeField]
    private GameObject commonPanel;// 通用面板物体
    [SerializeField]
    private Text commonPanelName;// 通用面板物品名称
    [SerializeField]
    private Text commonPanelDescription;// 通用面板物品描述
    [SerializeField]
    private Text commonPanelType;// 通用面板物品类型
    [SerializeField]
    private Text commonPanelValue;// 通用面板物品价值
    [SerializeField]
    private Text commonPanelTips;// 通用面板提示

    [Header("装备面板")]
    [SerializeField]
    private GameObject equipmentPanel;// 装备面板物体
    [SerializeField]
    private Text equipmentPanelName;// 装备面板物品名称
    [SerializeField]
    private Text equipmentPanelDescription;// 装备面板物品描述
    [SerializeField]
    private Text equipmentPanelType;// 装备面板物品类型
    [SerializeField]
    private Text equipmentPanelValue;// 装备面板物品价值
    [SerializeField]
    private Text equipmentPanelTips;// 装备面板提示
    [SerializeField]
    private Text equipmentPanelAttributes;// 装备面板物品属性

    private Camera uiCamera;// UI相机
    private Vector3 forwardOffset;// 前偏移量, 保证正确的物体间遮挡关系

    private void Awake()
    {
        uiCamera = GameObject.FindGameObjectWithTag("UICamera").GetComponent<Camera>();
        forwardOffset = Vector3.forward * uiCamera.WorldToScreenPoint(transform.position).z;
    }

    private void Update()
    {
        if (gameObject.activeSelf)
        {
            // 跟随鼠标位置
            transform.position = uiCamera.ScreenToWorldPoint(Input.mousePosition) + forwardOffset;
        }
    }

    /// <summary>
    /// 显示物品详细信息
    /// </summary>
    /// <param name="item">物品</param>
    public void ShowDetails(Item item)
    {
        if (item.itemType == ItemType.Material || item.itemType == ItemType.Usable)
        {
            commonPanel.SetActive(true);
            commonPanelName.color = ItemLevelColor(item.itemLevel);
            commonPanelName.text = item.itemName;
            commonPanelDescription.text = item.itemDescription;
            commonPanelType.text = ItemTypeText(item);
            commonPanelValue.text = ItemValueText(InventoryManager.Instance.inventoryType, item.itemValue);
            commonPanelTips.text = ItemTipsText(InventoryManager.Instance.inventoryType, item.itemType, item.itemValue);
        }
        else if (item.itemType == ItemType.Weapon || item.itemType == ItemType.Armor)
        {
            equipmentPanel.SetActive(true);
            equipmentPanelName.color = ItemLevelColor(item.itemLevel);
            equipmentPanelName.text = item.itemName;
            equipmentPanelDescription.text = item.itemDescription;
            equipmentPanelType.text = ItemTypeText(item);
            equipmentPanelValue.text = ItemValueText(InventoryManager.Instance.inventoryType, item.itemValue);
            equipmentPanelTips.text = ItemTipsText(InventoryManager.Instance.inventoryType, item.itemType, item.itemValue);
            equipmentPanelAttributes.text = EquipmentAttributesText(item);
        }
        transform.position = uiCamera.ScreenToWorldPoint(Input.mousePosition) + forwardOffset;
    }

    /// <summary>
    /// 关闭物品详细描述
    /// </summary>
    public void CloseDetails()
    {
        commonPanel.SetActive(false);
        equipmentPanel.SetActive(false);
    }

    /// <summary>
    /// 根据物品品质返回不同颜色
    /// </summary>
    /// <param name="level">物品品质</param>
    /// <returns>品质对应的颜色</returns>
    private Color ItemLevelColor(ItemLevel level)
    {
        if (level == ItemLevel.Common)
        {
            return new Color(1, 1, 1, 1);
        }
        else if (level == ItemLevel.Uncommon)
        {
            return new Color(0.4f, 1, 0.5f, 1);
        }
        else if (level == ItemLevel.Rare)
        {
            return new Color(0.7f, 0.7f, 1, 1);
        }
        else if (level == ItemLevel.Epic)
        {
            return new Color(1, 0.6f, 0.4f, 1);
        }

        return Color.black;
    }

    /// <summary>
    /// 根据物品类型返回类型描述
    /// </summary>
    /// <param name="item">物品</param>
    /// <returns>物品类型的文本描述</returns>
    private string ItemTypeText(Item item)
    {
        ItemType type = item.itemType;
        if (type == ItemType.Material)
        {
            return "材料";
        }
        else if (type == ItemType.Usable)
        {
            UsableItem usableItem = (UsableItem)item;
            if (usableItem.consumable)
                return "消耗品";
            else
                return "道具";
        }
        else if (type == ItemType.Weapon)
        {
            return "武器";
        }
        else if (type == ItemType.Armor)
        {
            return "防具";
        }

        return "物品";
    }
    
    /// <summary>
    /// 获得物品的出售或购买价格
    /// </summary>
    /// <param name="invType">库存的类型</param>
    /// <param name="value">物品的原价值</param>
    /// <returns>物品的出售或购买价格描述</returns>
    private string ItemValueText(InventoryType invType, int value)
    {
        value = invType == InventoryType.Buy ? value * 2 : value;
        if (value < 0)
        {
            return "无法出售";
        }
        else
        {
            return "单价: " + value.ToString();
        }
    }

    /// <summary>
    /// 获得物品操作提示(购买 出售 装备 使用)
    /// </summary>
    /// <param name="invType">库存的类型</param>
    /// <param name="itemType">物品的类型</param>
    /// <param name="value">物品的原价值</param>
    /// <returns>操作提示描述</returns>
    private string ItemTipsText(InventoryType invType, ItemType itemType, int value)
    {
        string tips = "";
        if (invType == InventoryType.Buy)
        {
            tips = "'右键'购买";
        }
        else if (invType == InventoryType.Sell && value >= 0)
        {
            tips = "'右键'出售";
        }
        else if (invType == InventoryType.Package)
        {
            if (itemType == ItemType.Weapon || itemType == ItemType.Armor)
                tips = "'拖动'装备";
            else if (itemType == ItemType.Usable)
                tips = "'右键'使用";
        }
        return tips;
    }

    /// <summary>
    /// 获得装备的属性描述
    /// </summary>
    /// <param name="item">物品</param>
    /// <returns>装备的属性描述</returns>
    private string EquipmentAttributesText(Item item)
    {
        string text = "";
        if (item.itemType == ItemType.Weapon || item.itemType == ItemType.Armor)
        {
            EquipmentItem equipment = (EquipmentItem)item;
            if (equipment.constitution != 0)
            {
                if (equipment.constitution > 0)
                {
                    text += "体质 +" + equipment.constitution + "\n";
                }
                else
                {
                    text += "体质 " + equipment.constitution + "\n";
                }
            }
            if (equipment.strength != 0)
            {
                if (equipment.strength > 0)
                {
                    text += "力量 +" + equipment.strength + "\n";
                }
                else
                {
                    text += "力量 " + equipment.strength + "\n";
                }
            }
            if (equipment.intelligence != 0)
            {
                if (equipment.intelligence > 0)
                {
                    text += "敏捷 +" + equipment.intelligence + "\n";
                }
                else
                {
                    text += "敏捷 " + equipment.intelligence + "\n";
                }
            }
        }
        return text;
    }
}
