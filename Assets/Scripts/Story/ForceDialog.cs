using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 强制播放对话
/// </summary>
public class ForceDialog : MonoBehaviour
{
    public Transform forcePosition;
    public DialogObject forceDialogObject;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerInputManager.Instance.moveController.SetPositionAndRotation(forcePosition);
            if (forceDialogObject.gameObject.activeSelf) forceDialogObject.Interact();
        }
    }
}
