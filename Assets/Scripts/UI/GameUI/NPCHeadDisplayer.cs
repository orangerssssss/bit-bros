using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// NPC头部显示, 用于显示NPC的名称
/// </summary>
public class NPCHeadDisplayer : MonoBehaviour
{
    private static Transform mainCamera;// 主相机
    [SerializeField]
    private float showDistance = 8.0f;// 显示距离, 当相机与NPC距离小于此时才会显示名称
    [SerializeField]
    private string headName;// 显示的名称
    [SerializeField]
    private GameObject nameObject;// 显示的父物体, 用于开关显示
    [SerializeField]
    private Text nameText;// 显示文本

    private void Awake()
    {
        if (mainCamera == null) mainCamera = Camera.main.transform;

        nameText.text = headName;
    }
    
    void Update()
    {
        if (Vector3.Distance(transform.position, mainCamera.position) < showDistance)
        {
            if (!nameObject.activeSelf) nameObject.SetActive(true);
        }
        else
        {
            if (nameObject.activeSelf) nameObject.SetActive(false);
        }

        // 始终朝向相机
        if (nameObject.activeSelf) nameObject.transform.LookAt(mainCamera);
    }
}
