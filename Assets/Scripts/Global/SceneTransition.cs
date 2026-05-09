using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransition : MonoBehaviour
{
    public string sceneName;
    
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Triggered by: " + other.name);  // Cek siapa yang kena portal
        
        if (other.CompareTag("Player"))
        {
            Debug.Log("Loading scene: " + sceneName);

            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("SceneTransition: sceneName is empty.");
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"SceneTransition: Scene '{sceneName}' can't be loaded. Add it to Build Settings or check the exact scene name.");
                return;
            }

            if (SceneLoader.instance != null)
            {
                SceneLoader.instance.LoadScene(sceneName, true);
            }
            else
            {
                SceneManager.LoadScene(sceneName);
            }
        }
    }
}
