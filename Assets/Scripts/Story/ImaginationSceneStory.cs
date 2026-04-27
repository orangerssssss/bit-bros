using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Playables;

/// <summary>
/// 梦境场景故事线, 包括故事进度的推进及各种故事需要的特殊函数
/// 完全复刻 MainSceneStory.cs 的架构，为梦境场景服务
/// </summary>
public class ImaginationSceneStory : MonoBehaviour
{
    private static ImaginationSceneStory instance;// 单例

    public static ImaginationSceneStory Instance
    {
        get
        {
            if (instance == null) instance = GameObject.FindObjectOfType<ImaginationSceneStory>();
            return instance;
        }
    }

    public int storyProcess = 0;

    public Image blackImage;// 黑色图, 用于过场

    // 此部分为梦境场景中涉及的各种物体、对话等，用于在代码中进行控制

    [Header("对话文件")]
    public DialogConfig dialog_0;// 开场对话

    [Header("对话物")]
    public DialogObject introDialogObject;// 开场对话物体


    [Header("战斗角色")]
    public FightAttributes imaginationEnemy;// 梦境中的敌人

    [Header("位置")]
    public Transform Exit;// 梦境出口
    public Transform enemySpawnPoint;// 敌人出现点

    [Header("物体")]
    public GameObject enemyObject;// 敌人GameObject


    [Header("标识")]
    public Transform mark_Exit;// 梦境出口标记
    public Transform mark_enemy;// 敌人标记

    [Header("场景管理")]
    public string exitSceneName = "BirthScene";// 返回的场景名称

    private ImaginationSceneListener storyListener = new ImaginationSceneListener();
    public bool autoPickupStoryDialog = false;

    private float dialogDelayTimer = 0f;
    private bool dialogStarted = false;
    [SerializeField, Tooltip("Timeout seconds to wait for dialog/UI initialization; 0 or negative = wait indefinitely")]
    private float dialogSystemWaitTimeout = 0f;
    [SerializeField, Tooltip("Poll interval seconds while waiting for dialog/UI initialization")]
    private float dialogSystemPollInterval = 0.2f;
    private Coroutine openDialogRetryCoroutine = null;

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


        UpdateStory();

        AddStoryEvents();
    }

    private void Update()
    {
        // 处理对话延迟计时（不用协程，用 Update 中的计时器）
        if (storyProcess == 0 && !dialogStarted)
        {
            dialogDelayTimer += Time.deltaTime;

            // 等待 1 秒确保所有 UI 组件都初始化完成（比 0.3 秒更稳定）
            // 同时检查 DialogDisplayer 是否已准备好
            if (dialogDelayTimer >= 1.0f && DialogDisplayer.Instance != null && GameUIManager.Instance != null)
            {
                dialogStarted = true;
                PlayOpeningDialog();
            }
        }
    }

    /// <summary>
    /// 添加故事事件监听
    /// </summary>
    private void AddStoryEvents()
    {
        // 监听对话结束事件
        GameEventManager.Instance.dialogConfigEndEvent.AddListener(storyListener.CheckDialogEnd);

        //修改点
        //监听敌人死亡事件
        GameEventManager.Instance.fightBeforeDeathEvent.AddListener(storyListener.OnEnemyDeath);
    }

    /// <summary>
    /// 更新故事进度
    /// </summary>
    private void UpdateStory()
    {

        switch (storyProcess)
        {
            case 0:// 梦境开始 - 自动播放开场对话


                // 播放背景音乐（已禁用）
                // if (bgms.Count > 0)
                //     SetAndPlayImaginationBGM(0);
                // 临时禁用敌人
                if (enemyObject != null)
                    enemyObject.SetActive(false);

                break;

            case 1:// 对话结束 - 显示主线任务"斩杀敌人"
                // 更新主线任务
                GameUIManager.Instance.mainTaskTip.UpdateTask("斩杀梦境中的敌人", "消灭眼前的威胁，才能逃离这里。");
                // 激活敌人
                if (enemyObject != null)
                {
                    enemyObject.SetActive(true);
                    // if (imaginationEnemy != null && imaginationEnemy.currentHealth <= 0)
                    {
                        //修改点 2
                        // 如果敌人已死亡，重置健康值
                        //imaginationEnemy.currentHealth = imaginationEnemy.MaxHealth;
                    }
                }

                // 设置目标标记指向敌人
                if (mark_enemy != null)
                    GameUIManager.Instance.destinationMark.SetTarget(mark_enemy);

                break;

            case 2:// 敌人被击杀 - 触发离开梦境逻辑

                // 更新任务状态
                GameUIManager.Instance.mainTaskTip.UpdateTask("离开梦境", "你成功了，现在逃离这个梦境吧。");

                // 触发离开梦境逻辑（延迟1秒演出效果）
                StartCoroutine(ExitImaginationAfterDelay(1.0f));

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

        // 保存游戏进度（暂时禁用，梦境场景中会报错）
        // try
        // {
        //     if (DataManager.Instance != null && DataManager.Instance.saveData != null)
        //     {
        //         DataManager.Instance.SaveGame();
        //         GameUIManager.Instance.messageTip.ShowTip("游戏进度已保存");
        //     }
        // }
        // catch (System.Exception e)
        // {
        //     Debug.LogError($"保存游戏进度失败: {e.Message}");
        // }
    }

    /// <summary>
    /// 播放开场对话（从 Update 中调用）
    /// </summary>
    private void PlayOpeningDialog()
    {
        // 播放开场对话
        if (dialog_0 == null || introDialogObject == null)
        {
            Debug.LogWarning($"{name} ImaginationSceneStory: dialog_0 or introDialogObject is missing; cannot play opening dialog.");
            return;
        }

        // If dialog systems are ready, start immediately
        if (DialogDisplayer.Instance != null && GameUIManager.Instance != null)
        {
            dialogStarted = true;
            DialogDisplayer.Instance.StartDialog(introDialogObject, dialog_0, introDialogObject.transform.position, null);
            return;
        }

        // Not ready yet: start a retry coroutine that will poll until systems are available
        if (openDialogRetryCoroutine == null)
        {
            Debug.LogWarning($"{name} ImaginationSceneStory: Dialog system/UI not ready yet - starting retry coroutine.");
            openDialogRetryCoroutine = StartCoroutine(WaitForDialogSystemsAndStart(dialogSystemWaitTimeout));
        }
    }

    private IEnumerator WaitForDialogSystemsAndStart(float timeout)
    {
        // Prefer event-driven notification if available
        bool started = false;

        System.Action onReady = null;
        onReady = () =>
        {
            if (DialogDisplayer.Instance != null && GameUIManager.Instance != null && dialog_0 != null && introDialogObject != null)
            {
                dialogStarted = true;
                DialogDisplayer.Instance.StartDialog(introDialogObject, dialog_0, introDialogObject.transform.position, null);
                Debug.Log($"{name} ImaginationSceneStory: Dialog system ready - started opening dialog via event.");
                started = true;
            }
        };

        // Subscribe to both UI and DialogDisplayer ready events if present
        try { GameUIManager.OnUIReady += onReady; } catch { }
        try { DialogDisplayer.OnDialogDisplayerReady += onReady; } catch { }

        float start = Time.realtimeSinceStartup;
        while (!started && (timeout <= 0f || Time.realtimeSinceStartup - start < timeout))
        {
            // also poll occasionally in case events were missed
            if (DialogDisplayer.Instance != null && GameUIManager.Instance != null && dialog_0 != null && introDialogObject != null)
            {
                dialogStarted = true;
                DialogDisplayer.Instance.StartDialog(introDialogObject, dialog_0, introDialogObject.transform.position, null);
                Debug.Log($"{name} ImaginationSceneStory: Dialog system ready - started opening dialog via polling.");
                started = true;
                break;
            }
            yield return new WaitForSecondsRealtime(dialogSystemPollInterval);
        }

        // cleanup subscriptions
        try { GameUIManager.OnUIReady -= onReady; } catch { }
        try { DialogDisplayer.OnDialogDisplayerReady -= onReady; } catch { }

        openDialogRetryCoroutine = null;

        if (!started)
        {
            Debug.LogWarning($"{name} ImaginationSceneStory: WaitForDialogSystemsAndStart timed out after {timeout} seconds.");
        }
    }

    /// <summary>
    /// 播放开场对话（延迟触发以确保UI完全初始化）
    /// </summary>
    private IEnumerator PlayOpeningDialogAfterDelay(float delay)
    {
        // 使用 WaitForSecondsRealtime 而不是 WaitForSeconds，以绕过时间暂停
        yield return new WaitForSecondsRealtime(delay);

        PlayOpeningDialog();
    }

    /// <summary>
    /// 离开梦境（延迟触发）
    /// </summary>
    private IEnumerator ExitImaginationAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        ExitImagination();
    }

    /// <summary>
    /// 离开梦境 - 返回主场景
    /// </summary>
    private void ExitImagination()
    {

        // 黑屏过场
        StartCoroutine(BlackAndLoadScene());
    }

    /// <summary>
    /// 黑屏过场并加载新场景
    /// </summary>
    private IEnumerator BlackAndLoadScene()
    {
        // 执行黑屏开始
        yield return StartCoroutine(BlackIEnum());

        // 加载场景
        UnityEngine.SceneManagement.SceneManager.LoadScene(exitSceneName);
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
        if (blackImage == null) yield break;

        Color color = Color.black;
        color.a = 0;
        blackImage.color = color;

        // 淡入黑色
        while (color.a < 1.0f)
        {
            color.a += Time.deltaTime * 2.5f;
            blackImage.color = color;
            yield return null;
        }

        yield return new WaitForSeconds(0.4f);

        // 淡出黑色
        while (color.a > 0)
        {
            color.a -= Time.deltaTime * 2.5f;
            blackImage.color = color;
            yield return null;
        }
    }

    /// <summary>
    /// 与目标交谈，用于在Timeline中进行事件配置
    /// </summary>
    public void Chat(DialogObject dialogObject)
    {
        if (!dialogObject.gameObject.activeSelf) dialogObject.gameObject.SetActive(true);
        dialogObject.Interact();
    }

    /// <summary>
    /// 开关玩家的输入，用于在Timeline中进行事件配置
    /// </summary>
    public void PlayerInputActive(bool active)
    {
        if (!active) GameMenu.Instance.CloseMenu();
        InventoryManager.Instance.CloseInventory();

        if (active) PlayerInputManager.Instance.OpenAllInput();
        else PlayerInputManager.Instance.CloseAllInput(false);
    }

    /// <summary>
    /// 设置玩家位置，用于在Timeline中进行事件配置
    /// </summary>
    public void SetPlayerPosition(Transform pos)
    {
        PlayerInputManager.Instance.moveController.SetPositionAndRotation(pos);
    }

    /// <summary>
    /// 播放梦境BGM
    /// </summary>
    public void PlayImaginationBGM()
    {
        // 已禁用 BGM 功能（暂时跳过）
        return;
        /*
        if (imaginationBGM == null && bgms.Count > 0)
            imaginationBGM = bgms[0];

        if (storyAudioSource != null && imaginationBGM != null)
        {
            storyAudioSource.Stop();
            storyAudioSource.clip = imaginationBGM;
            storyAudioSource.Play();
        }
        */
    }

    /// <summary>
    /// 设置并播放梦境BGM
    /// </summary>
    public void SetAndPlayImaginationBGM(int index)
    {
        // // 已禁用 BGM 功能（暂时跳过）
        // return;
        // 
        // if (bgms.Count > index)
        // {
        //     imaginationBGM = bgms[index];

        //     if (storyAudioSource != null)
        //     {
        //         storyAudioSource.Stop();
        //         storyAudioSource.clip = imaginationBGM;
        //         storyAudioSource.Play();
        //     }
        // }
        // 
    }

    /// // 已禁用 BGM 功能（暂时跳过）
    //     return;

    // if (storyAudioSource != null && bgms.Count > 2)
    // {
    //     storyAudioSource.Stop();
    //     storyAudioSource.clip = bgms[2];
    //     storyAudioSource.Play();
    // }
    // f (storyAudioSource != null && bgms.Count > 2)
    // {
    //     storyAudioSource.Stop();
    //     storyAudioSource.clip = bgms[2];
    //     storyAudioSource.Play();
    // }
    // }
}

/// <summary>
/// 梦境场景故事事件监听器
/// 用于处理梦境场景中的各种事件回调
/// </summary>
public class ImaginationSceneListener
{
    /// <summary>
    /// 检查对话是否结束
    /// </summary>
    public void CheckDialogEnd(DialogConfig endedDialog)
    {
        if (endedDialog == ImaginationSceneStory.Instance.dialog_0)
        {
            // 开场对话结束，推进进度到敌人出现阶段
            ImaginationSceneStory.Instance.DriveProcess();

            // 移除此监听，防止重复触发
            GameEventManager.Instance.dialogConfigEndEvent.RemoveListener(CheckDialogEnd);
        }
    }

    /// <summary>
    /// 敌人被击杀回调
    /// </summary>
    public void OnEnemyDeath(FightAttributes enemy)
    {
        // 检查被杀死的敌人是否是梦境敌人
        if (enemy == ImaginationSceneStory.Instance.imaginationEnemy)
        {
            ImaginationSceneStory.Instance.DriveProcess();

            //修改点 3
            // 移除此监听
            //GameEventManager.Instance.characterBeforeDeathEvent.RemoveListener(OnEnemyDeath);
        }
    }
}
