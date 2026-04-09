using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("移动参数")]
    public float moveSpeed = 5f;    // 移动速度
    public float rotateSpeed = 10f; // 转向速度

    private CharacterController cc;
    private Animator anim; // 如果你角色有动画，这里会自动获取


    // Start is called before the first frame update
    void Start()
    {
        cc = GetComponent<CharacterController>();
        anim = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        // 1. 获取键盘输入（WASD / 方向键）
        float h = Input.GetAxis("Horizontal"); // A/D ←/→
        float v = Input.GetAxis("Vertical");   // W/S ↑/↓

        // 2. 计算移动方向（基于世界坐标，也可以改成相机朝向）
        Vector3 moveDir = new Vector3(h, 0, v).normalized;

        // 3. 让角色面向移动方向
        if (moveDir.magnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
        }

        // 4. 执行移动
        cc.Move(moveDir * moveSpeed * Time.deltaTime);

        // 5. 同步动画（如果你的角色有动画系统）
        if (anim != null)
        {
            anim.SetFloat("MoveSpeedXZ", moveDir.magnitude);
        }
    }
}
