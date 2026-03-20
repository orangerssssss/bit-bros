using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 玩家受伤UI(屏幕泛红), 用于提示玩家受到伤害
/// </summary>
public class DamagedUI : MonoBehaviour
{
    [SerializeField]
    private Image damagedImage;// 泛红图片

    /// <summary>
    /// 显示受伤UI
    /// </summary>
    /// <param name="small">显示强度是否为小</param>
    public void Damaged(bool small)
    {
        StopAllCoroutines();
        if (small) StartCoroutine("DamagedSmallAnim");
        else StartCoroutine("DamagedBigAnim");
    }

    /// <summary>
    /// 受伤UI动画, 强度小
    /// </summary>
    private IEnumerator DamagedSmallAnim()
    {
        Color color = damagedImage.color;
        color.a = 0;
        damagedImage.color = color;

        while (color.a < 0.55f)
        {
            color.a += Time.deltaTime * 3.2f;
            damagedImage.color = color;
            yield return null;
        }
        while (color.a > 0)
        {
            color.a -= Time.deltaTime * 1.6f;
            damagedImage.color = color;
            yield return null;
        }
    }

    /// <summary>
    /// 受伤UI动画, 强度大
    /// </summary>
    private IEnumerator DamagedBigAnim()
    {
        Color color = damagedImage.color;
        color.a = 0;
        damagedImage.color = color;

        while (color.a < 0.85f)
        {
            color.a += Time.deltaTime * 3.8f;
            damagedImage.color = color;
            yield return null;
        }
        while (color.a > 0)
        {
            color.a -= Time.deltaTime * 2.0f;
            damagedImage.color = color;
            yield return null;
        }
    }
}
