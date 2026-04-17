using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Playables;

/// <summary>
/// 主故事线, 包括故事进度的推进及各种故事需要的特殊函数
/// </summary>
public class MainSceneStory : MonoBehaviour
{
    private static MainSceneStory instance;// 单例

    public static MainSceneStory Instance
    {
        get
        {
            if (instance == null) instance = GameObject.FindObjectOfType<MainSceneStory>();
            return instance;
        }
    }

    public int storyProcess = 0;

    public Image blackImage;// 黑色图, 用于过场

    public StorySetting storySetting;// 开始游戏时的故事设定展示

    // 此部分为故事中涉及的各种物体 对话等, 用于在代码中进行控制
    [Header("对话文件")]
    public DialogConfig dialog_0_0;
    public DialogConfig dialog_1_0;
    public DialogConfig dialog_2_0;
    public DialogConfig dialog_2_1;
    public DialogConfig dialog_2_2;
    public DialogConfig dialog_3_0;
    public DialogConfig dialog_4_0;
    public DialogConfig dialog_5_0;
    public DialogConfig dialog_5_1;
    public DialogConfig dialog_5_1_0;
    public DialogConfig dialog_5_1_1;
    public DialogConfig dialog_5_1_2;
    public DialogConfig dialog_5_2;
    public DialogConfig dialog_5_3;
    public DialogConfig dialog_5_4;
    public DialogConfig dialog_6_0;
    public DialogConfig dialog_7_0;
    public DialogConfig dialog_8_0;
    public DialogConfig dialogTrade;

    [Header("对话物")]
    public DialogObject merlinNPC;
    public DialogObject startVillagerNPC;
    public DialogObject ectorNPC;
    public DialogObject tradeNPC;
    public DialogObject hunterNPC;
    public DialogObject noticeMerlinNPC;
    public DialogObject weaponRack;
    public DialogObject beforeInvasionVillagerNPC;
    public DialogObject invasionEndChat;

    [Header("战斗角色")]
    public List<FightAttributes> invasionEnemies;
    public List<FightAttributes> invasionVillagers;

    [Header("位置")]
    public Transform beforeReportPosition;
    public Transform beforeInvasionPosition;
    public Transform invasionEndChatPoint;
    public Transform caveEntry;
    public Transform caveExit;

    [Header("物体")]
    public GameObject npc_Merlin;
    public GameObject firstWeapon;
    public GameObject merlinHunt;
    public GameObject noticeNPCs;
    public GameObject villageFire;
    public GameObject villageNPCs;
    public GameObject invasionFightVillager;
    public GameObject villagerNonCombat;
    public GameObject invasionSupport;
    public GameObject invasionDrops;
    public GameObject levelEnd;
    public GameObject cemeteryNPCs;
    public GameObject caveTriggerPoint;

    [Header("计时器")]
    public StoryTimer storyTimer;
    public float collectWeaponSeconds = 600.0f;

    [Header("祭坛")]
    public DialogConfig sideDialog_0_0;
    public DialogConfig sideDialog_0_2;

    public Transform mark_side0;

    [Header("入口营地")]
    public DialogConfig sideDialog_1_0;
    public DialogConfig sideDialog_1_1;
    public DialogConfig sideDialog_1_2;
    public DialogObject sideSoldierNPC;
    public FightAttributes sideSoldierFight;
    public Transform sideSoldierNPCPosition;
    public Transform mark_side1;

    [Header("过场动画")]
    public PlayableDirector timeline_7_noticeLeave;
    public PlayableDirector timeline_9_invasionEnd;

    [Header("标识")]
    public Transform mark_caveExit;
    public Transform mark_caveEntry;
    public Transform mark_merilin;
    public Transform mark_ector;
    public Transform mark_firstWeapon;
    public Transform mark_hunter;
    public Transform mark_hunt;
    public Transform mark_notice;
    public Transform mark_noticeMerlin;
    public Transform mark_weaponRack;
    public Transform mark_invasion;
    public Transform mark_invasionDrops;
    public Transform mark_villageTrade;
    public Transform mark_villagePath;

    private StoryListener storyListener = new StoryListener();
    // When false, story dialogs won't be auto-triggered by pick-up events.
    // Set true only if you want the original auto-dialog behavior.
    public bool autoPickupStoryDialog = false;
    [HideInInspector] public int pickMealCount = 3;
    [HideInInspector] public int playerDeathCount = 1;
    [HideInInspector] public int enemyDeathCount = 5;
    [HideInInspector] public int dropItemCount = 5;
    [HideInInspector] public int requiredWeaponCount = 4;
    [HideInInspector] public int submitWeaponCount = 0;

    [SerializeField] private AudioSource storyAudioSource;
    [SerializeField] private List<AudioClip> bgms;
    private AudioClip villageBGM;

    private void Start()
    {
        if (DataManager.Instance.loadSave)
        {
            storyProcess = DataManager.Instance.saveData.gameProcessSaveData.storyProcess;
        }
        else
        {
#if !UNITY_EDITOR
            storyProcess = 0;
#endif
        }

        UpdateStory();

        AddSideEvent();
    }

    private void AddSideEvent()
    {
        // 祭坛
        GameEventManager.Instance.dialogConfigEndEvent.AddListener(storyListener.SideStory0_0);

        // 入口士兵
        GameEventManager.Instance.dialogConfigEndEvent.AddListener(storyListener.SideStory1_0);
        GameEventManager.Instance.characterBeforeDeathEvent.AddListener(storyListener.SideStory1_1);
    }

    /// <summary>
    /// 更新故事进度
    /// </summary>
    private void UpdateStory()
    {
        switch (storyProcess)
        {
            case 0:// 背景
                storySetting.Show();
                SetAndPlayVillageBGM(0);

                GameEventManager.Instance.storySettingEndEvent.AddListener(storyListener.StoryProcess0_0);
                break;
            case 1://进入山洞
                GameUIManager.Instance.mainTaskTip.UpdateTask("探索山洞", "进入山洞深处，找到可以探索的位置。");
                if (caveTriggerPoint != null) caveTriggerPoint.SetActive(true);
                if (mark_caveEntry != null) GameUIManager.Instance.destinationMark.SetTarget(mark_caveEntry);
                break;

            case 2:// 和 Merlin 对话
                GameUIManager.Instance.mainTaskTip.UpdateTask("和陌生人对话", "那里似乎站着一个神秘的陌生人，去问问他是谁");

                if (npc_Merlin != null) npc_Merlin.SetActive(true);
                merlinNPC.AddSpecialDialog(dialog_2_0);
                GameUIManager.Instance.destinationMark.SetTarget(mark_merilin);

                GameEventManager.Instance.dialogConfigEndEvent.AddListener(storyListener.StoryProcess2_0);
                break;
            case 3:// 前进
                GameUIManager.Instance.mainTaskTip.UpdateTask("继续前进", "拿起旁边的剑，到达山洞出口。");
                firstWeapon.SetActive(true);
                if (mark_caveExit != null) GameUIManager.Instance.destinationMark.SetTarget(mark_caveExit);




                break;

        }
    }

    /// <summary>
    /// 推进故事进度并更新
    /// </summary>
    /// <param name="nextProcess">下一进度</param>
    public void DriveProcess()
    {
        storyProcess++;
        UpdateStory();

        DataManager.Instance.SaveGame();
        GameUIManager.Instance.messageTip.ShowTip("游戏进度已保存");
    }

    /// <summary>
    /// 黑屏过场
    /// </summary>
    public void Black()
    {
        StopCoroutine("BlackIEnum");
        StartCoroutine("BlackIEnum");
    }

    /// <summary>
    /// 黑屏过场动画控制
    /// </summary>
    private IEnumerator BlackIEnum()
    {
        Color color = Color.black;
        color.a = 0;
        blackImage.color = color;

        while (color.a < 1.0f)
        {
            color.a += Time.deltaTime * 2.5f;
            blackImage.color = color;
            yield return null;
        }

        yield return new WaitForSeconds(0.4f);

        while (color.a > 0)
        {
            color.a -= Time.deltaTime * 2.5f;
            blackImage.color = color;
            yield return null;
        }
    }

    /// <summary>
    /// 与目标交谈, 用于在Timeline中进行事件配置
    /// </summary>
    /// <param name="dialogObject">目标</param>
    public void Chat(DialogObject dialogObject)
    {
        if (!dialogObject.gameObject.activeSelf) dialogObject.gameObject.SetActive(true);
        dialogObject.Interact();
    }

    /// <summary>
    /// 开关玩家的输入, 用于在Timeline中进行事件配置
    /// </summary>
    /// <param name="active">是否开启</param>
    public void PlayerInputActive(bool active)
    {
        if (!active) GameMenu.Instance.CloseMenu();
        InventoryManager.Instance.CloseInventory();

        if (active) PlayerInputManager.Instance.OpenAllInput();
        else PlayerInputManager.Instance.CloseAllInput(false);

    }

    /// <summary>
    /// 设置玩家位置, 用于在Timeline中进行事件配置
    /// </summary>
    /// <param name="pos">位置</param>
    public void SetPlayerPosition(Transform pos)
    {
        PlayerInputManager.Instance.moveController.SetPositionAndRotation(pos);
    }

    public void PlayVillageBGM()
    {
        if (villageBGM == null) villageBGM = storyAudioSource.clip;

        storyAudioSource.Stop();
        storyAudioSource.clip = villageBGM;
        storyAudioSource.Play();
    }

    public void SetAndPlayVillageBGM(int index)
    {
        villageBGM = bgms[index];

        storyAudioSource.Stop();
        storyAudioSource.clip = villageBGM;
        storyAudioSource.Play();
    }

    public void PlayFightBGM()
    {
        storyAudioSource.Stop();
        storyAudioSource.clip = bgms[2];
        storyAudioSource.Play();
    }

    public void CompleteCaveTask()
    {
        if (storyListener != null)
            storyListener.OnCaveEntered();
        else
            DriveProcess(); // 直接推进，不经过 listener
    }

    public void CompleteExitTask()
    {
        if (storyListener != null)
            storyListener.OnExitReached();
        else
            DriveProcess();
    }
}
