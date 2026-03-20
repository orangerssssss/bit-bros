using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 获得物品提示, 控制自身的显示与消失, 最后入池
/// </summary>
public class ItemTip : MonoBehaviour
{
    [SerializeField]
    private Text text;// 文本
    private Color textColor;// 文本颜色, 用于动画控制

    private void Awake()
    {
        textColor = text.color;
    }

    /// <summary>
    /// 显示
    /// </summary>
    /// <param name="name">物品名</param>
    /// <param name="quantity">数量</param>
    public void Show(string name, int quantity)
    {
        text.text = "获得 " + name + "*" + quantity;
        transform.SetAsLastSibling();
        gameObject.SetActive(true);
        StartCoroutine("Fade");
    }

    /// <summary>
    /// 逐渐消失
    /// </summary>
    private IEnumerator Fade()
    {
        yield return new WaitForSeconds(4.0f);
        while (textColor.a > 0.3f)
        {
            textColor.a -= Time.deltaTime * 1.1f;
            text.color = textColor;
            yield return null;
        }
        gameObject.SetActive(false);
        textColor.a = 1.0f;
        text.color = textColor;
        GameUIManager.Instance.ItemTipEnqueue(this);
    }
}
