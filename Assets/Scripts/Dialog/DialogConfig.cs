using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 对话配置文件(ScriptableObject)
/// </summary>
[CreateAssetMenu(fileName = "DialogConfig", menuName = "Game Config/New DialogConfig")]
public class DialogConfig : ScriptableObject
{
    public List<DialogContent> contents;// 该对话的对话内容列表
    public List<DialogSelection> nextDialog;// 之后可能衔接的对话(对话选项, 玩家选择后进入下一对话)
}

/// <summary>
/// 对话选项
/// </summary>
[System.Serializable]
public class DialogSelection
{
    public string optionName;// 选项名
    public DialogConfig dialog;// 选项对应的对话
}

/// <summary>
/// 对话内容, 包含对话物名称 对话内容
/// </summary>
[System.Serializable]
public class DialogContent
{
    public string name;// 名称
    [TextArea(2, 10)]
    public string content;// 内容
}


