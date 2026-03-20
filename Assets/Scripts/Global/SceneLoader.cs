using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 场景加载器, 用于加载场景并显示加载进度
/// </summary>
public class SceneLoader : MonoBehaviour
{
    public static SceneLoader instance;// 单例

    [SerializeField]
    private GameObject loadPanel;// 加载界面
    [SerializeField]
    private Slider slider;// 加载界面进度条

    private float targetValue;// 加载值

    private float currentVelocity;

    private void Awake()
    {
        // 跨场景单例
        if (instance == null)
        {
            instance = this;
            SceneManager.sceneLoaded += OnSceneLoaded;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        // 平滑过渡进度条值为加载值
        if (loadPanel)
        {
            slider.value = Mathf.SmoothDamp(slider.value, targetValue, ref currentVelocity, 0.15f);
        }
    }

    /// <summary>
    /// 加载场景
    /// </summary>
    /// <param name="sceneName">场景名</param>
    /// <param name="loadPanel">是否显示载入界面</param>
    public void LoadScene(string sceneName, bool loadPanel)
    {
        targetValue = 0;
        currentVelocity = 0;

        // 直接载入和异步载入
        if (!loadPanel)
            SceneManager.LoadScene(sceneName);
        else
            StartCoroutine(AsyncLoader(sceneName));
    }

    /// <summary>
    /// 异步载入界面显示
    /// </summary>
    /// <param name="sceneName">场景名</param>
    private IEnumerator AsyncLoader(string sceneName)
    {
        // 显示界面
        loadPanel.SetActive(true);

        yield return new WaitForSeconds(0.5f);

        // 异步载入
        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(sceneName);

        // 关闭完成后跳转
        asyncOperation.allowSceneActivation = false;

        // 如果场景未完成载入
        while (!asyncOperation.isDone)
        {
            // 更新进度条目标值
            targetValue = asyncOperation.progress;

            // 大于等于0.9, 说明载入完成正在等待
            if (asyncOperation.progress >= 0.9f)
            {
                yield return new WaitForSeconds(0.8f);

                // 进度条满, 跳出循环
                targetValue = 1;
                break;
            }
            yield return null;
        }
        yield return new WaitForSeconds(0.3f);

        // 允许跳转
        asyncOperation.allowSceneActivation = true;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 载入后关闭界面并初始化数值
        loadPanel.SetActive(false);
        slider.value = 0;
        targetValue = 0;
    }
}
