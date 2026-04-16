using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家视角控制, 包括防止摄像机被物体遮挡
/// </summary>
public class ViewController : MonoBehaviour
{
    private Camera mainCamera;// 主相机
    public Transform playerViewPoint;// 摄像机指向的位置
    [SerializeField]
    private Transform followingCameraParent;// 相机的父物体
    [HideInInspector]
    public Transform playerVCamera;// 玩家虚拟相机

    private Vector3 lookRotationEuler;// 朝向的欧拉角

    private bool loggedViewControlDisabled = false;

    [SerializeField]
    private float sensitivityMouseX = 1.5f; // 鼠标旋转X轴灵敏度 (调低)
    [SerializeField]
    private float sensitivityMouseY = 0.9f; // 鼠标旋转Y轴灵敏度 (调低)

    [SerializeField]
    private float viewDistance = 3.0f; // 摄像机与玩家的距离 (默认更近)
    private float viewDistanceMin = 1.5f; // 摄像机离玩家的最小距离
    private float viewDistanceMax = 4.0f; // 摄像机离玩家的最大距离
    private float viewLerpSpeed = 0.06f; // 摄相机平滑过渡速度 (稍快一点)

    private Vector3[] viewBlockPoint;// 摄像机中间点及近面四个角的偏移, 用于射线检测

    public bool viewControllable = true;// 是否可以控制视角

    private void Awake()
    {
        Init();
        Debug.Log($"ViewController Awake: followingCameraParent={(followingCameraParent != null ? followingCameraParent.name : "null")}, playerVCamera={(playerVCamera != null ? playerVCamera.name : "null")}");
    }

    private void LateUpdate()
    {
        followingCameraParent.position = playerViewPoint.position;
        ViewControl();
    }

    // 初始化数据
    private void Init()
    {
        mainCamera = Camera.main;
        playerVCamera = followingCameraParent.GetChild(0);

        // 初始化相机近面的点偏移, 用于射线检测
        float halfFOV = (mainCamera.fieldOfView * 0.5f) * Mathf.Deg2Rad;
        float halfHeight = mainCamera.nearClipPlane * Mathf.Tan(halfFOV);
        float halfWidth = halfHeight * mainCamera.aspect;
        viewBlockPoint = new Vector3[5];
        viewBlockPoint[0] = new Vector3(0, 0, 0);
        viewBlockPoint[1] = new Vector3(-halfWidth, -halfHeight, 0);
        viewBlockPoint[2] = new Vector3(-halfWidth, halfHeight, 0);
        viewBlockPoint[3] = new Vector3(halfWidth, -halfHeight, 0);
        viewBlockPoint[4] = new Vector3(halfWidth, halfHeight, 0);

        InitCamera();
    }

    /// <summary>
    /// 初始化相机位置及方向
    /// </summary>
    public void InitCamera()
    {
        followingCameraParent.SetPositionAndRotation(playerViewPoint.position, playerViewPoint.rotation);
        playerVCamera.SetPositionAndRotation(playerViewPoint.position - playerVCamera.forward * viewDistance, playerViewPoint.rotation);
        lookRotationEuler = playerViewPoint.eulerAngles;
    }

    /// <summary>
    /// Set mouse sensitivity at runtime
    /// </summary>
    public void SetSensitivity(float x, float y)
    {
        sensitivityMouseX = x;
        sensitivityMouseY = y;
    }

    /// <summary>
    /// Set desired camera distance at runtime
    /// </summary>
    public void SetViewDistance(float d)
    {
        viewDistance = Mathf.Clamp(d, viewDistanceMin, viewDistanceMax);
    }

    /// <summary>
    /// 角色视角控制, 包括鼠标控制及被遮挡后的自动缩进
    /// </summary>
    private void ViewControl()
    {
        if (viewControllable)
        {
            // 角度旋转 - 带时间缩放和平滑以防抖动
            float mx = Input.GetAxis("Mouse X") * sensitivityMouseX * 100f * Time.deltaTime;
            float my = Input.GetAxis("Mouse Y") * sensitivityMouseY * 100f * Time.deltaTime;

            lookRotationEuler.x = Mathf.Clamp(lookRotationEuler.x + my * -1f, -60f, 80f);
            lookRotationEuler.y += mx;

            // 平滑旋转到目标角度
            Quaternion targetRot = Quaternion.Euler(lookRotationEuler);
            followingCameraParent.rotation = Quaternion.Slerp(followingCameraParent.rotation, targetRot, 10f * Time.deltaTime);
        }
        else
        {
            if (!loggedViewControlDisabled)
            {
                Debug.Log("ViewController.viewControllable is false");
                loggedViewControlDisabled = true;
            }
        }

        // 控制视角远近
        if (viewControllable)
            viewDistance = Mathf.Clamp(viewDistance - Input.GetAxisRaw("Mouse ScrollWheel") * 4, viewDistanceMin, viewDistanceMax);
        else
        {
            // also log raw axis values when disabled to check if input is coming
            if (Input.GetAxisRaw("Mouse X") != 0 || Input.GetAxisRaw("Mouse Y") != 0)
            {
                // Debug.Log($"Mouse axes while view disabled: MouseX={Input.GetAxisRaw("Mouse X")}, MouseY={Input.GetAxisRaw("Mouse Y")}");
            }
        }
        playerVCamera.position = Vector3.Lerp(playerVCamera.position, followingCameraParent.position - playerVCamera.forward * viewDistance, viewLerpSpeed);

        // 射线检测, 判断视线是否被遮挡
        float minDis = viewDistanceMax + 1;
        for (int i = 0; i < 5; i++)
        {
            // 射线检测到到目标, 且为环境
            if (Physics.Linecast(followingCameraParent.position + playerVCamera.TransformDirection(viewBlockPoint[i]), playerVCamera.position + playerVCamera.TransformDirection(viewBlockPoint[i]),
                    out RaycastHit hit, ~(1 << 2 | 1 << 7)))
            {
                float dis = Vector3.Distance(followingCameraParent.position, hit.point);
                if (dis < minDis) minDis = dis;
            }
        }
        // 判断视线是否被遮挡并更新相机位置
        if (minDis < viewDistanceMax)
        {
            playerVCamera.position = followingCameraParent.position - playerVCamera.forward * minDis;
        }
    }
}
