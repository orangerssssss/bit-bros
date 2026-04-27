using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 交互检测, 在玩家指向可交互物体时显示交互提示, 按下交互键时与物体进行交互
/// </summary>
public class InteractDetect : MonoBehaviour
{
    private ViewController viewController;// 玩家视角控制组件
    private float interactDistance = 3.5f;// 交互距离
    private InteractiveObject interactiveObject = null;// 当前交互物体

    private bool interactable = true;// 是否可交互

    private GameObject interactLabel;// 交互提示父物体
    private Text interactTextLabel;// 交互显示文本

    [SerializeField] private AudioSource interactAudioSource;
    [SerializeField] private AudioClip pickUpSFX;

    private void Awake()
    {
        viewController = GetComponent<ViewController>();
        if (GameEventManager.Instance != null)
            GameEventManager.Instance.pickUpItemEvent.AddListener((id) => PlayPickUpItemSFX());
    }

    private void Start()
    {
        if (GameUIManager.Instance != null)
        {
            interactLabel = GameUIManager.Instance.interact;
            interactTextLabel = GameUIManager.Instance.interactTextLabel;
        }
    }

    private void Update()
    {
        // Reacquire UI references if scene UI was recreated (persistent player keeps this component)
        if ((interactLabel == null || interactTextLabel == null) && GameUIManager.Instance != null)
        {
            interactLabel = GameUIManager.Instance.interact;
            interactTextLabel = GameUIManager.Instance.interactTextLabel;
        }

        if (interactable)
        {
            // 检测可交互物体
            DetectInteractiveObject();
            // 按E交互
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (interactiveObject)
                {
                    interactiveObject.Interact();
                }
            }
        }
    }

    /// <summary>
    /// 准星检测可交互物体
    /// </summary>
    public void DetectInteractiveObject()
    {
        if (!viewController) return;
        if (viewController.playerViewPoint == null || viewController.playerVCamera == null) return;

        // 从屏幕中央向屏幕内发出一段射线, 检测是否有可交互物体
        InteractiveObject obj = null;
        if (Physics.Raycast(viewController.playerViewPoint.position, viewController.playerVCamera.forward, out RaycastHit hitInfo, interactDistance))
        {
            // 兼容：很多NPC的Collider在子物体上，而DialogObject在父物体上
            Transform hitTrans = hitInfo.transform;
            obj = hitTrans.GetComponent<InteractiveObject>();
            if (obj == null) obj = hitTrans.GetComponentInParent<InteractiveObject>();
            if (obj == null) obj = hitTrans.GetComponentInChildren<InteractiveObject>();
        }

        // 交互显示更新
        if (obj == null || !obj.enabled)
        {
            obj = null;
            if (interactLabel != null && interactLabel.activeSelf) interactLabel.SetActive(false);
        }
        else
        {
            if (interactTextLabel != null) interactTextLabel.text = obj.interactName;
            if (interactLabel != null && !interactLabel.activeSelf) interactLabel.SetActive(true);
        }

        if (interactiveObject != obj) interactiveObject = obj;
    }

    /// <summary>
    /// 设置玩家是否可进行交互
    /// </summary>
    public void SetInteractable(bool enable)
    {
        interactable = enable;
        if (!enable)
        {
            if (interactLabel != null) interactLabel.SetActive(false);
        }
    }

    /// <summary>
    /// 播放玩家拾取物品特效
    /// </summary>
    private void PlayPickUpItemSFX()
    {
        interactAudioSource.Stop();
        interactAudioSource.clip = pickUpSFX;
        interactAudioSource.Play();
    }
}

