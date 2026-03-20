using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 任务提示UI, 用于显示当前任务及任务描述（缺少支线任务管理，无法存档）
/// </summary>
public class MainTaskTip : MonoBehaviour
{
    [SerializeField]
    private Image taskNameBackground;// 任务名的背景图, 用于动画效果
    [SerializeField]
    private Text taskName;// 任务名
    [SerializeField]
    private Text taskDescription;// 任务描述
    [SerializeField]
    private Text sideTask;

    private List<string> sideTaskList = new List<string>();

    /// <summary>
    /// 更新任务提示
    /// </summary>
    /// <param name="name">任务名</param>
    /// <param name="description">任务描述</param>
    public void UpdateTask(string name, string description)
    {
        taskNameBackground.color = new Color(taskNameBackground.color.r, taskNameBackground.color.g, taskNameBackground.color.b, 0);

        taskName.transform.localScale = new Vector3(taskName.transform.localScale.x, 1.0f, taskName.transform.localScale.z);

        taskDescription.color = new Color(taskDescription.color.r, taskDescription.color.g, taskDescription.color.b, 0);
        taskDescription.text = description;

        sideTask.color = new Color(sideTask.color.r, sideTask.color.g, sideTask.color.b, 0);

        StartCoroutine(UpdateTaskAnim(name));
    }

    public void UpdateTask(string description)
    {
        taskDescription.color = new Color(taskDescription.color.r, taskDescription.color.g, taskDescription.color.b, 0);
        taskDescription.text = description;

        sideTask.color = new Color(sideTask.color.r, sideTask.color.g, sideTask.color.b, 0);

        StartCoroutine(UpdateTaskAnim());
    }

    public void AddSideTask(string taskName, bool update = false)
    {
        sideTask.color = new Color(sideTask.color.r, sideTask.color.g, sideTask.color.b, 0);

        sideTaskList.Add(taskName);

        sideTask.text = "";
        foreach (string taskString in sideTaskList)
        {
            sideTask.text += "支线任务：" + taskString + "\n";
        }

        if (update) StartCoroutine(UpdateTaskAnim());
    }

    public void RemoveSideTask(string taskName)
    {

        if (sideTaskList.Contains(taskName))
        {
            sideTask.color = new Color(sideTask.color.r, sideTask.color.g, sideTask.color.b, 0);

            sideTaskList.Remove(taskName);

            sideTask.text = "";
            foreach (string taskString in sideTaskList)
            {
                sideTask.text += "支线任务：" + taskString + "\n";
            }

            StartCoroutine(UpdateTaskAnim());
        }
    }

    /// <summary>
    /// 任务提示更新的动画效果
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    private IEnumerator UpdateTaskAnim(string name)
    {
        Color backgroundColor = taskNameBackground.color;
        Vector3 scale = taskName.transform.localScale;
        Color descriptionColor = taskDescription.color;
        Color sideTaskColor = sideTask.color;

        while (scale.y > 0)
        {
            scale.y -= Time.deltaTime * 3.5f;
            taskName.transform.localScale = scale;
            yield return null;
        }
        scale.y = 0;
        taskName.transform.localScale = scale;
        taskName.text = "主线任务: " + name;
        while (scale.y < 1.0f)
        {
            scale.y += Time.deltaTime * 3.5f;
            taskName.transform.localScale = scale;
            yield return null;
        }
        scale.y = 1.0f;
        taskName.transform.localScale = scale;

        while(backgroundColor.a < 1.0f)
        {
            backgroundColor.a += Time.deltaTime * 4.5f;
            taskNameBackground.color = backgroundColor;
            yield return null;
        }
        while (backgroundColor.a > 0)
        {
            backgroundColor.a -= Time.deltaTime * 1.5f;
            taskNameBackground.color = backgroundColor;
            yield return null;
        }

        while (sideTaskColor.a < 1.0f)
        {
            descriptionColor.a += Time.deltaTime * 1.0f;
            sideTaskColor.a += Time.deltaTime * 1.0f;
            taskDescription.color = descriptionColor;
            sideTask.color = sideTaskColor;
            yield return null;
        }
    }

    private IEnumerator UpdateTaskAnim()
    {
        Color descriptionColor = taskDescription.color;
        Color sideTaskColor = sideTask.color;

        while (sideTaskColor.a < 1.0f)
        {
            descriptionColor.a += Time.deltaTime * 1.0f;
            sideTaskColor.a += Time.deltaTime * 1.0f;
            taskDescription.color = descriptionColor;
            sideTask.color = sideTaskColor;
            yield return null;
        }
    }
}
