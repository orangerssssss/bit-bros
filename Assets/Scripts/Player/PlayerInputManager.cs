using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家输入管理器, 管理玩家是否能够进行某些输入操作
/// </summary>
public class PlayerInputManager : MonoBehaviour
{
    private static PlayerInputManager instance;// 单例

    public static PlayerInputManager Instance
    {
        get
        {
            if (instance == null) instance = GameObject.FindObjectOfType<PlayerInputManager>();
            return instance;
        }
    }

    [HideInInspector] public PlayerMoveController moveController;// 玩家移动组件
    [HideInInspector] public ViewController viewController;// 玩家视角控制组件
    [HideInInspector] public InteractDetect interactDetect;// 玩家交互检测组件
    [HideInInspector] public PlayerCombatController combatController;// 玩家战斗控制组件

    private void Awake()
    {
        moveController = GetComponent<PlayerMoveController>();
        viewController = GetComponent<ViewController>();
        interactDetect = GetComponent<InteractDetect>();
        combatController = GetComponent<PlayerCombatController>();
        // Debug.Log($"PlayerInputManager Awake: moveController={(moveController!=null)}, viewController={(viewController!=null)}, interactDetect={(interactDetect!=null)}, combatController={(combatController!=null)}");
    }

    private void Start()
    {
        // Debug.Log($"PlayerInputManager Start: viewControllable={(viewController!=null?viewController.viewControllable.ToString():"no viewController")}");
        OpenAllInput();
    }

    /// <summary>
    /// 开启所有输入, 并隐藏鼠标
    /// </summary>
    public void OpenAllInput()
    {
        Debug.Log("PlayerInputManager.OpenAllInput called");
        if (moveController != null) moveController.playerMoveControllable = true;
        if (viewController != null) viewController.viewControllable = true;
        if (interactDetect != null) interactDetect.SetInteractable(true);
        if (combatController != null) combatController.playerSkillControllable = true;
        if (InventoryManager.Instance != null) InventoryManager.Instance.packageCanOpen = true;

        if (GameMenu.Instance != null) GameMenu.Instance.menuCanOpen = true;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        if (GameUIManager.Instance != null && GameUIManager.Instance.destinationMark != null) GameUIManager.Instance.destinationMark.markActive = true;
    }

    /// <summary>
    /// 关闭所有操作输入
    /// </summary>
    /// <param name="cursorVisible">鼠标是否可见</param>
    public void CloseAllInput(bool cursorVisible)
    {
        Debug.Log("PlayerInputManager.CloseAllInput called, cursorVisible=" + cursorVisible);
        if (moveController != null) moveController.playerMoveControllable = false;
        if (viewController != null) viewController.viewControllable = false;
        if (interactDetect != null) interactDetect.SetInteractable(false);
        if (combatController != null) combatController.playerSkillControllable = false;
        if (InventoryManager.Instance != null) InventoryManager.Instance.packageCanOpen = false;

        if (GameMenu.Instance != null) GameMenu.Instance.menuCanOpen = false;
        Cursor.visible = cursorVisible;
        if (!cursorVisible) Cursor.lockState = CursorLockMode.Locked;
        else Cursor.lockState = CursorLockMode.None;
        if (GameUIManager.Instance != null && GameUIManager.Instance.destinationMark != null) GameUIManager.Instance.destinationMark.markActive = false;
    }

    /// <summary>
    /// 关闭移动、视角、交互、战斗输入
    /// </summary>
    /// <param name="cursorVisible">鼠标是否可见</param>
    public void CloseControllInput(bool cursorVisible)
    {
        moveController.playerMoveControllable = false;
        viewController.viewControllable = false;
        interactDetect.SetInteractable(false);
        combatController.playerSkillControllable = false;

        Cursor.visible = cursorVisible;
        if (!cursorVisible) Cursor.lockState = CursorLockMode.Locked;
        else Cursor.lockState = CursorLockMode.None;
        GameUIManager.Instance.destinationMark.markActive = false;
    }
}
