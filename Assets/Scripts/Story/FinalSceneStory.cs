using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 最终场景故事线
/// 目前预留 King NPC、对话和任务标记的基础结构。
/// </summary>
public class FinalSceneStory : MonoBehaviour
{
    private static FinalSceneStory instance;

    public static FinalSceneStory Instance
    {
        get
        {
            if (instance == null) instance = GameObject.FindObjectOfType<FinalSceneStory>();
            return instance;
        }
    }

    public int storyProcess = 0;

    [Header("过场")]
    public Image blackImage;

    [Header("对话文件")]
    public DialogConfig dialog_4_0;   // King 初次对话
    public DialogConfig dialog_4_0_common; // King 常驻对话

    [Header("对话物")]
    public DialogObject kingNPC;

    [Header("物体")]
    public GameObject king;

    [Header("标识")]
    public Transform mark_king;

    public bool autoPickupStoryDialog = false;

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
    }

    /// <summary>
    /// 更新故事进度
    /// </summary>
    private void UpdateStory()
    {
        switch (storyProcess)
        {
            case 0:
                GameUIManager.Instance.mainTaskTip.UpdateTask("觐见国王", "前往王座前，和国王谈谈。");

                if (king != null) king.SetActive(true);

                if (kingNPC != null && dialog_4_0 != null)
                {
                    kingNPC.AddSpecialDialog(dialog_4_0);
                }

                if (kingNPC != null && dialog_4_0_common != null)
                {
                    kingNPC.SetCommonDialog(dialog_4_0_common);
                }

                if (mark_king != null)
                {
                    GameUIManager.Instance.destinationMark.SetTarget(mark_king);
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
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"FinalSceneStory: SaveGame failed but story continues. Reason: {e.Message}");
        }
    }

    /// <summary>
    /// 与目标交谈, 用于 Timeline 或事件配置
    /// </summary>
    public void Chat(DialogObject dialogObject)
    {
        if (dialogObject == null) return;

        if (!dialogObject.gameObject.activeSelf) dialogObject.gameObject.SetActive(true);
        dialogObject.Interact();
    }

    /// <summary>
    /// 开关玩家输入, 用于 Timeline 或事件配置
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
    /// 设置玩家位置, 用于 Timeline 或事件配置
    /// </summary>
    public void SetPlayerPosition(Transform pos)
    {
        if (pos == null) return;

        if (PlayerInputManager.Instance != null && PlayerInputManager.Instance.moveController != null)
        {
            PlayerInputManager.Instance.moveController.SetPositionAndRotation(pos);
        }
    }
}
