using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 库存物品类(背包 商店等), 包括悬浮 拖动 点击等事件中的功能
/// </summary>
public class InventoryItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private Camera uiCamera;// UI相机
    [SerializeField]
    private Transform itemParent;// 物品模型的父物体
    [SerializeField]
    private Text quantityTextLabel;// 物品数量文本

    private Vector3 offset;// 拖动偏移量, 用于物品拖动
    private int forwardOffset = 1;// 前偏移量, 保证正确的物体间遮挡关系

    [HideInInspector]
    public InventorySlot inventorySlot = null;// 该物品属于的库存单元槽
    [HideInInspector]
    public Item item = null;// 该库存物品中包含的物品数据
    [HideInInspector]
    public int itemQuantity;// 物品的数量
    [HideInInspector]
    public bool dropLegal = false;// 避免拖动时开关界面出现的问题

    private void Awake()
    {
        uiCamera = GameObject.FindGameObjectWithTag("UICamera").GetComponent<Camera>();
    }

    /// <summary>
    /// 该函数在开始拖动的那一帧被调用
    /// </summary>
    /// <param name="eventData">光标事件</param>
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (InventoryManager.Instance.inventoryType == InventoryType.Buy) return;
        if (eventData.button != PointerEventData.InputButton.Left) return;
        transform.position = offset + uiCamera.ScreenToWorldPoint(eventData.position);
        transform.SetParent(GameUIManager.Instance.draggingItemParent);
        GetComponent<CanvasGroup>().blocksRaycasts = false;

        dropLegal = true;
    }

    /// <summary>
    /// 该函数在拖动时被调用
    /// </summary>
    /// <param name="eventData">光标事件</param>
    public void OnDrag(PointerEventData eventData)
    {
        if (InventoryManager.Instance.inventoryType == InventoryType.Buy) return;
        if (eventData.button != PointerEventData.InputButton.Left) return;
        transform.position = offset + uiCamera.ScreenToWorldPoint(eventData.position);// 跟随光标移动
    }

    /// <summary>
    /// 该函数在结束拖动的那一帧调用
    /// </summary>
    /// <param name="eventData">光标事件</param>
    public void OnEndDrag(PointerEventData eventData)
    {
        if (InventoryManager.Instance.inventoryType == InventoryType.Buy) return;
        if (eventData.button != PointerEventData.InputButton.Left) return;

        // 如果父物体为draggingItemParent则返回原单元槽(OnDrop事件(InventorySlot有相关逻辑)先于OnEndDrag, 如果被放置在其他槽中则父物体已经被改变)
        if (transform.parent == GameUIManager.Instance.draggingItemParent)
        {
            transform.SetParent(inventorySlot.transform);
            transform.localPosition = Vector3.zero;
        }

        GetComponent<CanvasGroup>().blocksRaycasts = true;
        dropLegal = false;
    }

    /// <summary>
    /// 该函数在鼠标光标点下时被调用
    /// </summary>
    /// <param name="eventData">光标事件</param>
    public void OnPointerDown(PointerEventData eventData)
    {
        offset = transform.position - uiCamera.ScreenToWorldPoint(eventData.position) - forwardOffset * Vector3.forward;
    }

    /// <summary>
    /// 该函数在鼠标光标进入时被调用
    /// </summary>
    /// <param name="eventData">光标事件</param>
    public void OnPointerEnter(PointerEventData eventData)
    {
        GameUIManager.Instance.itemDetails.ShowDetails(item);
    }

    /// <summary>
    /// 该函数在鼠标光标离开时被调用
    /// </summary>
    /// <param name="eventData">光标事件</param>
    public void OnPointerExit(PointerEventData eventData)
    {
        GameUIManager.Instance.itemDetails.CloseDetails();
    }

    /// <summary>
    /// 该函数在鼠标光标点击时被调用
    /// </summary>
    /// <param name="eventData">光标事件</param>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right) 
            return;

        if (InventoryManager.Instance.inventoryType == InventoryType.Buy)
        {
            InventoryManager.Instance.BuyItem(item);
        }
        else if (InventoryManager.Instance.inventoryType == InventoryType.Sell)
        {
            InventoryManager.Instance.SellItem(this);
        }
        else if (InventoryManager.Instance.inventoryType == InventoryType.Package)
        {
            InventoryManager.Instance.UseItem(this);
        }
    }

    /// <summary>
    /// 设置物品数量
    /// </summary>
    /// <param name="quantity">数量</param>
    public void SetQuantity(int quantity)
    {
        itemQuantity = Mathf.Clamp(quantity, 0, 99999);
        if (quantity < 2)
            quantityTextLabel.text = "";
        else
            quantityTextLabel.text = quantity.ToString();
    }

    /// <summary>
    /// 初始化该库存物品的数据
    /// </summary>
    /// <param name="itemData">物品数据</param>
    /// <param name="slot">所属单元槽</param>
    /// <param name="quantity">数量</param>
    public void Init(Item itemData, InventorySlot slot, int quantity)
    {
        item = itemData;
        inventorySlot = slot;
        SetQuantity(quantity);
        GameObject itemObj = Instantiate(item.itemPrefab, itemParent);
        itemObj.layer = 5;// UI层, 否则不会被UI相机渲染
        // TODO: 此处为临时使用, 应更改为根据物品不同赋予不同的位置 旋转 大小, 使其以正常显示在单元槽内
        itemObj.transform.localScale *= 40;
    }

    /// <summary>
    /// 在开关Inventory界面时被调用, 重置使该物体回到原单元槽内
    /// </summary>
    public void DraggingItemReset()
    {
        transform.SetParent(inventorySlot.transform);
        transform.localPosition = Vector3.zero;
        GetComponent<CanvasGroup>().blocksRaycasts = true;
        dropLegal = false;
    }
}
