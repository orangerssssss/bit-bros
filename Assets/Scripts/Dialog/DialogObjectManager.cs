using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 对话物体管理器, 用于查找场景中的对话物体(方便更新对话物体能够触发的对话)
/// </summary>
public class DialogObjectManager : MonoBehaviour
{
    private static DialogObjectManager instance;// 单例

    public static DialogObjectManager Instance
    {
        get
        {
            if (instance == null) instance = GameObject.FindObjectOfType<DialogObjectManager>();
            return instance;
        }
    }

    private Dictionary<string, DialogObject> dialogObjectDict;// 物体字典

    private void Awake()
    {
        dialogObjectDict = new Dictionary<string, DialogObject>();
    }

    /// <summary>
    /// 对话物注册
    /// </summary>
    public void Register(DialogObject dialogObject)
    {
        if (!dialogObjectDict.ContainsKey(dialogObject.dialogObjectName))
            dialogObjectDict.Add(dialogObject.dialogObjectName, dialogObject);
    }

    /// <summary>
    /// 对话物注销
    /// </summary>
    public void Unregister(DialogObject dialogObject)
    {
        dialogObjectDict.Remove(dialogObject.dialogObjectName);
    }

    /// <summary>
    /// 查找场景中的对话物
    /// </summary>
    /// <param name="dialogObjectName">对话物名称</param>
    public DialogObject GetDialogObject(string dialogObjectName)
    {
        if (dialogObjectDict.ContainsKey(dialogObjectName))
            return dialogObjectDict[dialogObjectName];
        else
            throw new System.Exception("对话物名称配置错误.");
    }
}
