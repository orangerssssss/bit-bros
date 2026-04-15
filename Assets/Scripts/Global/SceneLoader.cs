using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Cinemachine;
using System.Reflection;

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
        // 尝试将持久化的Player移动到场景中的SpawnPoint/RespawnPoint
        TryMovePersistentPlayerToSpawn();
    }

    private void TryMovePersistentPlayerToSpawn()
    {
        // 尝试寻找常见的重生/出生点对象名
        GameObject spawn = GameObject.Find("SpawnPoint") ?? GameObject.Find("RespawnPoint") ?? GameObject.Find("PlayerSpawn");
        if (spawn == null)
        {
            Debug.Log("SceneLoader: no spawn point found in scene");
            return;
        }

        Transform s = spawn.transform;
        // 优先使用 PlayerInputManager 的 moveController
        if (PlayerInputManager.Instance != null && PlayerInputManager.Instance.moveController != null)
        {
            PlayerInputManager.Instance.moveController.SetPositionAndRotation(s);
            Debug.Log($"SceneLoader: moved player to spawn ({spawn.name}) via PlayerInputManager");
            // Ensure weapon visible after move
            if (PlayerInputManager.Instance.combatController != null)
            {
                PlayerInputManager.Instance.combatController.SetWeaponVisible(true);
            }

            // Re-init view controller and rewire Cinemachine to follow player
            TryInitializeCameraForPlayer(PlayerInputManager.Instance.viewController, GameObject.Find("PlayerControl"), GameObject.Find("Player"));
            return;
        }

        // 备用方案：直接寻找 PlayerControl 或 Player 对象并设置位置
        GameObject pc = GameObject.Find("PlayerControl");
        GameObject player = GameObject.Find("Player");

        if (pc != null)
        {
            var cc = player != null ? player.GetComponent<CharacterController>() : null;
            if (cc != null) cc.enabled = false;
            pc.transform.SetPositionAndRotation(s.position, s.rotation);
            if (cc != null) cc.enabled = true;
            Debug.Log($"SceneLoader: moved PlayerControl to spawn ({spawn.name})");
            // Try to re-enable weapon on player
            if (PlayerInputManager.Instance != null && PlayerInputManager.Instance.combatController != null)
            {
                PlayerInputManager.Instance.combatController.SetWeaponVisible(true);
            }
            else
            {
                var pcCombat = pc.GetComponentInChildren<PlayerCombatController>();
                if (pcCombat != null) pcCombat.SetWeaponVisible(true);
            }

            // Re-init view controller and rewire Cinemachine to follow player
            TryInitializeCameraForPlayer(PlayerInputManager.Instance != null ? PlayerInputManager.Instance.viewController : pc.GetComponentInChildren<ViewController>(), pc, player);
            return;
        }

        if (player != null)
        {
            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            player.transform.SetPositionAndRotation(s.position, s.rotation);
            if (cc != null) cc.enabled = true;
            Debug.Log($"SceneLoader: moved Player to spawn ({spawn.name})");
            // Try to re-enable weapon on player
            var pCombat = player.GetComponentInChildren<PlayerCombatController>();
            if (pCombat != null)
            {
                pCombat.SetWeaponVisible(true);
            }
            else if (PlayerInputManager.Instance != null && PlayerInputManager.Instance.combatController != null)
            {
                PlayerInputManager.Instance.combatController.SetWeaponVisible(true);
            }

            // Re-init view controller and rewire Cinemachine to follow player
            TryInitializeCameraForPlayer(PlayerInputManager.Instance != null ? PlayerInputManager.Instance.viewController : player.GetComponentInChildren<ViewController>(), pc, player);
            return;
        }

        Debug.Log("SceneLoader: no player object found to move to spawn");
    }

    private void TryInitializeCameraForPlayer(ViewController view, GameObject pc, GameObject player)
    {
        Transform preferredTarget = null;

        if (view != null)
        {
            try { view.InitCamera(); } catch { }
            if (view.playerViewPoint != null) preferredTarget = view.playerViewPoint;
            else if (view.playerVCamera != null) preferredTarget = view.playerVCamera;
            else preferredTarget = view.transform;
            view.viewControllable = true;

            // Apply per-scene camera settings similar to PlayerSpawner
            try
            {
                string sceneName = SceneManager.GetActiveScene().name;
                if (sceneName == "Imagination")
                {
                    view.SetSensitivity(0.9f, 0.55f);
                    view.SetViewDistance(1.8f);
                }
                else
                {
                    view.SetSensitivity(1.0f, 0.6f);
                    view.SetViewDistance(3.2f);
                }
            }
            catch { }
        }

        if (preferredTarget == null)
        {
            if (pc != null) preferredTarget = pc.transform;
            else if (player != null) preferredTarget = player.transform;
        }

        if (preferredTarget == null) return;

        // Ensure CinemachineBrain exists on main camera
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            var brain = mainCam.GetComponent<CinemachineBrain>();
            if (brain == null) mainCam.gameObject.AddComponent<CinemachineBrain>();
        }

        // Rewire all virtual cameras in scene to follow preferred target
        var vcams = GameObject.FindObjectsOfType<CinemachineVirtualCamera>();
        foreach (var vcam in vcams)
        {
            try
            {
                vcam.Follow = preferredTarget;
                // Do NOT set LookAt to preferredTarget to avoid rotation feedback loops with ViewController
                vcam.LookAt = null;
                vcam.Priority = 1000;

                // Try to set transposer binding mode to a world-space mode to keep camera offset stable
                try
                {
                    var transposer = vcam.GetCinemachineComponent<CinemachineTransposer>();
                    if (transposer != null)
                    {
                        var field = transposer.GetType().GetField("m_BindingMode", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                        if (field != null)
                        {
                            var enumType = field.FieldType;
                            string[] tryNames = new[] { "WorldSpace", "SimpleFollowWithWorldUp", "LockToTargetWithWorldUp" };
                            foreach (var name in tryNames)
                            {
                                try
                                {
                                    var parsed = System.Enum.Parse(enumType, name);
                                    field.SetValue(transposer, parsed);
                                    break;
                                }
                                catch { }
                            }
                        }
                    }
                }
                catch { }
            }
            catch { }
        }

        Debug.Log($"SceneLoader: rewired {vcams.Length} Cinemachine vcam(s) to follow {preferredTarget.name}");

        // Ensure player movement/combat systems are initialized and weapon is re-applied
        try
        {
            if (PlayerInputManager.Instance != null)
            {
                // Initialize move controller rotation target
                if (PlayerInputManager.Instance.moveController != null)
                {
                    Quaternion rot = (pc != null) ? pc.transform.rotation : (player != null ? player.transform.rotation : Quaternion.identity);
                    try { PlayerInputManager.Instance.moveController.InitMove(rot); } catch { }
                }

                // Init combat and re-apply equipped weapon model
                if (PlayerInputManager.Instance.combatController != null)
                {
                    try { PlayerInputManager.Instance.combatController.InitCombat(); } catch { }
                    try { PlayerInputManager.Instance.combatController.SwitchWeapon(PlayerInputManager.Instance.combatController.equipedWeaponID); } catch { }
                    try { PlayerInputManager.Instance.combatController.SetWeaponVisible(true); } catch { }
                }
            }
        }
        catch { }
    }
}
