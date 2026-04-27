using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 强制播放对话 - 自动查找玩家对象
/// </summary>
public class ForceDialog : MonoBehaviour
{
    public Transform forcePosition;
    public DialogObject forceDialogObject;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 自动查找标签为 Player 的对象
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject == null)
            {
                Debug.LogError("ForceDialog: 找不到标签为 'Player' 的对象！");
                return;
            }

            // 设置玩家位置和旋转
            PlayerInputManager piManager = PlayerInputManager.Instance;
            if (piManager != null && piManager.moveController != null)
            {
                piManager.moveController.SetPositionAndRotation(forcePosition);
            }
            else
            {
                Debug.LogError("ForceDialog: PlayerInputManager 或 moveController 为 null！");
            }

            // 触发对话
            if (forceDialogObject != null && forceDialogObject.gameObject.activeSelf)
            {
                forceDialogObject.Interact();
            }
            else
            {
                Debug.LogWarning("ForceDialog: forceDialogObject 为 null 或未激活！");
            }
        }
    }
}
