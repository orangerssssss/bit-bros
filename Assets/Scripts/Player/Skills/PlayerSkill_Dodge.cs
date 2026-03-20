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

    private float dodgeTime;// 用于翻滚CD计时
    private float dodgeCD = 1.2f;// 翻滚CD

    private void Awake()
    {
        combatController = GetComponent<PlayerCombatController>();
        animator = GetComponent<Animator>();
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
                combatController.ResetSkillTrigger();

                animator.SetTrigger("Dodge");
                dodgeTime = Time.time;
            }
        }
    }
}
