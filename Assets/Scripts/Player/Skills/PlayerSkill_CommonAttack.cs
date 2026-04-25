using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家普通攻击
/// </summary>
public class PlayerSkill_CommonAttack : MonoBehaviour
{
    private PlayerAttributes playerAttributes;// 玩家属性组件
    private PlayerMoveController moveController;// 玩家移动组件
    private PlayerCombatController combatController;// 玩家战斗控制组件
    private Animator animator;// 玩家动画组件
    
    [SerializeField]
    private List<ParticleSystem> vfx_CommonAttack = new List<ParticleSystem>();// 普通攻击特效
    [SerializeField]
    private List<AudioClip> sfx_CommonAttack = new List<AudioClip>();
    [SerializeField]
    private BoxAttackArea attackBox;// 普通攻击区域

    private void Awake()
    {
        playerAttributes = GetComponent<PlayerAttributes>();
        moveController = GetComponent<PlayerMoveController>();
        combatController = GetComponent<PlayerCombatController>();
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        AddCommonAttackEvent();

    }

    private void Update()
    {
        // 在能够使用技能且装备有武器时才能进行普通攻击
        // Disable common attack while shielding (right mouse)
        if (combatController.playerSkillControllable && combatController.equipedWeaponID > 0 && combatController.WeaponVisible && !combatController.isShielding)
        {
            CommonAttack();
        }
    }

    /// <summary>
    /// 进行一次普通攻击
    /// </summary>
    private void CommonAttack()
    {
        // 按下鼠标左键
        if (Input.GetMouseButtonDown(0))
        {
            // 玩家站在地面上(或接近站在地面上)
            if (moveController.characterController.isGrounded || 
                Physics.Linecast(transform.position + 0.05f * Vector3.up, transform.position - 0.2f * Vector3.up))
            {
                if (animator.GetCurrentAnimatorStateInfo(0).IsName("Move"))// 处于移动状态可以触发
                {
                    animator.SetTrigger("CommonAttack");
                }
                else if (animator.GetCurrentAnimatorStateInfo(0).IsTag("CommonAttack")
                    && animator.GetCurrentAnimatorStateInfo(0).normalizedTime > Mathf.Clamp(0.35f / animator.GetFloat("CommonAttackSpeed"), 0, 0.8f)
                    && !animator.GetNextAnimatorStateInfo(0).IsTag("CommonAttack"))// 普通攻击接近结束状态可以触发
                {
                    animator.SetTrigger("CommonAttack");
                }
            }
        }
    }

    /// <summary>
    /// 添加播放攻击特效事件及攻击伤害判定事件至动画
    /// </summary>
    private void AddCommonAttackEvent()
    {
        // 攻击特效
        combatController.AddEventToClips("PlayCommonAttackFX", 0.75f, new List<string> { "CommonAttack_0" }, intParameter: 0);
        combatController.AddEventToClips("PlayCommonAttackFX", 0.55f, new List<string> { "CommonAttack_1" }, intParameter: 1);
        combatController.AddEventToClips("PlayCommonAttackFX", 0.24f, new List<string> { "CommonAttack_2" }, intParameter: 2);
        
        // 伤害判定
        combatController.AddEventToClips("CommonAttackDamage", 0.8f, new List<string> { "CommonAttack_0" }, intParameter: 0);
        combatController.AddEventToClips("CommonAttackDamage", 0.6f, new List<string> { "CommonAttack_1" }, intParameter: 1);
        combatController.AddEventToClips("CommonAttackDamage", 0.29f, new List<string> { "CommonAttack_2" }, intParameter: 2);
    }

    /// <summary>
    /// 播放对应的攻击特效
    /// </summary>
    /// <param name="index">攻击索引, 第一段普通攻击为0, 第二段为1...</param>
    private void PlayCommonAttackFX(int index)
    {
        vfx_CommonAttack[index].Stop();
        vfx_CommonAttack[index].Play();

        combatController.combatAudioSource.Stop();
        combatController.combatAudioSource.clip = sfx_CommonAttack[index];
        combatController.combatAudioSource.Play();
    }

    /// <summary>
    /// 造成攻击伤害, 如果击中则产生顿帧效果
    /// </summary>
    /// <param name="index">攻击索引, 第一段普通攻击为0, 第二段为1...</param>
    private void CommonAttackDamage(int index)
    {
        if (attackBox.AreaDamage(playerAttributes.PhysicalAttack, true))
        {
            combatController.PlayerHitPause(0.06f);
            StartCoroutine(AttackParticlePause(index, 0.06f));
        }
    }

    /// <summary>
    /// 顿帧效果下同时暂停攻击特效的播放
    /// </summary>
    /// <param name="index">攻击索引, 第一段普通攻击为0, 第二段为1...</param>
    /// <param name="seconds">暂停时间(sec)</param>
    private IEnumerator AttackParticlePause(int index, float seconds)
    {
        vfx_CommonAttack[index].Pause();
        yield return new WaitForSeconds(seconds);
        vfx_CommonAttack[index].Play();
    }

}