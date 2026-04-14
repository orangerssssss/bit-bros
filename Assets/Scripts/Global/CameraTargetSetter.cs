using UnityEngine;
using Cinemachine;
using UnityEngine.SceneManagement;

public class CameraTargetSetter : MonoBehaviour
{
    private CinemachineVirtualCamera vcam;
    private string targetSceneName = "Imagination"; // Ganti dengan nama scene mimpi kamu
    
    void Awake()
    {
        vcam = GetComponent<CinemachineVirtualCamera>();
    }
    
    void Start()
    {
        // Cek apakah kita sedang di Scene 2
        if (SceneManager.GetActiveScene().name == targetSceneName)
        {
            // Hanya set target jika di Scene 2
            SetCameraTarget();
        }
    }
    
    void SetCameraTarget()
    {
        // Cari parent dengan tag "Player"
        Transform parent = transform.parent;
        while (parent != null)
        {
            if (parent.CompareTag("Player"))
            {
                if (vcam != null)
                {
                    vcam.Follow = parent;
                    vcam.LookAt = parent;
                    Debug.Log("Camera target set to: " + parent.name);
                }
                return;
            }
            parent = parent.parent;
        }
        
        Debug.LogWarning("Player not found as parent, trying FindWithTag...");
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null && vcam != null)
        {
            vcam.Follow = player.transform;
            vcam.LookAt = player.transform;
        }
    }
}