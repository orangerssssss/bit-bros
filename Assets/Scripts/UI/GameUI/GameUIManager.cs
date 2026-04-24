using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 游戏UI管理器, 方便对各UI的调用
/// </summary>
public class GameUIManager : MonoBehaviour
{
    public static event System.Action OnUIReady;
    private static GameUIManager instance;

    public static GameUIManager Instance
    {
        get
        {
            if (instance == null) instance = GameObject.FindObjectOfType<GameUIManager>();
            return instance;
        }
    }

    [Header("玩家状态")]
    public Text level;// 玩家等级文本
    public Text healthText;// 玩家血量文本
    public Slider healthBar;// 玩家快血条
    public Slider healthBarSlow;// 玩家慢血条
    public Slider expBar;// 玩家经验条
    public Text expText;
    public Slider manaBar;
    public Text manaText;

    [Header("交互")]
    public GameObject interact;// 交互提示父物体
    public Text interactTextLabel;// 交互显示文本

    [Header("对话")]
    public GameObject dialog;// 对话框父物体
    public Button dialogNextButton;// 继续对话按钮
    public Transform optionLabel;// 选项父物体
    public Text dialogNameTextLabel;// 对话物名称文本
    public Text dialogTextLabel;// 对话内容文本
    public GameObject dialogContinueTip;// 对话继续提示
    public Button dialogSkipButton;

    [Header("背包/商店")]
    public GameObject package;// 背包父物体
    public Text packageTitle;// 背包名(偷懒将背包与出售商店共用了同一套界面, 因此背包名可能有所变动: 背包或者出售)
    public Transform draggingItemParent;// 拖动中物体的父物体
    public GameObject villageStore;// 村庄商店父物体
    public InventoryItemDetails itemDetails;// 物品详细描述
    public Text coinText;// 玩家拥有金币文本

    [Header("获得物品提示")]// TODO: 此部分代码逻辑可以新建类单独管理
    [SerializeField] private Transform itemTipsParent;// 获得物品提示父物体
    [SerializeField] private GameObject itemTipPrefab;// 获得物品提示预制体
    [SerializeField] private int initItemTipQuantity = 8;// 获得物品提示对象池单次实例化数量
    private Queue<ItemTip> itemTipsPool = new Queue<ItemTip>();// 获得物品提示池

    [Header("消息提示")]
    public MessageTip messageTip;// 消息提示

    [Header("任务提示")]
    public MainTaskTip mainTaskTip;// 任务提示

    [Header("操作提示")]
    public ControlTip controlTip;// 操作提示

    [Header("玩家受伤/死亡")]
    public DamagedUI damagedUI;// 玩家受伤UI
    public DiedUI diedUI;// 玩家死亡UI

    [Header("目的地标识")]
    public DestinationMark destinationMark;
    public DestinationMark sideDestinationMark0;
    public DestinationMark sideDestinationMark1;

    [Header("关卡")]
    public GameObject levelEndPanel;

    private void Awake()
    {
        InitItemTips();
        // Notify listeners that the UI manager has been initialized
        try
        {
            OnUIReady?.Invoke();
            Debug.Log("GameUIManager: Awake complete, OnUIReady invoked.");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("GameUIManager: exception invoking OnUIReady: " + e.Message);
        }
    }

    /// <summary>
    /// 实例化对应数量的获得物品提示
    /// </summary>
    private void InitItemTips()
    {
        for (int i = 0; i < initItemTipQuantity; i++)
        {
            itemTipsPool.Enqueue(Instantiate(itemTipPrefab, itemTipsParent).GetComponent<ItemTip>());
        }
    }

    /// <summary>
    /// 显示获得物品提示并出池
    /// </summary>
    /// <param name="name">物品名</param>
    /// <param name="quantity">获得数量</param>
    public void ShowItemTip(string name, int quantity)
    {
        if (itemTipsPool.Count == 0)
        {
            InitItemTips();
        }
        itemTipsPool.Dequeue().Show(name, quantity);
    }

    /// <summary>
    /// 获得物品提示入池
    /// </summary>
    public void ItemTipEnqueue(ItemTip itemTip)
    {
        itemTipsPool.Enqueue(itemTip);
    }

    public void CloseAllWindow()
    {
        if (GameMenu.Instance.menuPanel.activeSelf) 
            GameMenu.Instance.CloseMenu();
        if (package.activeSelf) 
            InventoryManager.Instance.PackageActiveSwitch();
        if (villageStore.activeSelf) 
            InventoryManager.Instance.VillageStoreActiveSwitch();
        DialogDisplayer.Instance.ExitDialog();
    }
}
