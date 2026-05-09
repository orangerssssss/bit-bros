using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Debug/Animator State Logger")]
public class AnimatorStateLogger : MonoBehaviour
{
    public Animator animator;
    [Tooltip("Minimum seconds between logs to avoid spam")]
    public float logCooldown = 0.5f;

    private float lastLogTime = -10f;
    private int lastStateHash = 0;

    private void Start()
    {
        if (animator == null) animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (animator == null) return;

        var state = animator.GetCurrentAnimatorStateInfo(0);
        if (state.fullPathHash != lastStateHash || Time.time - lastLogTime > logCooldown)
        {
            lastStateHash = state.fullPathHash;
            lastLogTime = Time.time;
            Debug.Log($"{name} AnimatorStateLogger: curTag[Attack]={state.IsTag("Attack")}, curTag[Move]={state.IsTag("Move")}, curTag[Died]={state.IsTag("Died")}, normalizedTime={state.normalizedTime:F2}, length={state.length:F2}");
        }
    }
}
