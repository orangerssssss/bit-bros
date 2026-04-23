using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GameEventManager : MonoBehaviour
{
    private static GameEventManager instance;

    public static GameEventManager Instance
    {
        get
        {
            if (instance == null) instance = GameObject.FindObjectOfType<GameEventManager>();
            return instance;
        }
    }
    /// <summary>
    /// 对话开始前执行的Event
    /// </summary>
    public UnityEvent<DialogObject> beforeDialogEvent = new UnityEvent<DialogObject>();
    /// <summary>
    /// 对话结束后执行的Event
    /// </summary>
    public UnityEvent<DialogConfig> dialogConfigEndEvent = new UnityEvent<DialogConfig>();
    /// <summary>
    /// 拾取物品时执行的Event
    /// </summary>
    public UnityEvent<int> pickUpItemEvent = new UnityEvent<int>();
    /// <summary>
    /// 玩家死亡前执行的Event
    /// </summary>
    public UnityEvent<CharacterAttributes> characterBeforeDeathEvent = new UnityEvent<CharacterAttributes>();
    /// <summary>
    /// 玩家死亡前执行的Event
    /// </summary>
    public UnityEvent<FightAttributes> fightBeforeDeathEvent = new UnityEvent<FightAttributes>();
    /// <summary>
    /// 关闭背包时执行的Event
    /// </summary>
    public UnityEvent closePackageEvent = new UnityEvent();
    /// <summary>
    /// 故事展示完后执行的Event
    /// </summary>
    public UnityEvent storySettingEndEvent = new UnityEvent();
    /// <summary>
    /// 出售物品时执行的Event
    /// </summary>
    public UnityEvent sellEvent = new UnityEvent();
    //public UnityEvent<DialogObject> dialogEndEvent = new UnityEvent<DialogObject>();
    //public UnityEvent<int> playerEquipEvent = new UnityEvent<int>();
    public UnityEvent<Collider> playerEnterTriggerEvent;   // 参数为触发器碰撞体
}
