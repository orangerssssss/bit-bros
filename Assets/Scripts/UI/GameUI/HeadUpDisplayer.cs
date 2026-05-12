using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 玩家抬头显示, 包括玩家的等级、生命、经验属性
/// </summary>
public class HeadUpDisplayer : MonoBehaviour
{
    private PlayerAttributes playerAttributes;// 玩家属性组件
    private int level = -1;// 当前显示等级
    private int health = -1;// 当前显示生命值
    private int maxHealth = -1;// 当前显示最大生命值
    private int stamina = -1;// 当前显示体力值
    private int maxStamina = -1;// 当前显示最大体力值
    private Color staminaHighColor = new Color(0.20f, 0.78f, 0.29f);
    private Color staminaMidColor = new Color(0.92f, 0.74f, 0.16f);
    private Color staminaLowColor = new Color(0.86f, 0.23f, 0.19f);

    private void Awake()
    {
        playerAttributes = GetComponent<PlayerAttributes>();
        if (playerAttributes == null)
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) playerAttributes = p.GetComponent<PlayerAttributes>();
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        GameUIManager.OnUIReady += OnUIReadyHandler;
    }

    private void OnDisable()
    {
        try { SceneManager.sceneLoaded -= OnSceneLoaded; } catch { }
        try { GameUIManager.OnUIReady -= OnUIReadyHandler; } catch { }
    }

    private void OnUIReadyHandler()
    {
        // When UI is ready, ensure we have a valid player reference and refresh bars
        RefreshPlayerReference();
        string playerName = (playerAttributes != null && playerAttributes.gameObject != null) ? playerAttributes.gameObject.name : "null";
        Debug.Log($"HeadUpDisplayer.OnUIReadyHandler: playerAttributes={playerName}");
        HealthUpdate();
        StaminaUpdate();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // After a scene load, the player object or its attributes may be re-initialized.
        // Wait a frame or two to allow other Start/OnSceneLoaded handlers to run, then refresh UI.
        RefreshPlayerReference();
        StartCoroutine(DelayedRefreshUI());
    }

    private IEnumerator DelayedRefreshUI()
    {
        yield return null; // wait one frame
        yield return null; // wait second frame for safety
        RefreshPlayerReference();
        string playerName = (playerAttributes != null && playerAttributes.gameObject != null) ? playerAttributes.gameObject.name : "null";
        string healthStr = playerAttributes != null ? $"{playerAttributes.health}/{playerAttributes.MaxHealth}" : "n/a";
        var pa = playerAttributes as PlayerAttributes;
        string staminaStr = pa != null ? pa.CurrentStaminaInt.ToString() : "n/a";
        Debug.Log($"HeadUpDisplayer.DelayedRefreshUI: player={playerName}, health={healthStr}, stamina={staminaStr}");
        HealthUpdate();
        StaminaUpdate();
    }

    private void RefreshPlayerReference()
    {
        if (playerAttributes == null)
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) playerAttributes = p.GetComponent<PlayerAttributes>();
        }
        else
        {
            if (playerAttributes.gameObject == null || !playerAttributes.gameObject.activeInHierarchy)
            {
                var p = GameObject.FindWithTag("Player");
                if (p != null) playerAttributes = p.GetComponent<PlayerAttributes>();
            }
        }
    }

    private void Update()
    {
        HealthUpdate();
        StaminaUpdate();
    }

    /// <summary>
    /// 当生命的显示值与实际值不符时, 更新显示
    /// </summary>
    private void HealthUpdate()
    {
        if (playerAttributes == null)
        {
            Debug.Log("HeadUpDisplayer.HealthUpdate: playerAttributes == null");
            if (GameUIManager.Instance == null) Debug.Log("HeadUpDisplayer.HealthUpdate: GameUIManager.Instance == null");
            return;
        }

        if (GameUIManager.Instance == null)
        {
            string pn = playerAttributes.gameObject != null ? playerAttributes.gameObject.name : "unknown";
            Debug.Log("HeadUpDisplayer.HealthUpdate: GameUIManager.Instance == null (player present: " + pn + ")");
            return;
        }

        if (health != playerAttributes.health || maxHealth != playerAttributes.MaxHealth)
        {
            int newHealth = playerAttributes.health;
            int newMax = playerAttributes.MaxHealth;
            Debug.Log($"HeadUpDisplayer.HealthUpdate: detected change -> health {health} -> {newHealth}, maxHealth {maxHealth} -> {newMax}");
            health = newHealth;
            maxHealth = newMax;
            if (maxHealth <= 0)
            {
                Debug.LogWarning("HeadUpDisplayer.HealthUpdate: MaxHealth <= 0, adjusting to 1 to avoid division by zero.");
                maxHealth = 1;
            }

            if (GameUIManager.Instance.healthBar != null)
            {
                float v = (float)health / maxHealth;
                GameUIManager.Instance.healthBar.value = v;
                Debug.Log("HeadUpDisplayer.HealthUpdate: set healthBar.value = " + v);
            }
            else
            {
                Debug.Log("HeadUpDisplayer.HealthUpdate: GameUIManager.Instance.healthBar == null");
            }

            if (GameUIManager.Instance.healthText != null)
            {
                string txt = $"生命：{health} / {maxHealth}";
                GameUIManager.Instance.healthText.text = txt;
                Debug.Log("HeadUpDisplayer.HealthUpdate: set healthText = " + txt);
            }
            else
            {
                Debug.Log("HeadUpDisplayer.HealthUpdate: GameUIManager.Instance.healthText == null");
            }
        }

        if (GameUIManager.Instance.healthBarSlow != null)
        {
            float target = maxHealth > 0 ? (float)health / maxHealth : 0f;
            GameUIManager.Instance.healthBarSlow.value = Mathf.Lerp(GameUIManager.Instance.healthBarSlow.value, target, 1.5f * Time.deltaTime);
        }
    }

    /// <summary>
    /// 当体力值的显示值与实际值不符时, 更新显示
    /// </summary>
    private void StaminaUpdate()
    {
        if (playerAttributes == null || GameUIManager.Instance == null) return;
        if (stamina != playerAttributes.CurrentStaminaInt || maxStamina != playerAttributes.MaxStaminaInt)
        {
            stamina = playerAttributes.CurrentStaminaInt;
            maxStamina = playerAttributes.MaxStaminaInt;
            float staminaRatio = maxStamina > 0 ? (float)stamina / maxStamina : 0;
            if (GameUIManager.Instance.staminaBar != null) GameUIManager.Instance.staminaBar.value = staminaRatio;
            if (GameUIManager.Instance.staminaText != null) GameUIManager.Instance.staminaText.text = $"体力：{stamina} / {maxStamina}";
            GameUIManager.Instance.SetStaminaBarColor(GetStaminaColor(staminaRatio));
        }
    }

    private Color GetStaminaColor(float ratio)
    {
        if (ratio <= 0.25f) return staminaLowColor;
        if (ratio <= 0.55f) return staminaMidColor;
        return staminaHighColor;
    }
}
