using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 用于对话内容的触发 显示及退出, 并调用对话事件
/// </summary>
public class DialogDisplayer : MonoBehaviour
{
    private static DialogDisplayer instance;// 单例

    public static DialogDisplayer Instance
    {
        get
        {
            if (instance == null) instance = GameObject.FindObjectOfType<DialogDisplayer>();
            return instance;
        }
    }

    private GameObject dialogUIObject;// 对话框
    private Button dialogNextButton;// 继续对话按钮
    private Transform optionLabel;// 选项父物体
    private Text nameText;// 名称文本
    private Text contentText;// 对话文本
    private GameObject continueTip;// 继续提示
    public DialogConfig defaultDialogConfig;// 默认对话(当对话物没有配置对话时触发此对话)
    private Button dialogSkipButton;

    [Range(0.5f, 1.24f)]
    public float speed = 0.9f;// 文本逐字显示速度

    [HideInInspector]
    public PlayerMoveController controller;// 玩家控制器

    private bool isDialog = false;// 是否正在对话
    private bool isShowText = false;// 是否正在逐字显示文本
    private DialogConfig dialogConfig;// npc对话配置文件
    private int contentsIndex;// 当前显示到第几句对话
    private IdleAI dialogNPC;// 对话的NPC(如果目标为NPC的话)
    private DialogObject dialogObject;// 当前对话的对话物

    private void Awake()
    {
        controller = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerMoveController>();
    }

    private void Start()
    {
        dialogUIObject = GameUIManager.Instance.dialog;
        dialogNextButton = GameUIManager.Instance.dialogNextButton;
        optionLabel = GameUIManager.Instance.optionLabel;
        nameText = GameUIManager.Instance.dialogNameTextLabel;
        contentText = GameUIManager.Instance.dialogTextLabel;
        continueTip = GameUIManager.Instance.dialogContinueTip;
        dialogSkipButton = GameUIManager.Instance.dialogSkipButton;

        dialogNextButton.onClick.AddListener(() => DialogNext());
        dialogSkipButton.onClick.AddListener(SkipDialog);

        // 添加选项的点击事件
        for (int i = 0; i < optionLabel.childCount; i++)
        {
            int index = i;
            optionLabel.GetChild(i).GetComponent<Button>().onClick.AddListener(delegate () { OptionSelect(index); });
        }
    }

    /// <summary>
    /// 开始对话
    /// </summary>
    /// <param name="dialogObj">对话物</param>
    /// <param name="dialogCfg">对话文件</param>
    /// <param name="rotatePoint">玩家朝向的位置</param>
    /// <param name="npc">NPC的IdleAI组件, 如果不为NPC则此值为null</param>
    public void StartDialog(DialogObject dialogObj, DialogConfig dialogCfg, Vector3 rotatePoint, IdleAI npc)
    {
        if (isDialog) ExitDialog();
        if (dialogCfg == null)
        {
            dialogCfg = defaultDialogConfig;
        }


        // 标记对话开始
        isDialog = true;

        // 关闭玩家的移动控制和视角控制
        GameMenu.Instance.CloseMenu();
        PlayerInputManager.Instance.CloseAllInput(true);

        // 打开对话框
        dialogUIObject.SetActive(true);

        // 初始化对话数据
        dialogObject = dialogObj;
        dialogConfig = dialogCfg;
        contentsIndex = 0;

        // 玩家角色朝向对话目标
        controller.RotateTo(rotatePoint);
        if (npc)
        {
            dialogNPC = npc;
            dialogNPC.StartDialog();
        }

        // 显示下一对话文本
        DialogNext();
    }

    /// <summary>
    /// 显示下一对话内容
    /// </summary>
    public bool DialogNext()
    {
        if (dialogConfig == null) ExitDialog();

        continueTip.SetActive(false); 

        if (isDialog)
        {
            // 判断文字是否正在显示
            if (isShowText)
            {
                // 停止逐字显示, 并显示全部文字
                StopAllCoroutines();
                isShowText = false;
                contentText.text = dialogConfig.contents[contentsIndex - 1].content;

                if (contentsIndex >= dialogConfig.contents.Count)
                {
                    // 显示对话选择
                    ShowOption();
                }
                else
                {
                    continueTip.SetActive(true);
                }
            }
            else
            {
                // 判断内容是否结束
                if (contentsIndex < dialogConfig.contents.Count)
                {
                    // 显示下一句话
                    StartCoroutine(ShowText(contentsIndex));
                    contentsIndex++;
                }
                else
                {

                    // 判断是否结束对话
                    if (dialogConfig.nextDialog == null || dialogConfig.nextDialog.Count == 0)
                    {
                        ExitDialog();
                        //GameEventManager.Instance.dialogEndEvent.Invoke(dialogObject);
                        GameEventManager.Instance.dialogConfigEndEvent.Invoke(dialogConfig);
                    }
                    else
                    {
                        // 此部分为对话文本已经显示完毕且对话选项已经显示的情况, 此时调用此函数(鼠标点击对话框)不会有任何反应
                    }

                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// 结束对话
    /// </summary>
    public void ExitDialog()
    {
        // 判断是否在对话
        if (isDialog)
        {
            // 停止所有协程
            StopAllCoroutines();

            // 关闭状态
            isDialog = false;
            isShowText = false;

            // 停止尝试将玩家角色转向
            controller.StopRotateTo();
            if (dialogNPC)
            {
                dialogNPC.StopDialog();
                dialogNPC = null;
            }

            // 关闭对话框
            if (dialogConfig) CloseOption();
            dialogUIObject.SetActive(false);

            // 打开玩家控制
            PlayerInputManager.Instance.OpenAllInput();
        }
    }

    /// <summary>
    /// 逐字显示对话文本
    /// </summary>
    private IEnumerator ShowText(int index)
    {
        // 逐字显示
        isShowText = true;
        nameText.text = dialogConfig.contents[index].name;
        contentText.text = "";
        for (int i = 0; i < dialogConfig.contents[index].content.Length; i++)
        {
            contentText.text += dialogConfig.contents[index].content[i];
            yield return new WaitForSeconds((1.3f - speed) / 10f);
        }

        if (contentsIndex >= dialogConfig.contents.Count)
        {
            // 显示对话选项
            ShowOption();
        }
        else
        {
            continueTip.SetActive(true);
        }

        isShowText = false;
    }

    /// <summary>
    /// 显示对话选项
    /// </summary>
    private void ShowOption()
    {
        dialogSkipButton.gameObject.SetActive(false);

        if (dialogConfig.nextDialog.Count > 0)
        {
            for (int i = 0; i < dialogConfig.nextDialog.Count; i++)
            {
                // 更新选择按钮文本并显示
                Transform optionObj = optionLabel.GetChild(i);
                optionObj.GetChild(0).GetComponent<Text>().text = dialogConfig.nextDialog[i].optionName;
                optionObj.gameObject.SetActive(true);
            }
        }
        else
        {
            continueTip.SetActive(true);
        }
    }

    /// <summary>
    /// 选择对应选项并继续对话
    /// </summary>
    private void OptionSelect(int index)
    {
        // 关闭对话选择
        CloseOption();
        if (index < dialogConfig.nextDialog.Count)
        {
            GameEventManager.Instance.dialogConfigEndEvent.Invoke(dialogConfig);

            dialogConfig = dialogConfig.nextDialog[index].dialog;
            contentsIndex = 0;

            DialogNext();
        }
        else
        {
            throw new System.Exception("对话选项越界: " + index);
        }
    }

    private void SkipDialog()
    {
        while (DialogNext()) ;
    }

    /// <summary>
    /// 关闭对话选项
    /// </summary>
    private void CloseOption()
    {
        dialogSkipButton.gameObject.SetActive(true);

        for (int i = 0; i < dialogConfig.nextDialog.Count; i++)
        {
            optionLabel.GetChild(i).gameObject.SetActive(false);
        }
    }
}

