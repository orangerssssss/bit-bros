using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 梦境场景初始化器 - 确保所有全局管理器都已初始化
/// 在梦境场景中放一个空的 GameObject，挂上这个脚本
/// </summary>
public class ImaginationSceneInitializer : MonoBehaviour
{
    private void Awake()
    {
        // 确保 DataManager 已初始化
        if (DataManager.Instance == null)
        {
            Debug.LogError("ImaginationSceneInitializer: DataManager 初始化失败！");
        }
        else
        {
            Debug.Log("ImaginationSceneInitializer: DataManager 已就绪");
        }

        // 确保 GameUIManager 已初始化
        if (GameUIManager.Instance == null)
        {
            Debug.LogError("ImaginationSceneInitializer: GameUIManager 初始化失败！");
        }
        else
        {
            Debug.Log("ImaginationSceneInitializer: GameUIManager 已就绪");
        }

        // 确保 GameEventManager 已初始化
        if (GameEventManager.Instance == null)
        {
            Debug.LogError("ImaginationSceneInitializer: GameEventManager 初始化失败！");
        }
        else
        {
            Debug.Log("ImaginationSceneInitializer: GameEventManager 已就绪");
        }

        // 确保 CombatCharacterManager 已初始化
        if (CombatCharacterManager.Instance == null)
        {
            Debug.LogError("ImaginationSceneInitializer: CombatCharacterManager 初始化失败！");
        }
        else
        {
            Debug.Log("ImaginationSceneInitializer: CombatCharacterManager 已就绪");
        }
    }
}
