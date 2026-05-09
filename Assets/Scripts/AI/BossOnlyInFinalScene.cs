using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[AddComponentMenu("AI/Boss Only In FinalScene")]
public class BossOnlyInFinalScene : MonoBehaviour
{
    [Tooltip("Scene name where this boss should exist (case-sensitive)")]
    public string allowedSceneName = "FinalScene";

    [Tooltip("If true, destroy this GameObject when not in allowed scene. If false, disable it.")]
    public bool destroyWhenOutside = true;

    private void Awake()
    {
        if (SceneManager.GetActiveScene().name != allowedSceneName)
        {
            if (destroyWhenOutside)
                Destroy(gameObject);
            else
                gameObject.SetActive(false);
        }
    }
}