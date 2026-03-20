using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 开始界面菜单
/// </summary>
public class MainMenu : MonoBehaviour
{
    [SerializeField]
    private Image openingPanel;// 纯色, 铺满镜头, 用于场景进入动画(逐渐变亮)
    [SerializeField]
    private GameObject mainMenu;// 主菜单父物体
    [SerializeField]
    private GameObject mainMenuSettings;// 菜单设置父物体
    [SerializeField]
    private Slider soundSlider;// 音量条
    [SerializeField]
    private Button continueButton;

    private Color openingPanelColor;// 用于场景进入动画(逐渐变亮)

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

        openingPanelColor = Color.black;
        openingPanel.color = openingPanelColor;
    }

    private void Start()
    {
        if (DataManager.Instance.hasSave)
        {
            continueButton.interactable = true;
        }
    }

    private void Update()
    {
        // 场景逐渐变亮
        if (openingPanelColor.a > 0)
        {
            openingPanelColor.a -= Time.deltaTime * 0.3f;
            openingPanel.color = openingPanelColor;
        }
    }

    /// <summary>
    /// 开始新游戏, 载入至主游戏场景
    /// </summary>
    public void NewGame()
    {
        DataManager.Instance.loadSave = false;
        SceneLoader.instance.LoadScene("MainScene", true);
    }

    /// <summary>
    /// 继续游戏, 载入至主游戏场景
    /// </summary>
    public void Continue()
    {
        DataManager.Instance.loadSave = true;
        SceneLoader.instance.LoadScene("MainScene", true);
    }

    /// <summary>
    /// 打开菜单设置界面
    /// </summary>
    public void MainMenuSettings()
    {
        mainMenu.SetActive(false);
        mainMenuSettings.SetActive(true);
        if (PlayerPrefs.HasKey("Sound"))
        {
            soundSlider.value = PlayerPrefs.GetFloat("Sound");
        }
        else
        {
            soundSlider.value = 0.5f;
            PlayerPrefs.SetFloat("Sound", 0.5f);
        }
    }

    /// <summary>
    /// 关闭菜单设置界面返回至主菜单界面
    /// </summary>
    public void BackToMainMenu()
    {
        mainMenu.SetActive(true);
        mainMenuSettings.SetActive(false);
    }

    /// <summary>
    /// 退出游戏
    /// </summary>
    public void Exit()
    {
        Application.Quit();
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
