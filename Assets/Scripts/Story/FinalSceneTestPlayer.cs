using UnityEngine;

/// <summary>
/// FinalScene 临时测试玩家。
/// 挂在一个简单物体（例如 Sphere）上即可进行 Boss 一阶段测试。
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerAttributes))]
public class FinalSceneTestPlayer : MonoBehaviour
{
    [Header("移动")]
    [SerializeField] private float walkSpeed = 5.0f;
    [SerializeField] private float runSpeed = 8.0f;
    [SerializeField] private float rotateSpeed = 12.0f;
    [SerializeField] private float gravity = 25.0f;

    [Header("相机")]
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private float cameraHeight = 10.0f;
    [SerializeField] private float cameraDistance = 8.0f;
    [SerializeField] private float cameraLerpSpeed = 6.0f;

    private CharacterController characterController;
    private PlayerAttributes playerAttributes;
    private Vector3 velocity;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        playerAttributes = GetComponent<PlayerAttributes>();

        gameObject.tag = "Player";

        if (playerAttributes != null)
        {
            playerAttributes.combatCamp = CombatCamp.Player;
        }

        if (cameraTarget == null)
        {
            cameraTarget = transform;
        }
    }

    private void Update()
    {
        HandleMovement();
        UpdateCamera();
    }

    private void HandleMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 input = new Vector3(horizontal, 0f, vertical).normalized;
        float moveSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;

        if (input.sqrMagnitude > 0.001f)
        {
            Vector3 moveDirection = GetCameraRelativeDirection(input);
            moveDirection.y = 0f;
            moveDirection.Normalize();

            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);

            characterController.Move(moveDirection * moveSpeed * Time.deltaTime);
        }

        if (characterController.isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }

        velocity.y -= gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }

    private Vector3 GetCameraRelativeDirection(Vector3 input)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return input;
        }

        Vector3 forward = mainCamera.transform.forward;
        Vector3 right = mainCamera.transform.right;
        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        return forward * input.z + right * input.x;
    }

    private void UpdateCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null || cameraTarget == null) return;

        Vector3 desiredPosition = cameraTarget.position - cameraTarget.forward * cameraDistance + Vector3.up * cameraHeight;
        mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, desiredPosition, cameraLerpSpeed * Time.deltaTime);
        mainCamera.transform.LookAt(cameraTarget.position + Vector3.up * 1.2f);
    }
}
