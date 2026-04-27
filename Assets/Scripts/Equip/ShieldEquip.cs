using UnityEngine;

[RequireComponent(typeof(PlayerBlock))]
public class ShieldEquip : MonoBehaviour
{
    [Header("Defaults")]
    [Tooltip("Optional default shield prefab to equip if not provided on Equip() call")]
    public GameObject defaultShieldPrefab;

    [Tooltip("Optional manual left-hand holder transform. If empty, the animator's LeftHand bone is used (Humanoid rigs).")]
    public Transform leftHandHolder;

    private GameObject currentShieldInstance;
    private PlayerBlock playerBlock;
    private Animator animator;

    private void Awake()
    {
        playerBlock = GetComponent<PlayerBlock>();
        animator = GetComponentInChildren<Animator>();
    }

    /// <summary>
    /// Equip given shield prefab to left hand. If null, uses defaultShieldPrefab.
    /// Returns the instantiated shield GameObject or null on failure.
    /// </summary>
    public GameObject Equip(GameObject shieldPrefab)
    {
        if (shieldPrefab == null) shieldPrefab = defaultShieldPrefab;
        if (shieldPrefab == null)
        {
            Debug.LogWarning("ShieldEquip: No shield prefab provided.");
            return null;
        }

        if (currentShieldInstance != null) Destroy(currentShieldInstance);

        Transform parent = leftHandHolder;
        if (parent == null && animator != null && animator.isHuman)
        {
            parent = animator.GetBoneTransform(HumanBodyBones.LeftHand);
        }

        if (parent == null)
        {
            Debug.LogWarning("ShieldEquip: Left hand transform not found. Assign LeftHandHolder in inspector.");
            return null;
        }

        currentShieldInstance = Instantiate(shieldPrefab, parent);
        currentShieldInstance.transform.localPosition = Vector3.zero;
        currentShieldInstance.transform.localRotation = Quaternion.identity;
        currentShieldInstance.transform.localScale = Vector3.one;

        playerBlock?.SetEquippedShield(currentShieldInstance);

        return currentShieldInstance;
    }

    public void Unequip()
    {
        if (currentShieldInstance != null)
        {
            Destroy(currentShieldInstance);
            currentShieldInstance = null;
            playerBlock?.SetEquippedShield(null);
        }
    }
}
