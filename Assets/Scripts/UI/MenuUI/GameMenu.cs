using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 游戏内菜单界面
/// </summary>
public class GameMenu : MonoBehaviour
{
    private static GameMenu instance;// 单例

    public static GameMenu Instance
    {
        get
        {
            if (instance == null) instance = GameObject.FindObjectOfType<GameMenu>();
            return instance;
        }
    }

    public GameObject menuPanel;// 菜单父物体
    [SerializeField]
    private GameObject menu;// 一级菜单父物体
    [SerializeField]
    private GameObject exitConfirm;// 退出确认(二级菜单)父物体
    [SerializeField]
    private Slider soundSlider;// 音量条

    public bool menuCanOpen = true;// 是否能够开启菜单

    private void Awake()
    {
        // 注册事件, 当音量条变动时设置音量大小
        soundSlider.onValueChanged.AddListener(SetSoundVolumn);

        if (PlayerPrefs.HasKey("Sound"))
        {
            SetSoundVolumn(PlayerPrefs.GetFloat("Sound"));
        }
        else
        {
            SetSoundVolumn(0.5f);
        }
    }

    private void Update()
    {
        // 菜单开关控制
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!GameUIManager.Instance.dialog.activeSelf && !GameUIManager.Instance.package.activeSelf && !GameUIManager.Instance.villageStore.activeSelf)
            {
                if (!menuPanel.activeSelf)
                {
                    if (menuCanOpen) OpenMenu();
                }
                else
                {
                    CloseMenu();
                }
            }
        }
    }

    /// <summary>
    /// 打开菜单
    /// </summary>
    public void OpenMenu()
    {
        menuPanel.SetActive(true);
        menu.SetActive(true);
        exitConfirm.SetActive(false);
        if (PlayerPrefs.HasKey("Sound"))
        {
            soundSlider.value = PlayerPrefs.GetFloat("Sound");
        }
        else
        {
            soundSlider.value = 0.5f;
            PlayerPrefs.SetFloat("Sound", 0.5f);
        }
        PlayerInputManager.Instance.CloseAllInput(true);
    }

    /// <summary>
    /// 关闭菜单
    /// </summary>
    public void CloseMenu()
    {
        menuPanel.SetActive(false);
        PlayerInputManager.Instance.OpenAllInput();
    }

    /// <summary>
    /// 弹出退出确认界面
    /// </summary>
    public void Exit()
    {
        menu.SetActive(false);
        exitConfirm.SetActive(true);
    }

    /// <summary>
    /// 退出至开始界面
    /// </summary>
    public void ConfirmExit()
    {
        SceneLoader.instance.LoadScene("MenuScene", true);
    }

    /// <summary>
    /// 关闭退出确认界面, 回到菜单一级界面
    /// </summary>
    public void CancelExit()
    {
        menu.SetActive(true);
        exitConfirm.SetActive(false);
    }

    /// <summary>
    /// 设置音量大小
    /// </summary>
    /// <param name="value">音量值</param>
    private void SetSoundVolumn(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat("Sound", value);
    }
}
