using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 库存单元槽类型
/// </summary>
public enum InventorySlotType
{
    MaterialSlot,
    PropSlot,
    WeaponSlot,
    ArmorSlot,
    EquipmentSlot
}

/// <summary>
/// 库存单元槽类(背包 商店等), 包括库存物品在单元槽位置的变更, 以及由此产生的装备变更
/// </summary>
public class InventorySlot : MonoBehaviour, IDropHandler
{
    private static PlayerAttributes playerAttributes;// 玩家属性组件
    private static PlayerCombatController playerCombatController;// 玩家战斗控制器组件

    public InventorySlotType slotType;// 该单元槽所属类型
    [HideInInspector]
    public InventoryItem inventoryItem = null;// 该单元槽中包含的库存物品

    /// <summary>
    /// 该函数在物体放下时被调用
    /// </summary>
    /// <param name="eventData">光标事件</param>
    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) 
            return;
        if (InventoryManager.Instance.inventoryType == InventoryType.Buy) 
            return;

        // 获得被拖动中的库存物品
        InventoryItem draggedItem = eventData.pointerDrag.GetComponent<InventoryItem>();
        if (draggedItem == null || draggedItem.dropLegal == false)
            return;
        InventorySlot draggedSlot = draggedItem.inventorySlot;
        if (draggedItem.inventorySlot == this) 
            return;
        if (!IsDragLegal(draggedItem)) 
            return;

        // 装备变更
        if (slotType == InventorySlotType.WeaponSlot || draggedItem.inventorySlot.slotType == InventorySlotType.WeaponSlot ||
            slotType == InventorySlotType.ArmorSlot || draggedItem.inventorySlot.slotType == InventorySlotType.ArmorSlot)
            EquipmentChangeUpdate(draggedItem);

        // 物品换位
        if (inventoryItem)
        {
            inventoryItem.inventorySlot = draggedSlot;
            inventoryItem.transform.SetParent(draggedItem.inventorySlot.transform);
            inventoryItem.transform.localPosition = Vector3.zero;
        }
        draggedItem.inventorySlot = this;
        draggedItem.transform.SetParent(transform);
        eventData.pointerDrag.transform.localPosition = Vector3.zero;

        draggedSlot.inventoryItem = inventoryItem;
        inventoryItem = draggedItem;
        
    }

    /// <summary>
    /// 根据类型判断此次拖动是否合规
    /// </summary>
    /// <param name="draggedItem">拖动中的库存物品</param>
    /// <returns></returns>
    private bool IsDragLegal(InventoryItem draggedItem)
    {
        if (draggedItem == null) 
            return false;
        Item item = draggedItem.item;

        // 无法从已装备武器状态变为空手状态
        if (draggedItem.inventorySlot.slotType == InventorySlotType.WeaponSlot && inventoryItem == null)
        {
            GameUIManager.Instance.messageTip.ShowTip("无法卸下武器");
            return false;
        }

        switch (slotType)
        {
            case InventorySlotType.MaterialSlot:
                return item.itemType == ItemType.Material;
            case InventorySlotType.PropSlot:
                return item.itemType == ItemType.Usable;
            case InventorySlotType.WeaponSlot:
                return item.itemType == ItemType.Weapon;
            case InventorySlotType.ArmorSlot:
                return item.itemType == ItemType.Armor;
            case InventorySlotType.EquipmentSlot:
                return (item.itemType == ItemType.Weapon || item.itemType == ItemType.Armor) 
                    && ((draggedItem.inventorySlot.slotType == InventorySlotType.EquipmentSlot) || (inventoryItem == null || inventoryItem.item.itemType == item.itemType));
        }
        return false;
    }

    /// <summary>
    /// 装备变更
    /// </summary>
    /// <param name="draggedInvItem">拖动中的库存物品</param>
    private void EquipmentChangeUpdate(InventoryItem draggedInvItem)
    {
        if (playerAttributes == null) 
            playerAttributes = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerAttributes>();
        if (playerCombatController == null) 
            playerCombatController = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerCombatController>();
        
        EquipmentItem slotEquipment;
        if (inventoryItem == null)
        {
            slotEquipment = new EquipmentItem();
        }
        else
        {
            slotEquipment = (EquipmentItem)inventoryItem.item;
        }
        EquipmentItem draggedEquipment = (EquipmentItem)draggedInvItem.item;

        // 玩家属性及手持武器模型变动
        if (slotType == InventorySlotType.WeaponSlot || slotType == InventorySlotType.ArmorSlot)
        {
            playerAttributes.Constitution += draggedEquipment.constitution - slotEquipment.constitution;
            playerAttributes.Strength += draggedEquipment.strength - slotEquipment.strength;
            playerAttributes.Intelligence += draggedEquipment.intelligence - slotEquipment.intelligence;

            if(draggedInvItem.item.itemType == ItemType.Weapon) 
                playerCombatController.SwitchWeapon(draggedEquipment.itemID);
            if(draggedInvItem.item.itemType == ItemType.Armor) 
                playerCombatController.SwitchArmor(draggedEquipment.itemID);
        }
        else
        {
            playerAttributes.Constitution += slotEquipment.constitution - draggedEquipment.constitution;
            playerAttributes.Strength += slotEquipment.strength - draggedEquipment.strength;
            playerAttributes.Intelligence += slotEquipment.intelligence - draggedEquipment.intelligence;

            if (draggedInvItem.item.itemType == ItemType.Weapon) 
                playerCombatController.SwitchWeapon(slotEquipment.itemID);
            if (draggedInvItem.item.itemType == ItemType.Armor) 
                playerCombatController.SwitchArmor(slotEquipment.itemID);
        }

        //GameEventManager.Instance.playerEquipEvent.Invoke(draggedEquipment.itemID);
    }
}
