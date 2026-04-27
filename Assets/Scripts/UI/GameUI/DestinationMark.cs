using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DestinationMark : MonoBehaviour
{
    public Transform target;
    public float hideRange = 6f;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.6f, 0f);

    private RectTransform rectTransform;
    private Camera mainCamera;
    private Camera uiCamera;
    private Transform player;
    private RectTransform markRect;
    private Image markImage;
    private Canvas rootCanvas;

    private float maxPositionX;
    private float maxPositionY;
    private float markSizeX;
    private float markSizeY;

    public bool markActive = true;
    public float screenOffset = 100.0f;
    private bool warnedInvalidTarget = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        mainCamera = Camera.main;
        var uiCamObj = GameObject.FindGameObjectWithTag("UICamera");
        if (uiCamObj != null) uiCamera = uiCamObj.GetComponent<Camera>();
        markRect = transform.GetChild(0).GetComponent<RectTransform>();
        markImage = transform.GetChild(0).GetComponent<Image>();
        rootCanvas = GetComponentInParent<Canvas>();

        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        // 清空可能在 Inspector 中残留的初始目标，避免场景切换后错误指向玩家
        target = null;
        if (markImage != null) markImage.enabled = false;
    }

    private void Start()
    {
        // sizeDelta 在全屏拉伸布局下可能为 0，这会导致标记位置异常固定
        maxPositionX = rectTransform.rect.width / 2f;
        maxPositionY = rectTransform.rect.height / 2f;
        markSizeX = markRect.sizeDelta.x / 2;
        markSizeY = markRect.sizeDelta.y / 2;

    }

    private void LateUpdate()
    {
        // 动态修复引用（场景切换后 mainCamera / player / uiCamera 可能重建）
        if (mainCamera == null) mainCamera = Camera.main;
        if (uiCamera == null)
        {
            var uiCamObj = GameObject.FindGameObjectWithTag("UICamera");
            if (uiCamObj != null) uiCamera = uiCamObj.GetComponent<Camera>();
        }
        if (player == null)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }

        // 若目标误指向玩家（或玩家子物体），直接隐藏并清空
        if (target != null && player != null && (target == player || target.IsChildOf(player)))
        {
            if (!warnedInvalidTarget)
            {
                Debug.LogWarning($"DestinationMark: target 误指向玩家({target.name})，已自动清空。请检查 SetTarget 来源。");
                warnedInvalidTarget = true;
            }
            target = null;
        }

        if (mainCamera == null || rectTransform == null || markRect == null || markImage == null || player == null)
        {
            if (markImage != null && markImage.enabled) markImage.enabled = false;
            return;
        }

        // 分辨率/布局变化时，实时更新边界尺寸
        float currentHalfW = rectTransform.rect.width / 2f;
        float currentHalfH = rectTransform.rect.height / 2f;
        if (currentHalfW > 0.01f) maxPositionX = currentHalfW;
        if (currentHalfH > 0.01f) maxPositionY = currentHalfH;

        // 若 RectTransform 宽高仍异常，兜底使用屏幕尺寸
        if (maxPositionX <= 0.01f) maxPositionX = Screen.width / 2f;
        if (maxPositionY <= 0.01f) maxPositionY = Screen.height / 2f;

        if (!markActive || target == null || Vector3.Distance(target.position, player.position) < hideRange)
        {
            if (markImage.enabled) markImage.enabled = false;
        }
        else
        {
            if (!markImage.enabled) markImage.enabled = true;

            Vector2 localPoint;
            Vector3 worldPos = target.position + worldOffset;
            Vector3 screenPoint = mainCamera.WorldToScreenPoint(worldPos);

            Camera canvasCamera = uiCamera;
            if (rootCanvas != null && rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                canvasCamera = null;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,
                screenPoint, canvasCamera, out localPoint);

            if (screenPoint.z > 0 && localPoint.x > -maxPositionX && localPoint.x < maxPositionX)
            {
                localPoint.x = Mathf.Clamp(localPoint.x, -maxPositionX + markSizeX + screenOffset, maxPositionX - markSizeX - screenOffset);
                localPoint.y = Mathf.Clamp(localPoint.y, -maxPositionY + markSizeY + screenOffset, maxPositionY - markSizeY - screenOffset);

            }
            else
            {
                if (Vector3.Cross(mainCamera.transform.forward, target.position - mainCamera.transform.position).y > 0)
                {
                    localPoint.x = maxPositionX - markSizeX - screenOffset;
                }
                else
                {
                    localPoint.x = -maxPositionX + markSizeX + screenOffset;
                }

                // localPoint.y = Mathf.Clamp(localPoint.y, -maxPositionY + markSizeY, maxPositionY - markSizeY);
                localPoint.y = 0;
            }

            markRect.anchoredPosition = localPoint;
        }
    }

    public void SetTarget(Transform m_target)
    {
        warnedInvalidTarget = false;
        target = m_target;
        hideRange = 4.0f;
        markRect.GetComponent<Animator>().SetTrigger("Mark");
        Debug.Log($"DestinationMark.SetTarget -> {(target != null ? target.name : "null")}");
    }

    public void SetTarget(Transform m_target, float m_hideRange)
    {
        warnedInvalidTarget = false;
        target = m_target;
        hideRange = m_hideRange;
        markRect.GetComponent<Animator>().SetTrigger("Mark");
        Debug.Log($"DestinationMark.SetTarget(range) -> {(target != null ? target.name : "null")}, hideRange={hideRange}");
    }

    public void HideMark()
    {
        target = null;
    }
}
