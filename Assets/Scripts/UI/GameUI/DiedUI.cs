using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 玩家死亡UI, 包括点击界面重生
/// </summary>
public class DiedUI : MonoBehaviour
{
    [SerializeField]
    private Image diedPanel;// 死亡界面
    [SerializeField]
    private Text text;// 死亡文本
    [SerializeField]
    private Text tip;// 复活提示文本

    private void Start()
    {
        // 为按钮注册复活事件
        diedPanel.GetComponent<Button>().onClick.AddListener(
            () => { Respawn(); });
    }

    /// <summary>
    /// 重生(按钮点击事件)
    /// </summary>
    public void Respawn()
    {
        diedPanel.gameObject.SetActive(false);

        // 仅从最近存档点继续：重载当前场景并启用 loadSave
        var dm = DataManager.Instance;
        if (dm != null)
        {
            dm.loadSave = dm.hasSave;
        }
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// 死亡
    /// </summary>
    public void Died()
    {
        diedPanel.gameObject.SetActive(true);
        diedPanel.GetComponent<Button>().enabled = false;

        StartCoroutine("DiedAnim");
    }

    /// <summary>
    /// 死亡UI动画
    /// </summary>
    private IEnumerator DiedAnim()
    {
        Color backgroundColor = diedPanel.color;
        backgroundColor.a = 0;
        diedPanel.color = backgroundColor;

        Color textColor = text.color;
        textColor.a = 0;
        text.color = textColor;

        Color tipColor = tip.color;
        tipColor.a = 0;
        tip.color = tipColor;

        yield return new WaitForSeconds(2.5f);

        while (backgroundColor.a < 0.5f)
        {
            backgroundColor.a += Time.deltaTime * 0.5f;
            diedPanel.color = backgroundColor;

            textColor.a += Time.deltaTime * 1.0f;
            text.color = textColor;
            yield return null;
        }

        yield return new WaitForSeconds(0.5f);
        diedPanel.GetComponent<Button>().enabled = true;// 此处激活按钮, 可以点击重生
        Cursor.visible = true;

        while (tipColor.a < 1.0f)
        {
            tipColor.a += Time.deltaTime * 1.0f;
            tip.color = tipColor;
            yield return null;
        }
    }
}
