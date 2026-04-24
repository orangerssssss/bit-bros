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

    [Tooltip("Jika dicentang, gunakan 'absolutePosition' sebagai posisi spawn di world (bukan relatif terhadap musuh)")]
    public bool useAbsolutePosition = false;

    [Tooltip("Posisi absolut (world) untuk spawn portal bila 'useAbsolutePosition' dicentang")]
    public Vector3 absolutePosition = Vector3.zero;

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

        Debug.Log($"SpawnPortalOnDeath: OnCharacterBeforeDeath received for {attr.gameObject.name} at pos={attr.transform.position} (this={gameObject.name}). spawnDelay={spawnDelay}, useAbsolutePosition={useAbsolutePosition}");

        if (onlyOnce && hasSpawned) return;
        if (portalPrefab == null)
        {
            Debug.LogWarning($"SpawnPortalOnDeath: portalPrefab is null on {gameObject.name}, skipping spawn.");
            return;
        }

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
        Vector3 pos = useAbsolutePosition ? absolutePosition : transform.position + spawnOffset;
        Debug.Log($"SpawnPortalOnDeath: Spawning portalPrefab '{portalPrefab.name}' at {pos} (absolute={useAbsolutePosition}) from {gameObject.name}");
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
        Vector3 pos = useAbsolutePosition ? absolutePosition : transform.position + spawnOffset;
        Debug.Log($"SpawnPortalOnDeath: TriggerPortalSpawnImmediate called on {gameObject.name} -> spawning '{portalPrefab.name}' at {pos}");
        Instantiate(portalPrefab, pos, Quaternion.identity);
        hasSpawned = true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 gizmoPos = useAbsolutePosition ? absolutePosition : transform.position + spawnOffset;
        Gizmos.DrawWireSphere(gizmoPos, 0.5f);
    }
}
