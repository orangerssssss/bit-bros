using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家基础的移动 旋转控制(包括行走、奔跑)
/// </summary>
public class PlayerMoveController : MonoBehaviour
{
    const float gravity = 45.0f;// 重力常量
    
    [HideInInspector]
    public CharacterController characterController;// 玩家控制器组件(unity)
    private Animator animator;// 玩家动画组件
    private PlayerAttributes playerAttributes;// 玩家属性组件
    private ViewController viewController;// 玩家视角控制组件

    private Vector3 moveDirection = Vector3.zero;// 玩家移动方向
    [HideInInspector]
    public Vector3 velocity = Vector3.zero;// 记录玩家移动速度

    private Quaternion targetRotation;
    private float playerRotateLerpSpeed = 0.1f;// 玩家旋转平滑过渡速度

    public float baseWalkSpeed = 3.0f;// 玩家走路速度
    public float baseRunSpeed = 8.0f;// 玩家奔跑速度
    //[SerializeField]
    //private float jumpSpeed = 10.0f;// 玩家跳跃速度
    
    private float moveAnimLerpSpeed = 0.1f;// 移动动画过渡速度
    
    public bool playerMoveControllable = true;// 是否可以控制角色移动

    private float moveSpeedXZ;// 玩家水平移动速度相对值, 用于控制动画播放
    private float moveSpeedY = 0;// 玩家垂直移动速度相对值, 用于控制动画播放

    private Coroutine rotateCoroutine;// 在玩家停止移动后开始转向的协程

    [SerializeField] private AudioSource moveAudioSource;
    [SerializeField] private AudioClip walkFootStepClip;
    [SerializeField] private AudioClip runFootStepClip;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        playerAttributes = GetComponent<PlayerAttributes>();
        viewController = GetComponent<ViewController>();

        targetRotation = transform.rotation;
    }

    private void Update()
    {
        PlayerControl();

        // 移动角色
        if (characterController.enabled) characterController.Move(moveDirection * Time.deltaTime);
        velocity = characterController.velocity;

        // 角色旋转
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, playerRotateLerpSpeed * Time.deltaTime * 240);

        // 更新动画机中MoveSpeed, 保证动画播放与玩家移动同步
        animator.SetFloat("MoveSpeedXZ", Mathf.Lerp(animator.GetFloat("MoveSpeedXZ"), moveSpeedXZ, moveAnimLerpSpeed * playerAttributes.moveSpeedMultiplier));
        animator.SetFloat("MoveSpeedY", Mathf.Lerp(animator.GetFloat("MoveSpeedY"), moveSpeedY, moveAnimLerpSpeed * playerAttributes.moveSpeedMultiplier));
    }

    /// <summary>
    /// 初始化移动控制器
    /// </summary>
    /// <param name="rotation">设置玩家的朝向</param>
    public void InitMove(Quaternion rotation)
    {
        StopRotateTo();
        targetRotation = rotation;
    }

    /// <summary>
    /// 角色移动跳跃控制
    /// </summary>
    private void PlayerControl()
    {
        // Debug.DrawLine(transform.position + 0.05f * Vector3.up, transform.position - 0.01f * Vector3.up);
        if (characterController.isGrounded)
        // if (Physics.Linecast(transform.position + 0.05f * Vector3.up, transform.position - 0.01f * Vector3.up, out RaycastHit hit))
        {
            // 获取移动按键输入
            Vector3 inputDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));

            // 当玩家不处于移动状态且输入不为空时设置MoveInput为true, 用于打断其他动画到移动动画的缓慢过渡直接快速进入移动状态
            animator.SetBool("MoveInput", inputDir != Vector3.zero && !animator.GetCurrentAnimatorStateInfo(0).IsName("Move"));

            // 移动输入应用于移动方向
            if (playerMoveControllable && animator.GetCurrentAnimatorStateInfo(0).IsTag("Movable") && !animator.IsInTransition(0))
                moveDirection = inputDir;
            else
                moveDirection = Vector3.zero;

            if (moveDirection != Vector3.zero)
            {
                // 移动方向
                moveDirection = viewController.playerVCamera.TransformDirection(moveDirection);
                moveDirection.y = 0;
                moveDirection.Normalize();

                // 玩家朝向移动方向
                targetRotation.SetLookRotation(moveDirection, Vector3.up);

                // 奔跑
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    moveDirection *= baseRunSpeed * playerAttributes.moveSpeedMultiplier;
                    moveSpeedXZ = 1.0f;
                }
                else
                {
                    moveDirection *= baseWalkSpeed * playerAttributes.moveSpeedMultiplier;
                    moveSpeedXZ = 0.5f;
                }

                // 非移动状态移动速度减半
                if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Move"))
                {
                    moveDirection *= 0.5f;
                }
            }
            else
            {
                moveSpeedXZ = 0;
            }

            //// 跳跃
            //if (playerMoveControllable &&  Input.GetButton("Jump") && animator.GetCurrentAnimatorStateInfo(0).IsName("Move"))
            //{
            //    moveDirection.y = jumpSpeed;
            //}
            //else
            //{
            //    // 调整y轴移动值使移动方向不为0, 保证玩家可被推动而不是穿过
            //    moveDirection.y = -1f;
            //}
            moveSpeedY = 0;

            // 调整y轴移动值使移动方向不为0, 保证玩家可被推动而不是穿过
            moveDirection.y = -1f;
        }
        else
        {
            // Y轴速度受重力影响
            if (moveDirection.y > -10)
                moveDirection.y -= gravity * Time.deltaTime;
            
            if (!Physics.Linecast(transform.position + 0.5f * Vector3.up, transform.position - 2.0f * Vector3.up, out RaycastHit hit))
                moveSpeedY = -1.0f;
        }
    }

    /// <summary>
    /// 直接设置玩家位置及朝向
    /// </summary>
    /// <param name="trans"></param>
    public void SetPositionAndRotation(Transform trans)
    {
        GetComponent<CharacterController>().enabled = false;
        transform.SetPositionAndRotation(trans.position, trans.rotation);
        targetRotation = trans.rotation;
        viewController.InitCamera();
        StartCoroutine(ControllerEnable(trans));
    }
    
    /// <summary>
    /// 短暂停顿后打开角色控制器(角色控制器在开启状态无法直接设置位置)
    /// </summary>
    private IEnumerator ControllerEnable(Transform trans)
    {
        yield return null;

        transform.SetPositionAndRotation(trans.position, trans.rotation);
        targetRotation = trans.rotation;
        viewController.InitCamera();
        animator.Play("Move");

        yield return new WaitForSeconds(0.1f);
        GetComponent<CharacterController>().enabled = true;
    }

    /// <summary>
    /// 通过按键输入方向设置角色朝向, 无过渡
    /// </summary>
    /// <param name="direction">按键输入方向</param>
    public void SetRotation(Vector3 direction)
    {
        Vector3 moveDir = viewController.playerVCamera.TransformDirection(direction);
        moveDir.y = 0;
        Quaternion rot = Quaternion.LookRotation(moveDir, Vector3.up);
        targetRotation = rot;
        transform.rotation = targetRotation;
    }

    /// <summary>
    /// 尝试在角色没有移动时将角色转向至目标方向
    /// </summary>
    /// <param name="point">朝向的目标点</param>
    public void RotateTo(Vector3 point)
    {
        if (rotateCoroutine != null) StopCoroutine(rotateCoroutine);
        rotateCoroutine = StartCoroutine(RotationToIEnum(point));
    }

    /// <summary>
    /// 停止角色转向尝试
    /// </summary>
    public void StopRotateTo()
    {
        if (rotateCoroutine != null) StopCoroutine(rotateCoroutine);
    }

    /// <summary>
    /// 在角色没有移动时将角色转向至目标方向
    /// </summary>
    /// <param name="point">朝向的目标点</param>
    private IEnumerator RotationToIEnum(Vector3 point)
    {
        // 如果角色在移动则等待
        while (velocity != Vector3.zero) yield return null;

        // 设置角色转向
        Vector3 dir = point - transform.position;
        dir.y = 0;
        targetRotation.SetLookRotation(dir, Vector3.up);
        yield return null;
    }

    private void PlayFootStepSFXWalk()
    {
        if (moveSpeedXZ < 0.75f && moveSpeedXZ > 0.05f)
        {
            moveAudioSource.Stop();
            moveAudioSource.clip = walkFootStepClip;
            moveAudioSource.Play();
        }
    }

    private void PlayFootStepSFXRun()
    {
        if (moveSpeedXZ > 0.75f)
        {
            moveAudioSource.Stop();
            moveAudioSource.clip = runFootStepClip;
            moveAudioSource.Play();
        }
    }
}
