using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家翻滚
/// </summary>
public class PlayerSkill_Dodge : MonoBehaviour
{
    private PlayerCombatController combatController;// 玩家战斗控制组件
    private Animator animator;// 玩家动画组件
    private PlayerAttributes playerAttributes;
    [SerializeField] private float dodgeInvulnerableSeconds = 0.45f;
    [SerializeField] private float dodgeAnimSpeedMultiplier = 1.25f;
    [SerializeField] private float dodgeAnimSpeedDuration = 0.45f;

    private float dodgeTime;// 用于翻滚CD计时
    private float dodgeCD = 1.2f;// 翻滚CD
    private Coroutine dodgeSpeedCoroutine;

    private void Awake()
    {
        combatController = GetComponent<PlayerCombatController>();
        animator = GetComponent<Animator>();
        playerAttributes = GetComponent<PlayerAttributes>();
    }

    private void Update()
    {
        if (combatController.playerSkillControllable) Dodge();
    }

    private void Dodge()
    {
        // 玩家站在地面上
        if (combatController.playerMoveController.characterController.isGrounded)
        {
            if (Input.GetKeyDown(KeyCode.Space) && Time.time - dodgeTime > dodgeCD)
            {
                if (playerAttributes != null && !playerAttributes.TryConsumeStamina(playerAttributes.dodgeStaminaCost))
                {
                    return;
                }

                combatController.ResetSkillTrigger();

                if (playerAttributes != null)
                {
                    playerAttributes.SetInvulnerable(dodgeInvulnerableSeconds);
                }

                animator.SetTrigger("Dodge");
                if (dodgeSpeedCoroutine != null) StopCoroutine(dodgeSpeedCoroutine);
                dodgeSpeedCoroutine = StartCoroutine(BoostDodgeAnimSpeed());
                dodgeTime = Time.time;
            }
        }
    }

    private IEnumerator BoostDodgeAnimSpeed()
    {
        if (animator == null) yield break;

        float originalSpeed = animator.speed;
        animator.speed = originalSpeed * dodgeAnimSpeedMultiplier;
        yield return new WaitForSeconds(dodgeAnimSpeedDuration);

        if (animator != null && animator.speed > 0)
        {
            animator.speed = originalSpeed;
        }

        dodgeSpeedCoroutine = null;
    }
}
