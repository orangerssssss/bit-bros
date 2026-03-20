using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 库存的类型, 包括背包 购买 出售
/// </summary>
public enum InventoryType
{
    Package,
    Buy,
    Sell
}

/// <summary>
/// 库存管理器, 对各种库存进行管理
/// </summary>
public class InventoryManager : MonoBehaviour
{
    private static InventoryManager instance;// 单例

    public static InventoryManager Instance
    {
        get
        {
            if (instance == null) instance = GameObject.FindObjectOfType<InventoryManager>();
            return instance;
        }
    }

    [HideInInspector]
    public InventoryType inventoryType;// 当前打开的库存界面的类型
    
    [SerializeField]
    private GameObject inventoryItemPrefab;// 库存物品的预制体

    [Header("背包")]
    [SerializeField]
    private InventorySlot packageWeaponSlot;// 背包的武器槽
    [SerializeField]
    private InventorySlot packageArmorSlot;// 背包的防具槽
    [SerializeField]
    private GameObject packageMaterialSlotsParent;// 背包材料槽的父物体
    private List<InventorySlot> packageMaterialSlots;// 背包的材料槽列表
    [SerializeField]
    private GameObject packagePropSlotsParent;// 背包道具槽的父物体
    private List<InventorySlot> packagePropSlots;// 背包的道具槽列表
    [SerializeField]
    private GameObject packageEquipmentSlotsParent;// 背包装备槽的父物体
    private List<InventorySlot> packageEquipmentSlots;// 背包的装备槽列表

    [Header("村庄商店")]
    [SerializeField]
    private GameObject villageStoreSlotsParent;// 村庄商店单元槽的父物体
    private List<InventorySlot> villageStoreSlots;// 村庄商店的单元槽列表
    [SerializeField]
    private DialogConfig buyDialogConfig;
    [SerializeField]
    private DialogConfig sellDialogConfig;

    public int Coin { get; private set; }// 玩家持有的金币数

    [HideInInspector]
    public bool packageCanOpen = true;// 是否能够打开背包

    public int WeaponID
    {
        get
        {
            if (packageWeaponSlot.inventoryItem == null)
                return -1;
            else
                return packageWeaponSlot.inventoryItem.item.itemID;
        }
    }
    public int ArmorID
    {
        get
        {
            if (packageArmorSlot.inventoryItem == null)
                return -1;
            else
                return packageArmorSlot.inventoryItem.item.itemID;
        }
    }

    /// <summary>
    /// 背包物品内容存档
    /// </summary>
    /// <param name="itemType"></param>
    /// <returns></returns>
    public List<PackageItemData> GetPackageItemDataList(ItemType itemType)
    {
        List<InventorySlot> targetSlots = GetTargetSlotsByType(itemType);

        List<PackageItemData> itemDataList = new List<PackageItemData>(targetSlots.Count);
        foreach (InventorySlot slot in targetSlots)
        {
            if (slot.inventoryItem == null)
                itemDataList.Add(new PackageItemData(-1, 0));
            else
                itemDataList.Add(new PackageItemData(slot.inventoryItem.item.itemID, slot.inventoryItem.itemQuantity));
        }

        return itemDataList;
    }

    private void Start()
    {
        // TODO: 读档初始化背包内物品
        InitInventory();
        SetCoinQuantity(0);

        GameEventManager.Instance.dialogConfigEndEvent.AddListener((config) => { if (config == buyDialogConfig) VillageStoreActiveSwitch(); });
        GameEventManager.Instance.dialogConfigEndEvent.AddListener((config) => { if (config == sellDialogConfig) PackageActiveSwitch(sell: true); });
    }

    private void Update()
    {
        InventoryActiveSwitchInput();
    }
    
    /// <summary>
    /// 关闭所有Inventory界面
    /// </summary>
    public void CloseInventory()
    {
        if (GameUIManager.Instance.package.activeSelf) PackageActiveSwitch();
        if (GameUIManager.Instance.villageStore.activeSelf) VillageStoreActiveSwitch();
    }

    /// <summary>
    /// 通过按键输入开关背包和商店
    /// </summary>
    private void InventoryActiveSwitchInput()
    {
        if (Input.GetKeyDown(KeyCode.B) && packageCanOpen && !GameUIManager.Instance.villageStore.activeSelf)
        {
            PackageActiveSwitch();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (GameUIManager.Instance.package.activeSelf)
            {
                PackageActiveSwitch();
            }
            if (GameUIManager.Instance.villageStore.activeSelf)
            {
                VillageStoreActiveSwitch();
            }
        }
    }

    /// <summary>
    /// 背包界面开关切换(背包和出售功能均使用了背包界面)
    /// </summary>
    /// <param name="sell">是否为出售</param>
    public void PackageActiveSwitch(bool sell = false)
    {
        if (GameUIManager.Instance.package.activeSelf)
        {
            InventoryReset();
        }
        else
        {
            if (!sell)
            {
                inventoryType = InventoryType.Package;
                GameUIManager.Instance.packageTitle.text = "背包";
            }
            else
            {
                inventoryType = InventoryType.Sell;
                GameUIManager.Instance.packageTitle.text = "出售物品";
            }
        }
        GameUIManager.Instance.package.SetActive(!GameUIManager.Instance.package.activeSelf);
        GameUIManager.Instance.coinText.gameObject.SetActive(GameUIManager.Instance.package.activeSelf);

        if(GameUIManager.Instance.package.activeSelf)
        {
            PlayerInputManager.Instance.CloseControllInput(true);
        }
        else
        {
            PlayerInputManager.Instance.OpenAllInput();
            GameEventManager.Instance.closePackageEvent.Invoke();
        }
    }

    /// <summary>
    /// 村庄商店界面开关切换
    /// </summary>
    public void VillageStoreActiveSwitch()
    {
        if (GameUIManager.Instance.villageStore.activeSelf)
        {
            InventoryReset();
        }
        else
        {
            inventoryType = InventoryType.Buy;
        }
        GameUIManager.Instance.villageStore.SetActive(!GameUIManager.Instance.villageStore.activeSelf);
        GameUIManager.Instance.coinText.gameObject.SetActive(GameUIManager.Instance.villageStore.activeSelf);

        if (GameUIManager.Instance.villageStore.activeSelf)
        {
            PlayerInputManager.Instance.CloseControllInput(true);
        }
        else
        {
            PlayerInputManager.Instance.OpenAllInput();
        }
    }

    /// <summary>
    /// 获取物品库存单元槽
    /// </summary>
    /// <param name="itemType">物品类型</param>
    /// <returns></returns>
    private List<InventorySlot> GetTargetSlotsByType(ItemType itemType)
    {
        List<InventorySlot> targetSlots = null;

        if (itemType == ItemType.Weapon || itemType == ItemType.Armor)
        {
            targetSlots = packageEquipmentSlots;
        }
        else if (itemType == ItemType.Usable)
        {
            targetSlots = packagePropSlots;
        }
        else if (itemType == ItemType.Material)
        {
            targetSlots = packageMaterialSlots;
        }

        return targetSlots;
    }

    /// <summary>
    /// 在背包内查找物品
    /// </summary>
    /// <param name="item">要查找的物品</param>
    /// <returns>查找到的库存物品列表</returns>
    public List<InventoryItem> FindItemsInPackage(Item item)
    {
        List<InventoryItem> invItems = new List<InventoryItem>();
        List<InventorySlot> targetSlots = GetTargetSlotsByType(item.itemType);

        if (targetSlots != null)
        {
            foreach (InventorySlot invSlot in targetSlots)
            {
                if (invSlot.inventoryItem != null && invSlot.inventoryItem.item == item)
                {
                    invItems.Add(invSlot.inventoryItem);
                }
            }
        }
        return invItems;
    }

    /// <summary>
    /// 减少物品数量
    /// </summary>
    /// <param name="itemType">物品类型</param>
    /// <param name="maxCount">可减少的最大数量</param>
    /// <returns></returns>
    public int TryReduceItemsByType(ItemType itemType, int maxCount)
    {
        int count = 0;
        List<InventorySlot> targetSlots = GetTargetSlotsByType(itemType);

        if (targetSlots != null)
        {
            foreach (InventorySlot invSlot in targetSlots)
            {
                if (invSlot.inventoryItem != null && invSlot.inventoryItem.item.itemType == itemType)
                {
                    while (maxCount > 0 && invSlot.inventoryItem != null)
                    {
                        ReduceItemByOne(invSlot.inventoryItem);
                        count++;
                        maxCount--;
                    }

                    if (maxCount <= 0) break;
                }
            }
        }

        return count;
    }

    /// <summary>
    /// 判断玩家是否拥有一定数量的某物品
    /// </summary>
    /// <param name="item">要查找的物品</param>
    /// <param name="quantity">要查找的数量</param>
    /// <returns>玩家是否拥有大于等于该数量的该物品</returns>
    public bool HasItems(Item item, int quantity)
    {
        if (quantity <= 0) return true;
        List<InventoryItem> invItems = FindItemsInPackage(item);
        if (invItems == null || invItems.Count == 0) return false;

        if (item.itemType == ItemType.Weapon || item.itemType == ItemType.Armor)
        {
            if (invItems.Count >= quantity)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        else if (item.itemType == ItemType.Usable)
        {
            UsableItem usableItem = (UsableItem)item;
            if (usableItem.consumable)
            {
                if (invItems[0].itemQuantity >= quantity)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return true;
            }
        }
        else if (item.itemType == ItemType.Material)
        {
            if (invItems[0].itemQuantity >= quantity)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 扣除玩家一定数量的某物品, 数量不足则扣除失败
    /// </summary>
    /// <param name="item">要扣除的物品</param>
    /// <param name="quantity">要扣除的数量</param>
    /// <returns>是否扣除成功</returns>
    public bool ReduceItems(Item item, int quantity)
    {
        if (quantity <= 0) return true;
        List<InventoryItem> invItems = FindItemsInPackage(item);
        if (invItems == null || invItems.Count == 0) return false;

        if (item.itemType == ItemType.Weapon || item.itemType == ItemType.Armor)
        {
            if (invItems.Count >= quantity)
            {
                for (int i = 0; i < quantity; i++)
                {
                    DestroyInventoryItem(invItems[i]);
                }
            }
            else
            {
                return false;
            }
        }
        else if(item.itemType == ItemType.Usable)
        {
            UsableItem usableItem = (UsableItem)item;
            if (usableItem.consumable)
            {
                if (invItems[0].itemQuantity > quantity)
                {
                    invItems[0].SetQuantity(invItems[0].itemQuantity - quantity);
                }
                else if (invItems[0].itemQuantity == quantity)
                {
                    DestroyInventoryItem(invItems[0]);
                }
                else
                {
                    return false;
                }
            }
            else
            {
                DestroyInventoryItem(invItems[0]);
            }
        }
        else if (item.itemType == ItemType.Material)
        {
            if (invItems[0].itemQuantity > quantity)
            {
                invItems[0].SetQuantity(invItems[0].itemQuantity - quantity);
            }
            else if (invItems[0].itemQuantity == quantity)
            {
                DestroyInventoryItem(invItems[0]);
            }
            else
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 玩家获得单件物品, 背包空间不足则获得失败
    /// </summary>
    /// <param name="item">获得的物品</param>
    /// <returns>是否获得成功</returns>
    public bool AddItem(Item item)
    {
        if ((item.itemType == ItemType.Material && AddMaterial(packageMaterialSlots, item, 1)) ||
                (item.itemType == ItemType.Usable && AddProp(packagePropSlots, item, 1)) ||
                ((item.itemType == ItemType.Weapon || item.itemType == ItemType.Armor) && AddEquipment(packageEquipmentSlots, item)))
        {
            GameUIManager.Instance.ShowItemTip(item.itemName, 1);
            return true;
        }
        GameUIManager.Instance.messageTip.ShowTip("背包已满");
        return false;
    }

    /// <summary>
    /// 玩家购买单件物品, 金币不足或背包空间不足则购买失败
    /// </summary>
    /// <param name="item">购买的物品</param>
    /// <returns>是否购买成功</returns>
    public bool BuyItem(Item item)
    {
        bool success = false;
        if (Coin >= item.itemValue * 2)
        {
            success = AddItem(item);
            if (success)
            {
                SetCoinQuantity(Coin - item.itemValue * 2);
                GameUIManager.Instance.messageTip.ShowTip("购买成功");
            }
        }
        else
        {
            GameUIManager.Instance.messageTip.ShowTip("金币不足");
        }
        return success;
    }

    /// <summary>
    /// 玩家出售单件物品, 装备中的物品及无法出售的物品会导致出售失败
    /// </summary>
    /// <param name="invItem">出售的库存物品</param>
    /// <returns>是否出售成功</returns>
    public bool SellItem(InventoryItem invItem)
    {
        if (invItem.item.itemValue < 0)
        {
            GameUIManager.Instance.messageTip.ShowTip("该物品无法出售");
            return false;
        }
        if (invItem.inventorySlot.slotType == InventorySlotType.WeaponSlot || invItem.inventorySlot.slotType == InventorySlotType.ArmorSlot)
        {
            GameUIManager.Instance.messageTip.ShowTip("无法出售装备中的物品");
            return false;
        }

        ReduceItemByOne(invItem);
        AddCoin(invItem.item.itemValue);
        GameUIManager.Instance.messageTip.ShowTip("出售成功");

        GameEventManager.Instance.sellEvent.Invoke();

        return true;
    }

    /// <summary>
    /// 玩家使用单件物品
    /// </summary>
    /// <param name="invItem">使用的库存物品</param>
    public void UseItem(InventoryItem invItem)
    {
        if (invItem.item.itemType == ItemType.Usable)
        {
            UsableItem usableItem = (UsableItem)invItem.item;
            if (UsableItemEvent.instance.UsableItemInvoke(usableItem.itemEvent))
            {
                ReduceItemByOne(invItem);
                GameUIManager.Instance.messageTip.ShowTip("使用成功");
            }
        }
    }

    /// <summary>
    /// 扣除玩家一件物品
    /// </summary>
    /// <param name="invItem">扣除的库存物品</param>
    private void ReduceItemByOne(InventoryItem invItem)
    {
        if (invItem.itemQuantity > 1)
        {
            invItem.SetQuantity(invItem.itemQuantity - 1);
        }
        else
        {
            DestroyInventoryItem(invItem);
        }
    }

    /// <summary>
    /// 销毁某库存物品(该物品数量清0, 直接从背包中移除)
    /// </summary>
    /// <param name="invItem">销毁的库存物品</param>
    private void DestroyInventoryItem(InventoryItem invItem)
    {
        invItem.inventorySlot.inventoryItem = null;
        Destroy(invItem.gameObject);
        GameUIManager.Instance.itemDetails.CloseDetails();
    }

    /// <summary>
    /// 玩家获得金币
    /// </summary>
    /// <param name="quantity">获得的数量</param>
    public void AddCoin(int quantity)
    {
        GameUIManager.Instance.ShowItemTip("金币", quantity);
        SetCoinQuantity(Coin + quantity);
    }

    /// <summary>
    /// 设置玩家金币数量
    /// </summary>
    /// <param name="quantity">金币数量</param>
    private void SetCoinQuantity(int quantity)
    {
        Coin = quantity;
        if (Coin < 0) Coin = 0;

        GameUIManager.Instance.coinText.text = "金币: " + Coin.ToString();
    }

    /// <summary>
    /// 初始化库存
    /// </summary>
    private void InitInventory()
    {
        packageMaterialSlots = new List<InventorySlot>();
        packagePropSlots = new List<InventorySlot>();
        packageEquipmentSlots = new List<InventorySlot>();
        villageStoreSlots = new List<InventorySlot>();
        foreach (InventorySlot slot in packageMaterialSlotsParent.GetComponentsInChildren<InventorySlot>())
        {
            packageMaterialSlots.Add(slot);
        }
        foreach (InventorySlot slot in packagePropSlotsParent.GetComponentsInChildren<InventorySlot>())
        {
            packagePropSlots.Add(slot);
        }
        foreach (InventorySlot slot in packageEquipmentSlotsParent.GetComponentsInChildren<InventorySlot>())
        {
            packageEquipmentSlots.Add(slot);
        }
        foreach (InventorySlot slot in villageStoreSlotsParent.GetComponentsInChildren<InventorySlot>())
        {
            villageStoreSlots.Add(slot);
        }

        if (DataManager.Instance.loadSave)
        {
            SetCoinQuantity(DataManager.Instance.saveData.inventorySaveData.coin);

            for (int i = 0; i < packageMaterialSlots.Count; i++)
            {
                if (DataManager.Instance.saveData.inventorySaveData.materialItemDataList[i].itemID != -1)
                {
                    InstantiateInventoryItem(DataManager.Instance.itemConfig.FindItemByID(DataManager.Instance.saveData.inventorySaveData.materialItemDataList[i].itemID),
                        packageMaterialSlots[i], DataManager.Instance.saveData.inventorySaveData.materialItemDataList[i].itemQuantity);
                }
            }

            for (int i = 0; i < packagePropSlots.Count; i++)
            {
                if (DataManager.Instance.saveData.inventorySaveData.propItemDataList[i].itemID != -1)
                {
                    InstantiateInventoryItem(DataManager.Instance.itemConfig.FindItemByID(DataManager.Instance.saveData.inventorySaveData.propItemDataList[i].itemID),
                        packagePropSlots[i], DataManager.Instance.saveData.inventorySaveData.propItemDataList[i].itemQuantity);
                }
            }

            for (int i = 0; i < packageEquipmentSlots.Count; i++)
            {
                if (DataManager.Instance.saveData.inventorySaveData.equipmentItemDataList[i].itemID != -1)
                {
                    InstantiateInventoryItem(DataManager.Instance.itemConfig.FindItemByID(DataManager.Instance.saveData.inventorySaveData.equipmentItemDataList[i].itemID),
                        packageEquipmentSlots[i], DataManager.Instance.saveData.inventorySaveData.materialItemDataList[i].itemQuantity);
                }
            }

            if (DataManager.Instance.saveData.inventorySaveData.weaponID != -1)
            {
                EquipmentItem weaponItem = (EquipmentItem)DataManager.Instance.itemConfig.FindItemByID(DataManager.Instance.saveData.inventorySaveData.weaponID);
                InstantiateInventoryItem(weaponItem, packageWeaponSlot, 1);
                SetEquipmentItemUpdate(weaponItem);
            }

            if (DataManager.Instance.saveData.inventorySaveData.armorID != -1)
            {
                EquipmentItem armorItem = (EquipmentItem)DataManager.Instance.itemConfig.FindItemByID(DataManager.Instance.saveData.inventorySaveData.armorID);
                InstantiateInventoryItem(armorItem, packageArmorSlot, 1);
                SetEquipmentItemUpdate(armorItem);
            }
            PlayerInputManager.Instance.GetComponent<PlayerAttributes>().InitAttributes();
        }

        InitVillageStore();
    }

    /// <summary>
    /// 初始化村庄商店中售卖的物品
    /// </summary>
    private void InitVillageStore()
    {
        AddEquipment(villageStoreSlots, DataManager.Instance.itemConfig.FindItemByID(4001));
    }

    /// <summary>
    /// 添加装备, 单元槽满则添加失败
    /// </summary>
    /// <param name="slots">要添加到的单元槽列表</param>
    /// <param name="item">要添加的物品</param>
    /// <returns>是否添加成功</returns>
    private bool AddEquipment(List<InventorySlot> slots, Item item)
    {
        if (item == null || (item.itemType != ItemType.Armor && item.itemType != ItemType.Weapon))
        {
            throw new System.Exception("物品添加错误.");
        }

        foreach(InventorySlot slot in slots)
        {
            if (slot.inventoryItem == null)
            {
                InstantiateInventoryItem(item, slot, 1);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 添加道具, 单元槽满则添加失败
    /// </summary>
    /// <param name="slots">要添加到的单元槽列表</param>
    /// <param name="item">要添加的物品</param>
    /// <param name="quantity">要添加的数量</param>
    /// <returns>是否添加成功</returns>
    private bool AddProp(List<InventorySlot> slots, Item item, int quantity)
    {
        if (item == null || item.itemType != ItemType.Usable)
        {
            throw new System.Exception("物品添加错误.");
        }

        UsableItem usableItem = (UsableItem)item;
        InventorySlot firstEmptySlot = null;

        foreach (InventorySlot slot in slots)
        {
            if (slot.inventoryItem != null)
            {
                if (slot.inventoryItem.item == item)
                {
                    if (usableItem.consumable)
                    {
                        slot.inventoryItem.SetQuantity(slot.inventoryItem.itemQuantity + quantity);
                    }
                    return true;
                }
            }
            else if (firstEmptySlot == null)
            {
                firstEmptySlot = slot;
            }
        }

        if (firstEmptySlot)
        {
            if (usableItem.consumable)
                InstantiateInventoryItem(item, firstEmptySlot, quantity);
            else
                InstantiateInventoryItem(item, firstEmptySlot, 1);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 添加材料, 单元槽满则添加失败
    /// </summary>
    /// <param name="slots">要添加到的单元槽列表</param>
    /// <param name="item">要添加的物品</param>
    /// <param name="quantity">要添加的数量</param>
    /// <returns>是否添加成功</returns>
    private bool AddMaterial(List<InventorySlot> slots, Item item, int quantity)
    {
        if (item == null || item.itemType != ItemType.Material)
        {
            throw new System.Exception("物品添加错误.");
        }

        InventorySlot firstEmptySlot = null;

        foreach (InventorySlot slot in slots)
        {
            if (slot.inventoryItem != null)
            {
                if (slot.inventoryItem.item == item)
                {
                    slot.inventoryItem.SetQuantity(slot.inventoryItem.itemQuantity + quantity);
                    return true;
                }
            }
            else if (firstEmptySlot == null)
            {
                firstEmptySlot = slot;
            }
        }

        if (firstEmptySlot)
        {
            InstantiateInventoryItem(item, firstEmptySlot, quantity);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 实例化一个库存物品
    /// </summary>
    /// <param name="item">库存物品中包含的物品</param>
    /// <param name="slot">库存物品所属的单元槽</param>
    /// <param name="quantity">物品的数量</param>
    private void InstantiateInventoryItem(Item item, InventorySlot slot, int quantity)
    {
        InventoryItem invItem = Instantiate(inventoryItemPrefab, slot.transform).GetComponent<InventoryItem>();
        invItem.Init(item, slot, quantity);
        slot.inventoryItem = invItem;
    }

    /// <summary>
    /// 库存界面重置(重置拖动中的物品, 关闭物品详细描述)
    /// </summary>
    private void InventoryReset()
    {
        if (GameUIManager.Instance.draggingItemParent.childCount > 0)
        {
            InventoryItem draggingItem = GameUIManager.Instance.draggingItemParent.GetChild(0).GetComponent<InventoryItem>();
            if (draggingItem) draggingItem.DraggingItemReset();
        }
        GameUIManager.Instance.itemDetails.CloseDetails();
    }

    private void SetEquipmentItemUpdate(EquipmentItem item)
    {
        PlayerAttributes playerAttributes = FindObjectOfType<PlayerAttributes>();

        playerAttributes.Constitution += item.constitution;
        playerAttributes.Strength += item.strength;
        playerAttributes.Intelligence += item.intelligence;
        if (item.itemType == ItemType.Weapon) FindObjectOfType<PlayerCombatController>().SwitchWeapon(item.itemID);
    }
}
