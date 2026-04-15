using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TeleportToScene : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("Nama scene tujuan (harus persis sama dengan nama di Build Settings)")]
    public string targetSceneName = "Scene3";
    
    [Header("Visual Feedback (Opsional)")]
    [Tooltip("Effect particle saat teleport")]
    public ParticleSystem teleportEffect;
    
    [Tooltip("Delay sebelum pindah scene (detik)")]
    public float teleportDelay = 0.5f;
    
    [Tooltip("Apakah portal sekali pakai?")]
    public bool oneTimeUse = true;

    [Tooltip("Jika tersedia, gunakan SceneLoader (asynchronous + loading UI)")]
    public bool useAsyncLoader = true;
    
    private bool hasTeleported = false;
    
    private void OnTriggerEnter(Collider other)
    {
        // Cek apakah yang masuk adalah Player
        if (!other.CompareTag("Player")) return;
        
        // Cek apakah portal masih aktif (untuk one time use)
        if (oneTimeUse && hasTeleported) return;
        
        Debug.Log($"Player menyentuh portal! Pindah ke scene: {targetSceneName}");
        
        hasTeleported = true;
        
        // Mainkan effect jika ada
        if (teleportEffect != null)
        {
            teleportEffect.Play();
        }
        
        // Pindah scene setelah delay
        Invoke(nameof(LoadTargetScene), teleportDelay);
    }
    
    private void LoadTargetScene()
    {
        // Prefer SceneLoader (shows loading UI and loads async) if available
        if (useAsyncLoader && SceneLoader.instance != null)
        {
            SceneLoader.instance.LoadScene(targetSceneName, true);
            return;
        }

        // Fallback: use async load to reduce hitching
        StartCoroutine(LoadSceneAsyncFallback());
    }

    private IEnumerator LoadSceneAsyncFallback()
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(targetSceneName);
        if (op == null)
        {
            // fallback to immediate load
            SceneManager.LoadScene(targetSceneName);
            yield break;
        }

        // wait until done
        while (!op.isDone)
        {
            yield return null;
        }
    }
}