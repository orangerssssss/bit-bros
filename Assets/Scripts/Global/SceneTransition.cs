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
            SceneManager.LoadScene(sceneName);
        }
    }
}