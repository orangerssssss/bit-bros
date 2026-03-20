using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 故事设定展示器, 用于游戏开始时展示故事背景
/// </summary>
public class StorySetting : MonoBehaviour
{
    [SerializeField]
    private GameObject setting;// 故事设定的父物体
    [SerializeField]
    private Image settingImage;// 故事设定的图片
    [SerializeField]
    private Text settingText;// 故事设定的文本

    [SerializeField, TextArea(2, 10)]
    private string text;// 故事设定

    /// <summary>
    /// 展示故事设定
    /// </summary>
    public void Show()
    {
        settingImage.color = new Color(settingImage.color.r, settingImage.color.g, settingImage.color.b, 0);
        settingText.color = new Color(settingText.color.r, settingText.color.g, settingText.color.b, 1.0f);
        settingText.text = "";
        StartCoroutine(ShowSetting());
    }

    /// <summary>
    /// 展示动画的主逻辑控制
    /// </summary>
    private IEnumerator ShowSetting()
    {
        setting.SetActive(true);

        yield return null;

        PlayerInputManager.Instance.CloseAllInput(false);

        StartCoroutine(ShowSettingImage());
        yield return StartCoroutine(ShowSettingText());

        yield return new WaitForSeconds(5.0f);

        Color imageColor = settingImage.color;
        Color textColor = settingText.color;
        while (imageColor.a > 0.1f)
        {
            imageColor.a -= Time.deltaTime * 0.3f;
            textColor.a -= Time.deltaTime * 0.3f;
            settingImage.color = imageColor;
            settingText.color = textColor;
            yield return null;
        }
        setting.SetActive(false);

        PlayerInputManager.Instance.OpenAllInput();
        GameEventManager.Instance.storySettingEndEvent.Invoke();
    }

    /// <summary>
    /// 展示动画的图片显示控制
    /// </summary>
    private IEnumerator ShowSettingImage()
    {
        Color color = settingImage.color;
        while(color.a < 1.0f)
        {
            color.a += Time.deltaTime * 0.15f;
            settingImage.color = color;
            yield return null;
        }
        color.a = 1.0f;
        settingImage.color = color;
    }

    /// <summary>
    /// 展示动画的文本显示控制
    /// </summary>
    private IEnumerator ShowSettingText()
    {
        yield return new WaitForSeconds(3.0f);
        for (int i = 0; i < text.Length; i++)
        {
            settingText.text += text[i];
            yield return new WaitForSeconds(0.05f);
        }
    }
}
