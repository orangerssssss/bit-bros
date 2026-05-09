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
    public DialogConfig dialog_3_1;       // 加雷斯 - 最后的对话
    public DialogConfig dialog_3_2;       // 梅林 - 显现后的对话
    public DialogConfig dialog_3_2_common; // 梅林 - 常驻对话
    public DialogConfig dialog_3_board;       //木板 - 交互
    [Header("对话物")]
    public DialogObject strangerSoldierNPC;     // 陌生士兵
    public DialogObject garethNPC;               // 加雷斯
    public DialogObject merlinNPC;               // 梅林
    [Header("战斗角色")]
    public List<FightAttributes> villageEnemies;    // 村庄中的敌人列表
    public FightAttributes garethLeader;             // 加雷斯
    public DialogObject boardDialogObject;       // 板子对话物

    [Header("位置")]
    public Transform villageEntrancePosition;   // 村庄入口位置
    public Transform villageExitPosition;       // 村庄出口位置
    public Transform strangerSoldierPosition;   // 陌生士兵位置
    public Transform garethPosition;             // 加雷斯位置

    [Header("物体")]
    public GameObject strangerSoldier;          // 陌生士兵GameObject
    public GameObject gareth;                    // 加雷斯GameObject
    public GameObject merlin;                    // 梅林GameObject
    public GameObject villageEnemySpawners;     // 敌人生成点
    public GameObject villageTriggerPoint;      // 村庄入口触发点

    [Header("标识")]
    public Transform mark_strangerSoldier;      // 陌生士兵标记
    public Transform mark_villageEntrance;      // 村庄入口标记
    public Transform mark_gareth;                // 加雷斯标记
    public Transform mark_merlin;                // 梅林标记
    public Transform mark_exit_gate;          // 出口大门标记

    private StoryListener storyListener = new StoryListener();
    public bool autoPickupStoryDialog = false;

    [HideInInspector] public int villageEnemyDeathCount;    // 需要击杀的敌人数量
    [HideInInspector] public int villageEnemyKilledCount;   // 已击杀的敌人数量
    private Coroutine villageEntranceCheckCoroutine;
    private Coroutine villageExitCheckCoroutine;
    private bool villageBattleBuffApplied = false;

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

        if (storyProcess < 4 && merlin != null)
        {
            merlin.SetActive(false);
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
                GameUIManager.Instance.mainTaskTip.UpdateTask("进入村庄", "前往村庄，帮助加雷斯");

                // 激活村庄入口触发点
                if (villageTriggerPoint != null)
                {
                    villageTriggerPoint.SetActive(true);
                }

                // 设置目标标记
                Transform villageEntranceTarget = ResolveVillageEntranceTarget();
                if (villageEntranceTarget != null)
                {
                    GameUIManager.Instance.destinationMark.SetTarget(villageEntranceTarget);
                }

                StartVillageEntranceCheck();
                break;

            case 2: // 任务3：斩杀一切阻碍，为了拯救世界
                GameUIManager.Instance.mainTaskTip.UpdateTask("拔除一切", "消灭村庄中被黑魔法控制的村民，让他们安息。");

                // 激活敌人生成点
                if (villageEnemySpawners != null)
                {
                    villageEnemySpawners.SetActive(true);
                }

                Transform villageExitTarget = ResolveVillageExitTarget();
                if (villageExitTarget != null)
                {
                    GameUIManager.Instance.destinationMark.SetTarget(villageExitTarget);
                }

                ApplyVillageBattleBuff();
                StartVillageExitCheck();
                break;

            case 3: // 任务4：和即将死亡的加雷斯聊聊
                GameUIManager.Instance.mainTaskTip.UpdateTask("和加雷斯聊聊", "去和奄奄一息的加雷斯交谈。");

                if (merlin != null)
                {
                    merlin.SetActive(false);
                }

                // 激活格雷斯
                if (gareth != null)
                {
                    gareth.SetActive(true);
                }

                // 添加加雷斯的对话
                if (garethNPC != null && dialog_3_1 != null)
                {
                    garethNPC.AddSpecialDialog(dialog_3_1);
                    GameUIManager.Instance.destinationMark.SetTarget(mark_gareth);
                }

                // 注册对话结束事件
                GameEventManager.Instance.dialogConfigEndEvent.AddListener(storyListener.StoryProcess3_1);
                break;

            case 4: // 任务5：继续前进，梅林显现为可选对话
                GameUIManager.Instance.mainTaskTip.UpdateTask("继续前进", "真相就在前方，前往出口大门。");

                if (merlin != null)
                {
                    merlin.SetActive(true);
                }

                if (merlinNPC != null && dialog_3_2 != null)
                {
                    merlinNPC.AddSpecialDialog(dialog_3_2);
                }

                if (merlinNPC != null && dialog_3_2_common != null)
                {
                    merlinNPC.SetCommonDialog(dialog_3_2_common);
                }

                if (mark_exit_gate != null)
                {
                    GameUIManager.Instance.destinationMark.SetTarget(mark_exit_gate);
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
        Debug.Log($"VillageSceneStory: Enemy killed. Progress: {villageEnemyKilledCount}/5 (requirement)");

        // 如果击杀了5个或以上敌人，推进故事进度
        if (villageEnemyKilledCount >= 5)
        {
            Debug.Log("VillageSceneStory: Enough enemies defeated! Moving to next stage.");
            DriveProcess();
        }
    }

    /// <summary>
    /// 任务2：到达村庄入口触发点时调用，推进到任务3。
    /// </summary>
    public void OnVillageEntranceReached()
    {
        Debug.Log($"VillageSceneStory: OnVillageEntranceReached called, storyProcess={storyProcess}");

        if (storyProcess != 1)
        {
            Debug.LogWarning($"VillageSceneStory: Story is at process {storyProcess}, expected 1. Ignoring trigger.");
            return;
        }

        Debug.Log("VillageSceneStory: 到达村庄入口，任务2完成，推进到任务3。");
        if (villageTriggerPoint != null)
        {
            villageTriggerPoint.SetActive(false);
        }

        DriveProcess();
    }

    public void OnVillageExitReached()
    {
        Debug.Log($"VillageSceneStory: OnVillageExitReached called, storyProcess={storyProcess}");

        if (storyProcess != 2)
        {
            Debug.LogWarning($"VillageSceneStory: Story is at process {storyProcess}, expected 2. Ignoring exit trigger.");
            return;
        }

        Debug.Log("VillageSceneStory: 到达村庄出口，推进到 Gareth 对话任务。");
        DriveProcess();
    }

    private Transform ResolveVillageEntranceTarget()
    {
        Transform player = null;
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null) player = playerObject.transform;

        Transform[] candidates = new Transform[]
        {
            mark_villageEntrance,
            villageEntrancePosition,
            villageTriggerPoint != null ? villageTriggerPoint.transform : null
        };

        foreach (Transform candidate in candidates)
        {
            if (candidate == null) continue;

            if (player != null && (candidate == player || candidate.IsChildOf(player)))
            {
                Debug.LogWarning($"VillageSceneStory: village entrance target '{candidate.name}' points to player and will be ignored.");
                continue;
            }

            return candidate;
        }

        Debug.LogWarning("VillageSceneStory: no valid village entrance target found.");
        return null;
    }

    private void StartVillageEntranceCheck()
    {
        if (villageEntranceCheckCoroutine != null)
        {
            StopCoroutine(villageEntranceCheckCoroutine);
        }
        villageEntranceCheckCoroutine = StartCoroutine(CheckVillageEntranceReachedAfterActivation());
    }

    private void StartVillageExitCheck()
    {
        if (villageExitCheckCoroutine != null)
        {
            StopCoroutine(villageExitCheckCoroutine);
        }
        villageExitCheckCoroutine = StartCoroutine(CheckVillageExitReached());
    }

    private IEnumerator CheckVillageEntranceReachedAfterActivation()
    {
        yield return null;

        if (storyProcess != 1)
        {
            villageEntranceCheckCoroutine = null;
            yield break;
        }

        if (IsPlayerAlreadyInsideVillageEntrance())
        {
            Debug.Log("VillageSceneStory: player already inside village entrance trigger after activation, auto advancing.");
            OnVillageEntranceReached();
        }

        villageEntranceCheckCoroutine = null;
    }

    private IEnumerator CheckVillageExitReached()
    {
        while (storyProcess == 2)
        {
            if (IsPlayerInsideExitTrigger())
            {
                OnVillageExitReached();
                break;
            }

            yield return new WaitForSeconds(0.1f);
        }

        villageExitCheckCoroutine = null;
    }

    private bool IsPlayerAlreadyInsideVillageEntrance()
    {
        if (villageTriggerPoint == null) return false;

        Collider triggerCollider = villageTriggerPoint.GetComponent<Collider>();
        if (triggerCollider == null) return false;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null) return false;

        Transform player = playerObject.transform;
        Vector3 playerProbe = player.position + Vector3.up;
        Vector3 closestPoint = triggerCollider.ClosestPoint(playerProbe);

        if ((closestPoint - playerProbe).sqrMagnitude < 0.04f)
        {
            return true;
        }

        CharacterController characterController = playerObject.GetComponent<CharacterController>();
        if (characterController != null)
        {
            Vector3 centerWorld = playerObject.transform.TransformPoint(characterController.center);
            closestPoint = triggerCollider.ClosestPoint(centerWorld);
            if ((closestPoint - centerWorld).sqrMagnitude < 0.04f)
            {
                return true;
            }
        }

        return false;
    }

    private Transform ResolveBattleGuideTarget()
    {
        if (mark_gareth != null) return mark_gareth;
        if (garethPosition != null) return garethPosition;
        if (gareth != null) return gareth.transform;

        Debug.LogWarning("VillageSceneStory: no valid Gareth target found for battle guidance.");
        return null;
    }

    private Transform ResolveVillageExitTarget()
    {
        if (villageExitPosition != null) return villageExitPosition;
        return ResolveBattleGuideTarget();
    }

    private Transform ResolveMerlinTarget()
    {
        if (mark_merlin != null) return mark_merlin;
        if (merlinNPC != null) return merlinNPC.transform;
        if (merlin != null) return merlin.transform;

        Debug.LogWarning("VillageSceneStory: no valid Merlin target found.");
        return null;
    }

    private bool IsPlayerInsideExitTrigger()
    {
        if (villageExitPosition == null) return false;

        Collider exitCollider = villageExitPosition.GetComponent<Collider>();
        if (exitCollider == null) return false;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null) return false;

        Transform player = playerObject.transform;
        Vector3 playerProbe = player.position + Vector3.up;
        Vector3 closestPoint = exitCollider.ClosestPoint(playerProbe);

        if ((closestPoint - playerProbe).sqrMagnitude < 0.04f)
        {
            return true;
        }

        CharacterController characterController = playerObject.GetComponent<CharacterController>();
        if (characterController != null)
        {
            Vector3 centerWorld = playerObject.transform.TransformPoint(characterController.center);
            closestPoint = exitCollider.ClosestPoint(centerWorld);
            if ((closestPoint - centerWorld).sqrMagnitude < 0.04f)
            {
                return true;
            }
        }

        return false;
    }

    private void ApplyVillageBattleBuff()
    {
        if (villageBattleBuffApplied) return;

        PlayerAttributes player = GameObject.FindObjectOfType<PlayerAttributes>();
        if (player == null)
        {
            Debug.LogWarning("VillageSceneStory: PlayerAttributes not found, cannot apply village battle buff.");
            return;
        }

        player.Strength += 3;
        player.commonAttackSpeed = Mathf.Max(player.commonAttackSpeed, 1.5f);
        villageBattleBuffApplied = true;

        Debug.Log($"VillageSceneStory: applied village battle buff to player. Strength={player.Strength}, commonAttackSpeed={player.commonAttackSpeed}");
    }
}
