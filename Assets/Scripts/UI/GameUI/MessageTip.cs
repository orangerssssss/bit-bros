using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 消息提示UI, 显示消息提示
/// </summary>
public class MessageTip : MonoBehaviour
{
    [SerializeField]
    private Text text;// 文本
    private Vector3 scale;// 缩放, 用于动画控制

    private void Awake()
    {
        scale = transform.localScale;
    }

    /// <summary>
    /// 显示消息提示
    /// </summary>
    /// <param name="tip">提示内容</param>
    public void ShowTip(string tip)
    {
        StopAllCoroutines();

        scale.y = 0;
        transform.localScale = scale;
        text.text = tip;

        StartCoroutine(TipAnim());
    }

    /// <summary>
    /// 消息提示动画效果
    /// </summary>
    private IEnumerator TipAnim()
    {
        float speed = 3.5f;

        while(scale.y < 1.0f)
        {
            scale.y += Time.deltaTime * speed;
            if (scale.y > 1.0f) scale.y = 1.0f;
            transform.localScale = scale;

            yield return null;
        }

        yield return new WaitForSeconds(2.5f);

        while (scale.y > 0)
        {
            scale.y -= Time.deltaTime * speed;
            if (scale.y < 0) scale.y = 0;
            transform.localScale = scale;

            yield return null;
        }
    }
}
