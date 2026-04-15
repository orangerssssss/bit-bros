using System.Collections;
using UnityEngine;

/// <summary>
/// Spawn portal ketika karakter ini akan mati.
/// Subscribe ke GameEventManager.characterBeforeDeathEvent dan Instantiate prefab portal.
/// Attach ke prefab musuh dan set `portalPrefab` ke Assets/PortalToScene3.prefab
/// </summary>
public class SpawnPortalOnDeath : MonoBehaviour
{
    [Tooltip("Prefab portal untuk di-spawn (contoh: Assets/PortalToScene3.prefab)")]
    public GameObject portalPrefab;

    [Tooltip("Posisi relatif terhadap posisi musuh tempat portal muncul")]
    public Vector3 spawnOffset = Vector3.zero;

    [Tooltip("Delay sebelum spawn portal (detik)")]
    public float spawnDelay = 0f;

    [Tooltip("Hanya spawn satu kali")]
    public bool onlyOnce = true;

    bool hasSpawned = false;
    CharacterAttributes characterAttributes;
    bool subscribed = false;
    Coroutine waitCoroutine = null;

    private void Awake()
    {
        characterAttributes = GetComponent<CharacterAttributes>();
    }

    private void OnEnable()
    {
        // try immediate subscribe, otherwise wait until GameEventManager exists
        TrySubscribe();
    }

    private void OnDisable()
    {
        if (subscribed && GameEventManager.Instance != null && GameEventManager.Instance.characterBeforeDeathEvent != null)
        {
            GameEventManager.Instance.characterBeforeDeathEvent.RemoveListener(OnCharacterBeforeDeath);
            subscribed = false;
        }
        if (waitCoroutine != null) StopCoroutine(waitCoroutine);
    }

    void TrySubscribe()
    {
        var gem = GameEventManager.Instance;
        if (gem != null && gem.characterBeforeDeathEvent != null)
        {
            if (!subscribed)
            {
                gem.characterBeforeDeathEvent.AddListener(OnCharacterBeforeDeath);
                subscribed = true;
            }
        }
        else
        {
            if (waitCoroutine == null) waitCoroutine = StartCoroutine(WaitForGameEventManager());
        }
    }

    IEnumerator WaitForGameEventManager()
    {
        while (GameEventManager.Instance == null)
        {
            yield return null;
        }
        if (GameEventManager.Instance != null && GameEventManager.Instance.characterBeforeDeathEvent != null)
        {
            if (!subscribed)
            {
                GameEventManager.Instance.characterBeforeDeathEvent.AddListener(OnCharacterBeforeDeath);
                subscribed = true;
            }
        }
        waitCoroutine = null;
    }

    private void OnCharacterBeforeDeath(CharacterAttributes attr)
    {
        if (attr == null) return;
        if (characterAttributes == null) characterAttributes = GetComponent<CharacterAttributes>();
        if (attr != characterAttributes) return; // bukan kematian dari karakter ini

        if (onlyOnce && hasSpawned) return;
        if (portalPrefab == null) return;

        if (spawnDelay <= 0f)
        {
            SpawnPortal();
        }
        else
        {
            StartCoroutine(SpawnAfterDelay(spawnDelay));
        }
    }

    IEnumerator SpawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SpawnPortal();
    }

    void SpawnPortal()
    {
        if (portalPrefab == null) return;
        Vector3 pos = transform.position + spawnOffset;
        Instantiate(portalPrefab, pos, Quaternion.identity);
        hasSpawned = true;
    }

    /// <summary>
    /// Public API to trigger immediate portal spawn (ignores spawnDelay).
    /// Useful as a fallback when GameEventManager is not present.
    /// </summary>
    public void TriggerPortalSpawnImmediate()
    {
        if (onlyOnce && hasSpawned) return;
        if (portalPrefab == null) return;
        Vector3 pos = transform.position + spawnOffset;
        Instantiate(portalPrefab, pos, Quaternion.identity);
        hasSpawned = true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position + spawnOffset, 0.5f);
    }
}
