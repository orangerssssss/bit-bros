using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerBlock : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [Header("Parameters")]
    [SerializeField] private string blockTrigger = "Block";
    [SerializeField] private string isBlockingBool = "isShielding";
    [Header("Options")]
    [Tooltip("If true, hold right mouse to block; if false, single-press triggers block animation")]
    [SerializeField] private bool useHold = false;

    // runtime flag used by animation events
    private bool isParryWindow = false;
    // runtime flag for hold-mode blocking state
    private bool isBlocking = false;
    // instance of equipped shield (if any)
    private GameObject equippedShield = null;
    private bool loggedMissingIsBlockingParam = false;
    private bool loggedMissingBlockTriggerParam = false;

    private void Reset()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (animator == null) return;

        if (useHold)
        {
            bool holdInput = Input.GetMouseButton(1); // right mouse button (hold)
            // only allow blocking when a shield is equipped
            bool hold = holdInput && HasShield();
            // always update runtime flag so damage logic works even if Animator parameter missing
            isBlocking = hold;
            // set animator parameter only if it exists to avoid errors
            if (AnimatorHasParameter(isBlockingBool))
            {
                animator.SetBool(isBlockingBool, hold);
            }
            else if (!loggedMissingIsBlockingParam)
            {
                Debug.LogWarning($"PlayerBlock: Animator parameter '{isBlockingBool}' not found. Blocking will still prevent damage but no animation will play.");
                loggedMissingIsBlockingParam = true;
            }
        }
        else
        {
            if (Input.GetMouseButtonDown(1) && HasShield()) // right mouse click (single press)
            {
                // set runtime flag briefly (useful if other systems check IsBlocking)
                isBlocking = true;
                if (AnimatorHasParameter(blockTrigger))
                {
                    animator.SetTrigger(blockTrigger);
                }
                else if (!loggedMissingBlockTriggerParam)
                {
                    Debug.LogWarning($"PlayerBlock: Animator trigger '{blockTrigger}' not found. Blocking will still be applied logically.");
                    loggedMissingBlockTriggerParam = true;
                }
            }
            // reset runtime flag when mouse released
            if (Input.GetMouseButtonUp(1)) isBlocking = false;
        }
    }

    // Called by Animation Event at parry window start
    public void OnParryStart()
    {
        isParryWindow = true;
    }

    // Called by Animation Event at parry window end
    public void OnParryEnd()
    {
        isParryWindow = false;
    }

    // Example helper for enemy scripts to query
    public bool IsInParryWindow() => isParryWindow;

    // Whether player is currently holding block (useHold mode)
    // Only returns true when a shield is equipped
    public bool IsBlocking() => isBlocking && HasShield();

    private bool AnimatorHasParameter(string paramName)
    {
        if (animator == null) return false;
        foreach (var p in animator.parameters)
            if (p.name == paramName) return true;
        return false;
    }

    // Called by equip logic to register the shield instance
    public void SetEquippedShield(GameObject shieldInstance)
    {
        equippedShield = shieldInstance;
    }

    public bool HasShield() => equippedShield != null;
}
