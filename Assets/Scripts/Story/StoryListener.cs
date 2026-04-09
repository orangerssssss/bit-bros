using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// 任务触发的所有事件，更进一步可以抽象为可配置的任务系统
/// </summary>
public class StoryListener
{

    public void OnCaveEntered()
    {
        Debug.Log("进入山洞触发器");
        MainSceneStory.Instance.DriveProcess();  // 推进到任务2
    }

    public void OnExitReached()
    {
        Debug.Log("到达出口触发器");
        MainSceneStory.Instance.DriveProcess();  // 推进到任务完成
    }
    /// <summary>
    /// 故事背景展示完毕，自动开始与村民的对话
    /// </summary>
    public void StoryProcess0_0()
    {
        GameEventManager.Instance.storySettingEndEvent.RemoveListener(StoryProcess0_0);

        MainSceneStory.Instance.startVillagerNPC.AddSpecialDialog(MainSceneStory.Instance.dialog_0_0);
        MainSceneStory.Instance.startVillagerNPC.Interact();
        MainSceneStory.Instance.SetAndPlayVillageBGM(1);

        GameEventManager.Instance.dialogConfigEndEvent.AddListener(StoryProcess0_1);
    }

    /// <summary>
    /// 与开始村民对话完毕，去与艾克特对话
    /// </summary>
    public void StoryProcess0_1(DialogConfig dialog)
    {
        if (dialog == MainSceneStory.Instance.dialog_0_0)
        {
            GameEventManager.Instance.dialogConfigEndEvent.RemoveListener(StoryProcess0_1);

            MainSceneStory.Instance.DriveProcess();
        }
    }

    /// <summary>
    /// 与艾克特对话完毕，去拾取武器
    /// </summary>
    public void StoryProcess1_0(DialogConfig dialog)
    {
        if (dialog == MainSceneStory.Instance.dialog_1_0)
        {
            GameEventManager.Instance.dialogConfigEndEvent.RemoveListener(StoryProcess1_0);

            MainSceneStory.Instance.DriveProcess();
        }
    }

    /// <summary>
    /// 拾取武器完毕，自动与铁匠对话
    /// </summary>
    public void StoryProcess2_0(int itemID)
    {
        if (itemID == 4001)
        {
            GameEventManager.Instance.pickUpItemEvent.RemoveListener(StoryProcess2_0);

            MainSceneStory.Instance.tradeNPC.AddSpecialDialog(MainSceneStory.Instance.dialog_2_0);
            MainSceneStory.Instance.tradeNPC.Interact();

            GameEventManager.Instance.dialogConfigEndEvent.AddListener(StoryProcess2_1);
        }
    }

    /// <summary>
    /// 与铁匠对话完毕，去与猎人对话
    /// </summary>
    public void StoryProcess2_1(DialogConfig dialog)
    {
        if (dialog == MainSceneStory.Instance.dialog_2_0)
        {
            GameEventManager.Instance.dialogConfigEndEvent.RemoveListener(StoryProcess2_1);

            GameUIManager.Instance.controlTip.ShowTip("‘B’ 打开角色及背包界面\n‘拖动物品’ 装备武器\n‘鼠标左键’ 攻击");
            GameUIManager.Instance.mainTaskTip.AddSideTask("探索山洞获取武器");
            GameUIManager.Instance.sideDestinationMark0.SetTarget(MainSceneStory.Instance.mark_side0);
            GameUIManager.Instance.mainTaskTip.UpdateTask("在背包内装备获得的武器，在山林中杀死野猪，取得3块兽肉后交给艾克特。");
            MainSceneStory.Instance.hunterNPC.AddSpecialDialog(MainSceneStory.Instance.dialog_2_1);
            GameUIManager.Instance.destinationMark.SetTarget(MainSceneStory.Instance.mark_hunter);

            GameEventManager.Instance.dialogConfigEndEvent.AddListener(StoryProcess2_2);
        }

    }

    /// <summary>
    /// 与猎人对话完毕，去提交兽肉
    /// </summary>
    public void StoryProcess2_2(DialogConfig dialog)
    {
        if (dialog == MainSceneStory.Instance.dialog_2_1)
        {
            GameEventManager.Instance.dialogConfigEndEvent.RemoveListener(StoryProcess2_2);

            GameUIManager.Instance.mainTaskTip.AddSideTask("调查猎人营地", true);
            GameUIManager.Instance.sideDestinationMark1.SetTarget(MainSceneStory.Instance.mark_side1);
            GameUIManager.Instance.destinationMark.SetTarget(MainSceneStory.Instance.mark_hunt, 36.0f);

            GameEventManager.Instance.beforeDialogEvent.AddListener(StoryProcess2_3);
            GameEventManager.Instance.pickUpItemEvent.AddListener(StoryProcess2_2Tip);
        }
    }

    public void StoryProcess2_2Tip(int itemID)
    {
        if (itemID == 1001)
        {
            MainSceneStory.Instance.pickMealCount--;
            if (MainSceneStory.Instance.pickMealCount == 2)
            {
                GameUIManager.Instance.controlTip.ShowTip("‘B’ 打开角色及背包界面\n‘背包->材料’ 检查物品");
            }

            if (MainSceneStory.Instance.pickMealCount <= 0)
            {
                GameUIManager.Instance.destinationMark.SetTarget(MainSceneStory.Instance.mark_ector);
            }
        }
    }

    /// <summary>
    /// 检查兽肉完毕，自动与艾克特对话
    /// </summary>
    public void StoryProcess2_3(DialogObject dialogNPC)
    {
        if (dialogNPC == MainSceneStory.Instance.ectorNPC && InventoryManager.Instance.HasItems(DataManager.Instance.itemConfig.FindItemByID(1001), 3))
        {
            GameEventManager.Instance.beforeDialogEvent.RemoveListener(StoryProcess2_3);

            MainSceneStory.Instance.ectorNPC.AddSpecialDialog(MainSceneStory.Instance.dialog_2_2);

            GameEventManager.Instance.dialogConfigEndEvent.AddListener(StoryProcess2_4);
        }
    }

    /// <summary>
    /// 提交兽肉完毕，去查看告示
    /// </summary>
    public void StoryProcess2_4(DialogConfig dialog)
    {
        if (dialog == MainSceneStory.Instance.dialog_2_2)
        {
            GameEventManager.Instance.dialogConfigEndEvent.RemoveListener(StoryProcess2_4);

            InventoryManager.Instance.ReduceItems(DataManager.Instance.itemConfig.FindItemByID(1001), 3);

            MainSceneStory.Instance.DriveProcess();
        }
    }

    /// <summary>
    /// 查看告示完毕，去与梅林对话
    /// </summary>
    public void StoryProcess3_0(DialogConfig dialog)
    {
        if (dialog == MainSceneStory.Instance.dialog_3_0)
        {
            GameEventManager.Instance.dialogConfigEndEvent.RemoveListener(StoryProcess3_0);

            MainSceneStory.Instance.DriveProcess();
        }
    }

    /// <summary>
    /// 与梅林对话完毕，播放离开动画
    /// </summary>
    public void StoryProcess4_0(DialogConfig dialog)
    {
        if (dialog == MainSceneStory.Instance.dialog_4_0)
        {
            GameEventManager.Instance.dialogConfigEndEvent.RemoveListener(StoryProcess4_0);

            MainSceneStory.Instance.timeline_7_noticeLeave.Play();
            MainSceneStory.Instance.PlayerInputActive(false);

            MainSceneStory.Instance.timeline_7_noticeLeave.stopped += StoryProcess4_1;
        }
    }

    /// <summary>
    /// 播放离开动画完毕，去向艾克特报告
    /// </summary>
    public void StoryProcess4_1(PlayableDirector director)
    {
        MainSceneStory.Instance.timeline_7_noticeLeave.stopped -= StoryProcess4_1;

        MainSceneStory.Instance.PlayerInputActive(true);
        MainSceneStory.Instance.noticeNPCs.SetActive(false);
        MainSceneStory.Instance.SetAndPlayVillageBGM(1);

        MainSceneStory.Instance.DriveProcess();
    }

    /// <summary>
    /// 向艾克特报告完毕，去武装/计时
    /// </summary>
    public void StoryProcess5_0(DialogConfig dialog)
    {
        if (dialog == MainSceneStory.Instance.dialog_5_0)
        {
            GameEventManager.Instance.dialogConfigEndEvent.RemoveListener(StoryProcess5_0);

            MainSceneStory.Instance.tradeNPC.SetCommonDialog(MainSceneStory.Instance.dialog_5_1);
            GameUIManager.Instance.destinationMark.SetTarget(MainSceneStory.Instance.mark_villageTrade);
            GameUIManager.Instance.mainTaskTip.UpdateTask($"为村民准备武器，武装{MainSceneStory.Instance.requiredWeaponCount}个村民。");

            // 武装村民
            GameEventManager.Instance.dialogConfigEndEvent.AddListener(StoryProcess5_1);
            GameEventManager.Instance.dialogConfigEndEvent.AddListener(StoryProcess5_2);
            // 计时
            MainSceneStory.Instance.storyTimer.StartTimer("强盗到达倒计时", MainSceneStory.Instance.collectWeaponSeconds, StoryProcess5_3);
        }
    }

    /// <summary>
    /// 与武器架交互，显示提交武器数量
    /// </summary>
    public void StoryProcess5_1(DialogConfig dialog)
    {
        if (dialog == MainSceneStory.Instance.dialog_5_1_0)
        {
            int submitWeaponCount = InventoryManager.Instance.TryReduceItemsByType(ItemType.Weapon, MainSceneStory.Instance.requiredWeaponCount);
            MainSceneStory.Instance.requiredWeaponCount -= submitWeaponCount;

            MainSceneStory.Instance.dialog_5_1_1.contents[0].content = $"已为{submitWeaponCount}名村民装备武器。";

            MainSceneStory.Instance.submitWeaponCount = submitWeaponCount;

            if (submitWeaponCount > 0)
            {
                for (int i = 0; i < MainSceneStory.Instance.submitWeaponCount; i++)
                {
                    if (!MainSceneStory.Instance.invasionVillagers[i].gameObject.activeSelf)
                    {
                        MainSceneStory.Instance.invasionVillagers[i].gameObject.SetActive(true);
                    }
                }
            }
        }
        MainSceneStory.Instance.dialog_5_1_2.contents[0].content = $"还需{MainSceneStory.Instance.requiredWeaponCount}件武器。";
    }

    /// <summary>
    /// 提交武器完毕，自动与村民对话选择战斗
    /// </summary>
    public void StoryProcess5_2(DialogConfig dialog)
    {
        if (dialog == MainSceneStory.Instance.dialog_5_1_2)
        {
            if (MainSceneStory.Instance.requiredWeaponCount <= 0)
            {
                MainSceneStory.Instance.storyTimer.StopTimer();
                StoryProcess5_4();
            }
        }
    }

    /// <summary>
    /// 计时结束，自动与村民对话选择战斗
    /// </summary>
    public void StoryProcess5_3()
    {
        // 未考虑玩家死亡
        GameUIManager.Instance.CloseAllWindow();

        StoryProcess5_4();
    }

    /// <summary>
    /// 与村民对话并选择
    /// </summary>
    private void StoryProcess5_4()
    {
        MainSceneStory.Instance.invasionFightVillager.SetActive(false);

        GameEventManager.Instance.dialogConfigEndEvent.RemoveListener(StoryProcess5_2);
        GameEventManager.Instance.dialogConfigEndEvent.RemoveListener(StoryProcess5_1);
        MainSceneStory.Instance.tradeNPC.SetCommonDialog(MainSceneStory.Instance.dialogTrade);

        MainSceneStory.Instance.villageFire.SetActive(true);
        MainSceneStory.Instance.villageNPCs.SetActive(false);
        MainSceneStory.Instance.beforeInvasionVillagerNPC.gameObject.SetActive(true);

        PlayerInputManager.Instance.moveController.SetPositionAndRotation(MainSceneStory.Instance.beforeInvasionPosition);
        MainSceneStory.Instance.beforeInvasionVillagerNPC.AddSpecialDialog(MainSceneStory.Instance.dialog_5_2);
        MainSceneStory.Instance.beforeInvasionVillagerNPC.Interact();

        GameEventManager.Instance.dialogConfigEndEvent.AddListener(StoryProcess5_5);
    }

    /// <summary>
    /// 选择结束，去士兵战斗
    /// </summary>
    public void StoryProcess5_5(DialogConfig dialog)
    {
        // 独自/联合的选择
        if (dialog == MainSceneStory.Instance.dialog_5_3)
        {
            GameEventManager.Instance.dialogConfigEndEvent.RemoveListener(StoryProcess5_5);

            // 独自挑战时降低难度
            foreach (FightAttributes attributes in MainSceneStory.Instance.invasionEnemies)
            {
                attributes.ChangeAttributes(-2, -2, 0);
                attributes.InitAttributes();
            }

            MainSceneStory.Instance.DriveProcess();
        }
        else if (dialog == MainSceneStory.Instance.dialog_5_4)
        {
            GameEventManager.Instance.dialogConfigEndEvent.RemoveListener(StoryProcess5_5);

            MainSceneStory.Instance.invasionFightVillager.SetActive(true);
            for (int i = 0; i < MainSceneStory.Instance.submitWeaponCount; i++)
            {
                MainSceneStory.Instance.invasionVillagers[i].gameObject.SetActive(true);
                if (i >= MainSceneStory.Instance.requiredWeaponCount)
                {
                    // 统一提升属性
                    MainSceneStory.Instance.invasionVillagers[i].ImproveAttributesByEquipments((EquipmentItem)DataManager.Instance.itemConfig.FindItemByID(4002));
                    MainSceneStory.Instance.invasionVillagers[i].InitAttributes();
                }
            }

            MainSceneStory.Instance.DriveProcess();
        }
    }

    /// <summary>
    /// 与士兵战斗结束，分为杀死所有敌人、玩家第一次死、玩家第二次死三种情况，最终进入梅林动画
    /// </summary>
    public void StoryProcess6_0(CharacterAttributes character)
    {
        if (character.GetType() == typeof(FightAttributes))
        {
            if (((FightAttributes)character).fightName == "蒙面武士")
            {
                MainSceneStory.Instance.enemyDeathCount--;
                if (MainSceneStory.Instance.enemyDeathCount <= 0)
                {
                    GameEventManager.Instance.characterBeforeDeathEvent.RemoveListener(StoryProcess6_0);

                    MainSceneStory.Instance.villageFire.SetActive(false);
                    MainSceneStory.Instance.timeline_9_invasionEnd.Play();

                    MainSceneStory.Instance.timeline_9_invasionEnd.stopped += StoryProecess6_2;
                }
            }
        }
        else if (character.GetType() == typeof(PlayerAttributes))
        {
            MainSceneStory.Instance.playerDeathCount--;
            if (MainSceneStory.Instance.playerDeathCount == 0)
            {
                //GameEventManager.Instance.playerRespawnEvent.AddListener(StoryProcess6_1);
            }
            else
            {
                GameEventManager.Instance.characterBeforeDeathEvent.RemoveListener(StoryProcess6_0);
                GameObject.FindObjectOfType<PlayerAttributes>().AvoidDeath();

                MainSceneStory.Instance.villageFire.SetActive(false);
                MainSceneStory.Instance.timeline_9_invasionEnd.Play();

                MainSceneStory.Instance.timeline_9_invasionEnd.stopped += StoryProecess6_2;
            }
        }
    }

    /// <summary>
    /// 梅林动画播放完毕，自动与梅林对话
    /// </summary>
    public void StoryProecess6_2(PlayableDirector director)
    {
        MainSceneStory.Instance.timeline_9_invasionEnd.stopped -= StoryProecess6_2;

        MainSceneStory.Instance.villageNPCs.SetActive(true);
        MainSceneStory.Instance.beforeInvasionVillagerNPC.gameObject.SetActive(false);
        MainSceneStory.Instance.villagerNonCombat.SetActive(true);

        PlayerInputManager.Instance.moveController.SetPositionAndRotation(MainSceneStory.Instance.invasionEndChatPoint);
        MainSceneStory.Instance.ectorNPC.AddSpecialDialog(MainSceneStory.Instance.dialog_6_0);
        MainSceneStory.Instance.ectorNPC.Interact();

        MainSceneStory.Instance.SetAndPlayVillageBGM(3);

        GameEventManager.Instance.dialogConfigEndEvent.AddListener(StoryProcess6_3);
    }

    /// <summary>
    /// 与梅林对话完毕，去拾取物品
    /// </summary>
    public void StoryProcess6_3(DialogConfig dialog)
    {
        if (dialog == MainSceneStory.Instance.dialog_6_0)
        {
            GameEventManager.Instance.dialogConfigEndEvent.RemoveListener(StoryProcess6_3);

            MainSceneStory.Instance.DriveProcess();
        }
    }

    /// <summary>
    /// 拾取物品完毕，去出售物品
    /// </summary>
    public void StoryProcess7_0(int itemID)
    {
        if (itemID == 2001 || itemID == 4003)
        {
            MainSceneStory.Instance.dropItemCount--;
            if (MainSceneStory.Instance.dropItemCount <= 0)
            {
                GameEventManager.Instance.pickUpItemEvent.RemoveListener(StoryProcess7_0);

                GameUIManager.Instance.destinationMark.SetTarget(MainSceneStory.Instance.mark_villageTrade);

                GameEventManager.Instance.sellEvent.AddListener(StoryProcess7_1);
            }
        }
    }

    /// <summary>
    /// 出售物品完毕，等待关闭界面
    /// </summary>
    public void StoryProcess7_1()
    {
        GameEventManager.Instance.sellEvent.RemoveListener(StoryProcess7_1);

        GameEventManager.Instance.closePackageEvent.AddListener(StoryProcess7_2);
    }

    /// <summary>
    /// 关闭界面完毕，自动与铁匠对话
    /// </summary>
    public void StoryProcess7_2()
    {
        GameEventManager.Instance.closePackageEvent.RemoveListener(StoryProcess7_2);

        MainSceneStory.Instance.tradeNPC.AddSpecialDialog(MainSceneStory.Instance.dialog_7_0);
        MainSceneStory.Instance.tradeNPC.Interact();

        GameEventManager.Instance.dialogConfigEndEvent.AddListener(StoryProcess7_3);
    }

    /// <summary>
    /// 与铁匠对话完毕，去向艾克特告别
    /// </summary>
    public void StoryProcess7_3(DialogConfig dialog)
    {
        if (dialog == MainSceneStory.Instance.dialog_7_0)
        {
            GameEventManager.Instance.dialogConfigEndEvent.RemoveListener(StoryProcess7_3);

            MainSceneStory.Instance.DriveProcess();
        }
    }

    /// <summary>
    /// 向艾克特告别完毕，去离开村庄
    /// </summary>
    public void StoryProcess8_0(DialogConfig dialog)
    {
        if (dialog == MainSceneStory.Instance.dialog_8_0)
        {
            GameEventManager.Instance.dialogConfigEndEvent.RemoveListener(StoryProcess8_0);

            MainSceneStory.Instance.DriveProcess();
        }
    }

    public void SideStory0_0(DialogConfig dialog)
    {
        if (dialog == MainSceneStory.Instance.sideDialog_0_0)
        {
            // 入口对话
            if (InventoryManager.Instance.ReduceItems(DataManager.Instance.itemConfig.FindItemByID(1002), 1))
            {
                // 物品足够，将玩家传送至山洞内
                PlayerInputManager.Instance.moveController.SetPositionAndRotation(MainSceneStory.Instance.caveExit);
                GameUIManager.Instance.mainTaskTip.RemoveSideTask("探索山洞获取武器");
                GameUIManager.Instance.sideDestinationMark0.HideMark();
            }
            else
            {
                // 物品不足，扣除玩家血量
                PlayerAttributes attributes = GameObject.FindObjectOfType<PlayerAttributes>();
                attributes.GetAttack((int)(attributes.MaxHealth * 0.1f), false);

                GameUIManager.Instance.messageTip.ShowTip("解密失败");
            }
        }
        else if (dialog == MainSceneStory.Instance.sideDialog_0_2)
        {
            // 出口对话，将玩家传送至山洞外
            PlayerInputManager.Instance.moveController.SetPositionAndRotation(MainSceneStory.Instance.caveEntry);
        }
    }

    public void SideStory1_0(DialogConfig dialog)
    {
        if (dialog == MainSceneStory.Instance.sideDialog_1_0)
        {
            GameUIManager.Instance.mainTaskTip.RemoveSideTask("调查猎人营地");
            GameUIManager.Instance.sideDestinationMark1.HideMark();
        }

        if (dialog == MainSceneStory.Instance.sideDialog_1_1)
        {
            MainSceneStory.Instance.sideSoldierNPC.gameObject.SetActive(false);

            MainSceneStory.Instance.sideSoldierFight.transform.SetPositionAndRotation
                (MainSceneStory.Instance.sideSoldierNPC.transform.position, MainSceneStory.Instance.sideSoldierNPC.transform.rotation);
            MainSceneStory.Instance.sideSoldierFight.gameObject.SetActive(true);
        }
    }

    public void SideStory1_1(CharacterAttributes character)
    {
        if (character == MainSceneStory.Instance.sideSoldierFight)
        {
            MainSceneStory.Instance.sideSoldierFight.InitAttributes();
            MainSceneStory.Instance.sideSoldierFight.headDisplayer.InitState();
            MainSceneStory.Instance.sideSoldierFight.GetComponent<FightAI>().ResetFightAI();
            MainSceneStory.Instance.sideSoldierFight.gameObject.SetActive(false);

            MainSceneStory.Instance.sideSoldierNPC.gameObject.SetActive(true);
            MainSceneStory.Instance.sideSoldierNPC.AddSpecialDialog(MainSceneStory.Instance.sideDialog_1_2);
            PlayerInputManager.Instance.moveController.SetPositionAndRotation(MainSceneStory.Instance.sideSoldierNPCPosition);
            MainSceneStory.Instance.sideSoldierNPC.Interact();
        }
    }
}
