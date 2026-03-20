using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 对话计时器
/// </summary>
public class StoryTimer : MonoBehaviour
{
    public Text timerText;

    private string timerName;
    private float timer = 0;

    private UnityAction timerAction;

    private void Update()
    {
        if (timer > 0)
        {
            timer -= Time.deltaTime;
            timerText.text = $"{timerName}：{Mathf.Max(0, (int)timer)}秒";

            if (timer <= 0)
            {
                if (timerAction != null) timerAction.Invoke();
                timerText.text = "";
            }
        }
    }

    public void StartTimer(string name, float seconds, UnityAction action)
    {
        timerName = name;
        timer = seconds;

        timerAction = action;
    }

    public void StopTimer()
    {
        timerName = "";
        timer = 0;
        timerText.text = "";

        timerAction = null;
    }
}
