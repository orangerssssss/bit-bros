using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DestinationMark : MonoBehaviour
{
    public Transform target;
    public float hideRange = 6f;

    private RectTransform rectTransform;
    private Camera mainCamera;
    private Camera uiCamera;
    private Transform player;
    private RectTransform markRect;
    private Image markImage;

    private float maxPositionX;
    private float maxPositionY;
    private float markSizeX;
    private float markSizeY;

    public bool markActive = true;
    public float screenOffset = 100.0f;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        mainCamera = Camera.main;
        uiCamera = GameObject.FindGameObjectWithTag("UICamera").GetComponent<Camera>();
        markRect = transform.GetChild(0).GetComponent<RectTransform>();
        markImage = transform.GetChild(0).GetComponent<Image>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    private void Start()
    {
        maxPositionX = rectTransform.sizeDelta.x / 2;
        maxPositionY = rectTransform.sizeDelta.y / 2;
        markSizeX = markRect.sizeDelta.x / 2;
        markSizeY = markRect.sizeDelta.y / 2;

    }

    private void LateUpdate()
    {
        if (!markActive || target == null || Vector3.Distance(target.position, player.position) < hideRange)
        {
            if (markImage.enabled) markImage.enabled = false;
        }
        else
        {
            if (!markImage.enabled) markImage.enabled = true;

            Vector2 localPoint;
            Vector3 screenPoint = mainCamera.WorldToScreenPoint(target.position);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,
                screenPoint, uiCamera, out localPoint);

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
        target = m_target;
        hideRange = 4.0f;
        markRect.GetComponent<Animator>().SetTrigger("Mark");
    }

    public void SetTarget(Transform m_target, float m_hideRange)
    {
        target = m_target;
        hideRange = m_hideRange;
        markRect.GetComponent<Animator>().SetTrigger("Mark");
    }

    public void HideMark()
    {
        target = null;
    }
}
