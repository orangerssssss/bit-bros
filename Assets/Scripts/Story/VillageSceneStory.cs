using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 村庄救援故事线, 包括故事进度的推进及各种故事需要的特殊函数
/// </summary>
public class VillageSceneStory : MonoBehaviour
{
    private static VillageSceneStory instance;// 单例

    public static VillageSceneStory Instance
    {
        get
        {
            if (instance == null) instance = GameObject.FindObjectOfType<VillageSceneStory>();
            return instance;
        }
    }

    public int storyProcess = 0;

    public Image blackImage;// 黑色图, 用于过场

    // 此部分为故事中涉及的各种物体、对话等，用于在代码中进行控制

    [Header("对话文件")]
    public DialogConfig dialog_3_0;       // 陌生士兵 - 介绍背景信息
    public DialogConfig dialog_3_1;       // 格雷斯 - 最后的对话
    public DialogConfig dialog_3_board;       //木板 - 交互
    [Header("对话物")]
    public DialogObject strangerSoldierNPC;     // 陌生士兵
    public DialogObject garethNPC;               // 格雷斯
    [Header("战斗角色")]
    public List<FightAttributes> villageEnemies;    // 村庄中的敌人列表
    public FightAttributes garethLeader;             // 格雷斯
    public DialogObject boardDialogObject;       // 板子对话物

    [Header("位置")]
    public Transform villageEntrancePosition;   // 村庄入口位置
    public Transform villageExitPosition;       // 村庄出口位置
    public Transform strangerSoldierPosition;   // 陌生士兵位置
    public Transform garethPosition;             // 格雷斯位置

    [Header("物体")]
    public GameObject strangerSoldier;          // 陌生士兵GameObject
    public GameObject gareth;                    // 格雷斯GameObject
    public GameObject villageEnemySpawners;     // 敌人生成点
    public GameObject villageTriggerPoint;      // 村庄入口触发点

    [Header("标识")]
    public Transform mark_strangerSoldier;      // 陌生士兵标记
    public Transform mark_villageEntrance;      // 村庄入口标记
    public Transform mark_gareth;                // 格雷斯标记

    private StoryListener storyListener = new StoryListener();
    public bool autoPickupStoryDialog = false;

    [HideInInspector] public int villageEnemyDeathCount;    // 需要击杀的敌人数量
    [HideInInspector] public int villageEnemyKilledCount;   // 已击杀的敌人数量

    private void Start()
    {
        // 加载游戏进度（如果有存档）
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

        // 初始化敌人计数
        if (villageEnemies != null)
        {
            villageEnemyDeathCount = villageEnemies.Count;
            villageEnemyKilledCount = 0;
        }


        UpdateStory();
    }



    /// <summary>
    /// 更新故事进度
    /// </summary>
    private void UpdateStory()
    {
        switch (storyProcess)
        {
            case 0: // 任务1：和前方的陌生人聊一聊 - 初始状态，出山洞
                GameUIManager.Instance.mainTaskTip.UpdateTask("和前方的陌生人聊一聊", "那里站着一个陌生的士兵，去和他交谈吧。");

                // 激活陌生士兵
                if (strangerSoldier != null)
                {
                    strangerSoldier.SetActive(true);
                }

                // 添加对话配置
                if (strangerSoldierNPC != null && dialog_3_0 != null)
                {
                    strangerSoldierNPC.AddSpecialDialog(dialog_3_0);
                    GameUIManager.Instance.destinationMark.SetTarget(mark_strangerSoldier);
                }

                // 注册对话结束事件
                GameEventManager.Instance.dialogConfigEndEvent.AddListener(storyListener.StoryProcess3_0);
                break;

            case 1: // 任务2：进入村庄，到达村庄门口trigger
                GameUIManager.Instance.mainTaskTip.UpdateTask("进入村庄", "前往村庄，帮助格雷斯");

                // 激活村庄入口触发点
                if (villageTriggerPoint != null)
                {
                    villageTriggerPoint.SetActive(true);
                }

                // 设置目标标记
                if (mark_villageEntrance != null)
                {
                    GameUIManager.Instance.destinationMark.SetTarget(mark_villageEntrance);
                }
                break;

            case 2: // 任务3：斩杀一切阻碍，为了拯救世界
                GameUIManager.Instance.mainTaskTip.UpdateTask("斩杀一切", "消灭村庄中的所有被黑魔法控制的村民，让他们安息。");

                // 激活敌人生成点
                if (villageEnemySpawners != null)
                {
                    villageEnemySpawners.SetActive(true);
                }

                // 在此处可以根据需要生成敌人或触发战斗逻辑
                break;

            case 3: // 任务4：和即将死亡的格雷斯聊聊
                GameUIManager.Instance.mainTaskTip.UpdateTask("和格雷斯聊聊", "去和伤痕累累的格雷斯交谈。");

                // 激活格雷斯
                if (gareth != null)
                {
                    gareth.SetActive(true);
                }

                // 添加格雷斯的对话
                if (garethNPC != null && dialog_3_1 != null)
                {
                    garethNPC.AddSpecialDialog(dialog_3_1);
                    GameUIManager.Instance.destinationMark.SetTarget(mark_gareth);
                }

                // 注册对话结束事件
                GameEventManager.Instance.dialogConfigEndEvent.AddListener(storyListener.StoryProcess3_1);
                break;

            case 4: // 任务5：继续前进，我只要我需要的东西
                GameUIManager.Instance.mainTaskTip.UpdateTask("继续前进", "离开村庄，踏上新的冒险。");

                // 设置出口标记
                if (mark_villageEntrance != null)
                {
                    GameUIManager.Instance.destinationMark.SetTarget(mark_villageEntrance);
                }
                break;
        }
    }

    /// <summary>
    /// 推进故事进度并更新
    /// </summary>
    public void DriveProcess()
    {
        storyProcess++;
        UpdateStory();

        // 保存游戏进度（容错：保存失败不应中断剧情推进）
        try
        {
            var dm = DataManager.Instance;
            if (dm != null && dm.saveData != null)
            {
                dm.SaveGame();
                if (GameUIManager.Instance != null && GameUIManager.Instance.messageTip != null)
                {
                    GameUIManager.Instance.messageTip.ShowTip("游戏进度已保存");
                }
            }
            else
            {
                Debug.LogWarning("VillageSceneStory: 跳过保存，DataManager 或 saveData 未初始化。");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"VillageSceneStory: SaveGame 失败但剧情继续。原因: {e.Message}");
        }
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
    public void Chat(DialogObject dialogObject)
    {
        if (!dialogObject.gameObject.activeSelf) dialogObject.gameObject.SetActive(true);
        dialogObject.Interact();
    }

    /// <summary>
    /// 开关玩家的输入, 用于在Timeline中进行事件配置
    /// </summary>
    public void PlayerInputActive(bool active)
    {
        if (!active)
        {
            if (GameMenu.Instance != null) GameMenu.Instance.CloseMenu();
            if (InventoryManager.Instance != null) InventoryManager.Instance.CloseInventory();
        }

        if (active)
            PlayerInputManager.Instance.OpenAllInput();
        else
            PlayerInputManager.Instance.CloseAllInput(false);
    }

    /// <summary>
    /// 设置玩家位置, 用于在Timeline中进行事件配置
    /// </summary>
    public void SetPlayerPosition(Transform pos)
    {
        if (PlayerInputManager.Instance != null && PlayerInputManager.Instance.moveController != null)
        {
            PlayerInputManager.Instance.moveController.SetPositionAndRotation(pos);
        }
    }

    /// <summary>
    /// 记录敌人被击杀
    /// </summary>
    public void OnEnemyKilled()
    {
        villageEnemyKilledCount++;
        Debug.Log($"VillageSceneStory: Enemy killed. Progress: {villageEnemyKilledCount}/{villageEnemyDeathCount}");

        // 如果所有敌人都被击杀，推进故事进度
        if (villageEnemyKilledCount >= villageEnemyDeathCount)
        {
            Debug.Log("VillageSceneStory: All enemies defeated! Moving to next stage.");
            DriveProcess();
        }
    }

    /// <summary>
    /// 任务2：到达村庄入口触发点时调用，推进到任务3。
    /// </summary>
    public void OnVillageEntranceReached()
    {
        if (storyProcess != 1) return;

        Debug.Log("VillageSceneStory: 到达村庄入口，任务2完成，推进到任务3。");
        if (villageTriggerPoint != null)
        {
            villageTriggerPoint.SetActive(false);
        }

        DriveProcess();
    }
}
