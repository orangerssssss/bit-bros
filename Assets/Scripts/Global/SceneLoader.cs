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

    // If we create runtime UI because inspector refs were missing
    private bool runtimeCreatedLoadUI = false;

    // Temp transforms created to pass into SetPositionAndRotation (keep alive until reposition finish)
    private List<GameObject> tempSpawnTransforms = new List<GameObject>();

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
        if (loadPanel && slider != null)
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
        // 显示界面（如果已配置），或者在运行时创建简单的 loading UI 作为后备
        if (loadPanel != null)
        {
            loadPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("SceneLoader: loadPanel not assigned, creating runtime loading UI fallback.");
            // Create minimal Canvas + Panel + Slider so player sees loading feedback
            GameObject canvasGO = new GameObject("RuntimeLoadCanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            GameObject panel = new GameObject("RuntimeLoadPanel");
            panel.transform.SetParent(canvasGO.transform, false);
            var img = panel.AddComponent<UnityEngine.UI.Image>();
            img.color = new Color(0f, 0f, 0f, 0.85f);
            var prt = panel.GetComponent<RectTransform>();
            prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one; prt.offsetMin = Vector2.zero; prt.offsetMax = Vector2.zero;

            GameObject sliderGO = new GameObject("RuntimeLoadSlider");
            sliderGO.transform.SetParent(panel.transform, false);
            var runtimeSlider = sliderGO.AddComponent<UnityEngine.UI.Slider>();
            var srt = sliderGO.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.1f, 0.06f); srt.anchorMax = new Vector2(0.9f, 0.12f); srt.offsetMin = Vector2.zero; srt.offsetMax = Vector2.zero;

            // Keep references
            loadPanel = canvasGO;
            slider = runtimeSlider;
            runtimeCreatedLoadUI = true;
        }

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
        // 载入后关闭界面并初始化数值（如果已配置）
        if (loadPanel != null) loadPanel.SetActive(false);
        if (slider != null) slider.value = 0;
        targetValue = 0;

        // If we created runtime UI, destroy it now
        if (runtimeCreatedLoadUI && loadPanel != null)
        {
            Destroy(loadPanel);
            loadPanel = null;
            slider = null;
            runtimeCreatedLoadUI = false;
        }
        // 尝试将持久化的Player移动到场景中的SpawnPoint/RespawnPoint
        TryMovePersistentPlayerToSpawn();

        // Clean up any temporary transforms created to assist SetPositionAndRotation
        if (tempSpawnTransforms.Count > 0)
        {
            StartCoroutine(CleanupTempSpawnTransforms());
        }
    }

    private IEnumerator CleanupTempSpawnTransforms()
    {
        yield return new WaitForSeconds(0.5f);
        foreach (var go in tempSpawnTransforms)
        {
            if (go != null) Destroy(go);
        }
        tempSpawnTransforms.Clear();
    }

    private void TryMovePersistentPlayerToSpawn()
    {
        // Do not move persistent player into UI/menu scenes
        try
        {
            string sceneName = SceneManager.GetActiveScene().name;
            if (sceneName == "MenuScene")
            {
                Debug.Log("SceneLoader: skipping persistent player move for MenuScene");
                return;
            }
        }
        catch { }

        // If we're loading a saved game and a saved player transform exists, restore it directly
        try
        {
            if (DataManager.Instance != null && DataManager.Instance.loadSave && DataManager.Instance.hasSave && DataManager.Instance.saveData != null && DataManager.Instance.saveData.gameProcessSaveData != null)
            {
                var gp = DataManager.Instance.saveData.gameProcessSaveData;
                if (gp.hasSavedPlayerTransform)
                {
                    var savedPos = new Vector3(gp.lastPlayerPosX, gp.lastPlayerPosY, gp.lastPlayerPosZ);
                    var savedRot = new Quaternion(gp.lastPlayerRotX, gp.lastPlayerRotY, gp.lastPlayerRotZ, gp.lastPlayerRotW);

                    if (PlayerInputManager.Instance != null && PlayerInputManager.Instance.moveController != null)
                    {
                        GameObject tmp = new GameObject("SavedSpawnTransform");
                        tmp.transform.SetPositionAndRotation(savedPos, savedRot);
                        PlayerInputManager.Instance.moveController.SetPositionAndRotation(tmp.transform);
                        tempSpawnTransforms.Add(tmp);
                        Debug.Log("SceneLoader: moved persistent player to saved position via DataManager save data");
                        // ensure weapon visible and camera
                        if (PlayerInputManager.Instance.combatController != null)
                        {
                            PlayerInputManager.Instance.combatController.SetWeaponVisible(true);
                            StartCoroutine(ReenableWeaponsDelayed(0.2f));
                        }
                        TryInitializeCameraForPlayer(PlayerInputManager.Instance.viewController, GameObject.Find("PlayerControl"), GameObject.Find("Player"));
                        return;
                    }
                    // fallback: move scene PlayerControl or Player directly
                    var scenePc = GameObject.Find("PlayerControl");
                    var scenePlayer = GameObject.Find("Player");
                    if (scenePc != null)
                    {
                        var cc = scenePlayer != null ? scenePlayer.GetComponent<CharacterController>() : null;
                        if (cc != null) cc.enabled = false;
                        scenePc.transform.SetPositionAndRotation(savedPos, savedRot);
                        if (cc != null) cc.enabled = true;
                        Debug.Log("SceneLoader: moved PlayerControl to saved position via DataManager save data");
                        TryInitializeCameraForPlayer(PlayerInputManager.Instance != null ? PlayerInputManager.Instance.viewController : scenePc.GetComponentInChildren<ViewController>(), scenePc, scenePlayer);
                        return;
                    }
                    if (scenePlayer != null)
                    {
                        var cc = scenePlayer.GetComponent<CharacterController>();
                        if (cc != null) cc.enabled = false;
                        scenePlayer.transform.SetPositionAndRotation(savedPos, savedRot);
                        if (cc != null) cc.enabled = true;
                        Debug.Log("SceneLoader: moved Player to saved position via DataManager save data");
                        TryInitializeCameraForPlayer(PlayerInputManager.Instance != null ? PlayerInputManager.Instance.viewController : scenePlayer.GetComponentInChildren<ViewController>(), pc: null, player: scenePlayer);
                        return;
                    }
                }
            }
        }
        catch { }

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
                    // re-enable after a short delay in case triggers (NonCombatField) just fired
                    StartCoroutine(ReenableWeaponsDelayed(0.2f));
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
                StartCoroutine(ReenableWeaponsDelayed(0.2f));
            }
            else if (PlayerInputManager.Instance != null && PlayerInputManager.Instance.combatController != null)
            {
                PlayerInputManager.Instance.combatController.SetWeaponVisible(true);
                StartCoroutine(ReenableWeaponsDelayed(0.2f));
            }

            // Re-init view controller and rewire Cinemachine to follow player
            TryInitializeCameraForPlayer(PlayerInputManager.Instance != null ? PlayerInputManager.Instance.viewController : player.GetComponentInChildren<ViewController>(), pc, player);
            return;
        }

        Debug.Log("SceneLoader: no player object found to move to spawn");
    }

    private IEnumerator ReenableWeaponsDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        PlayerCombatController pc = null;
        if (PlayerInputManager.Instance != null) pc = PlayerInputManager.Instance.combatController;
        if (pc == null)
        {
            var player = GameObject.Find("Player") ?? GameObject.Find("PlayerControl");
            if (player != null) pc = player.GetComponentInChildren<PlayerCombatController>();
        }
        if (pc != null)
        {
            pc.SetWeaponVisible(true);
            Debug.Log("SceneLoader: Reenabled weapon visibility after delay");
        }
    }

    private IEnumerator RestoreSavedPlayerPosition(Vector3 savedPos, Quaternion savedRot)
    {
        // Wait until PlayerInputManager and its controllers are initialized (or timeout)
        int attempts = 0;
        while ((PlayerInputManager.Instance == null || PlayerInputManager.Instance.moveController == null) && attempts < 120)
        {
            attempts++;
            yield return null;
        }

        // If moveController is available, use it to set position safely
        if (PlayerInputManager.Instance != null && PlayerInputManager.Instance.moveController != null)
        {
            GameObject tmp = new GameObject("SavedSpawnTransform");
            tmp.transform.SetPositionAndRotation(savedPos, savedRot);
            tempSpawnTransforms.Add(tmp);

            // Set position using moveController helper which handles CharacterController enable/disable
            PlayerInputManager.Instance.moveController.SetPositionAndRotation(tmp.transform);

            // small delay to let CharacterController re-enable and systems initialize
            yield return new WaitForSeconds(0.12f);

            // Ensure weapon and camera are initialized
            if (PlayerInputManager.Instance.combatController != null)
            {
                try { PlayerInputManager.Instance.combatController.SetWeaponVisible(true); } catch { }
                StartCoroutine(ReenableWeaponsDelayed(0.2f));
            }

            TryInitializeCameraForPlayer(PlayerInputManager.Instance.viewController, GameObject.Find("PlayerControl"), GameObject.Find("Player"));

            // Re-enable player input and UI state
            try { PlayerInputManager.Instance.OpenAllInput(); } catch { }
            yield break;
        }

        // Fallback: directly move objects in scene if moveController not available
        var scenePc = GameObject.Find("PlayerControl");
        var scenePlayer = GameObject.Find("Player");
        if (scenePc != null)
        {
            var cc = scenePlayer != null ? scenePlayer.GetComponent<CharacterController>() : null;
            if (cc != null) cc.enabled = false;
            scenePc.transform.SetPositionAndRotation(savedPos, savedRot);
            if (cc != null) cc.enabled = true;
            TryInitializeCameraForPlayer(PlayerInputManager.Instance != null ? PlayerInputManager.Instance.viewController : scenePc.GetComponentInChildren<ViewController>(), scenePc, scenePlayer);
            if (PlayerInputManager.Instance != null) try { PlayerInputManager.Instance.OpenAllInput(); } catch { }
            yield break;
        }
        if (scenePlayer != null)
        {
            var cc = scenePlayer.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            scenePlayer.transform.SetPositionAndRotation(savedPos, savedRot);
            if (cc != null) cc.enabled = true;
            TryInitializeCameraForPlayer(PlayerInputManager.Instance != null ? PlayerInputManager.Instance.viewController : scenePlayer.GetComponentInChildren<ViewController>(), null, scenePlayer);
            if (PlayerInputManager.Instance != null) try { PlayerInputManager.Instance.OpenAllInput(); } catch { }
            yield break;
        }
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
                    // Slightly increase camera distance for Imagination (scene 2)
                    view.SetViewDistance(2.2f);
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

                // Add CinemachineCollider if available to help avoid camera clipping
                try
                {
                    var existing = vcam.GetComponent<Cinemachine.CinemachineCollider>();
                    if (existing == null)
                    {
                        vcam.gameObject.AddComponent<Cinemachine.CinemachineCollider>();
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

                    // Prefer applying the equipped weapon based on Inventory or persisted save data
                    int weaponToApply = -1;
                    try
                    {
                        if (InventoryManager.Instance != null && InventoryManager.Instance.WeaponID > 0)
                        {
                            weaponToApply = InventoryManager.Instance.WeaponID;
                        }
                        else if (DataManager.Instance != null && DataManager.Instance.saveData != null && DataManager.Instance.saveData.inventorySaveData != null && DataManager.Instance.saveData.inventorySaveData.weaponID > 0)
                        {
                            weaponToApply = DataManager.Instance.saveData.inventorySaveData.weaponID;
                        }
                        else
                        {
                            weaponToApply = PlayerInputManager.Instance.combatController.equipedWeaponID;
                        }
                    }
                    catch { weaponToApply = PlayerInputManager.Instance.combatController.equipedWeaponID; }

                    if (weaponToApply > 0)
                    {
                        try { PlayerInputManager.Instance.combatController.SwitchWeapon(weaponToApply); } catch { }
                    }
                    try { PlayerInputManager.Instance.combatController.SetWeaponVisible(true); } catch { }
                }
            }
        }
        catch { }
    }
}
