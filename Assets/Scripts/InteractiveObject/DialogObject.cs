using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 可交互对话物体, 能够交互并进行对话
/// </summary>
public class DialogObject : InteractiveObject
{
    public string dialogObjectName;// 对话物名称
    [SerializeField]
    private DialogConfig commonDialog;// 普通对话(可重复触发此对话)
    private Queue<DialogConfig> specialDialogs = new Queue<DialogConfig>();// 特殊对话队列, 对话只能单次触发

    /// <summary>
    /// 交互并触发对话, 优先触发特殊对话
    /// </summary>
    public override void Interact()
    {
        if (interactable)
        {
            GameEventManager.Instance.beforeDialogEvent.Invoke(this);
            if (specialDialogs.Count > 0)
            {
                DialogDisplayer.Instance.StartDialog(this, specialDialogs.Dequeue(), transform.position, GetComponent<IdleAI>());
            }
            else
            {
                DialogDisplayer.Instance.StartDialog(this, commonDialog, transform.position, GetComponent<IdleAI>());
            }
        }
    }

    private void Start()
    {
        if (dialogObjectName != "")
            DialogObjectManager.Instance.Register(this);
    }

    /// <summary>
    /// 添加特殊对话
    /// </summary>
    public void AddSpecialDialog(DialogConfig dialog)
    {
        specialDialogs.Enqueue(dialog);
    }

    /// <summary>
    /// 设置普通对话
    /// </summary>
    public void SetCommonDialog(DialogConfig dialog)
    {
        commonDialog = dialog;
    }
}
