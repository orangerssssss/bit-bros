using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 交互物体抽象类, 玩家能够与该类物体进行交互(物体需处于交互层)
/// </summary>
public abstract class InteractiveObject : MonoBehaviour
{
    public string interactName = "交互";// 交互提示
    
    public bool interactable = true;// 是否能够进行交互
    
    /// <summary>
    /// 交互
    /// </summary>
    public abstract void Interact();
}
