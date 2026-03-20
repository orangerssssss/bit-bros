using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 操作提示UI, 用于开启 关闭操作提示
/// </summary>
public class ControlTip : MonoBehaviour
{
    [SerializeField]
    private Text tipText;// 提示文本
    [SerializeField]
    private Image tipBackground;
    private bool isActive = false;// 是否处于开启状态

    private float backgroundAlpha;
    private float currentAlpha;
    private bool hasLast = false;

    private void Awake()
    {
        backgroundAlpha = tipBackground.color.a;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            if (isActive) CloseTip();
            else if (hasLast) ShowTip();
        }

        Color currentColor = tipBackground.color;
        currentColor.a = Mathf.PingPong(currentAlpha, backgroundAlpha);
        tipBackground.color = currentColor;

        currentAlpha += Time.deltaTime * 1.5f;
    }

    /// <summary>
    /// 开启操作提示
    /// </summary>
    /// <param name="tip">提示内容</param>
    public void ShowTip(string tip)
    {
        hasLast = true;
        isActive = true;
        tipText.text = tip + "\n\n（‘T’ 关闭此提示界面）";
        StopAllCoroutines();
        StartCoroutine("ShowTipAnim");
    }

    private void ShowTip()
    {
        isActive = true;
        StopAllCoroutines();
        StartCoroutine("ShowTipAnim");
    }

    /// <summary>
    /// 关闭操作提示
    /// </summary>
    private void CloseTip()
    {
        isActive = false;
        StopAllCoroutines();
        StartCoroutine("CloseTipAnim");
    }

    /// <summary>
    /// 显示动画
    /// </summary>
    private IEnumerator ShowTipAnim()
    {
        RectTransform rect = GetComponent<RectTransform>();
        Vector2 pos = new Vector2(120, rect.anchoredPosition.y);
        rect.anchoredPosition = pos;
        while (pos.x > -12)
        {
            pos.x -= Time.deltaTime * 180.0f;
            rect.anchoredPosition = pos;
            yield return null;
        }

        pos.x = -12;
        rect.anchoredPosition = pos;
    }

    /// <summary>
    /// 关闭动画
    /// </summary>
    private IEnumerator CloseTipAnim()
    {
        RectTransform rect = GetComponent<RectTransform>();
        Vector2 pos = rect.anchoredPosition;
        while (pos.x < 120)
        {
            pos.x += Time.deltaTime * 180.0f;
            rect.anchoredPosition = pos;
            yield return null;
        }
    }
}
