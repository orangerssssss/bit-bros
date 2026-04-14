using UnityEngine;

public class DontDestroyOnLoad : MonoBehaviour
{
    private static DontDestroyOnLoad instance;
    
    void Awake()
    {
        // Cek apakah sudah ada instance PlayerControl yang lain
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("PlayerControl persisted: " + gameObject.name);
        }
        else
        {
            Debug.Log("Duplicate PlayerControl detected - destroying");
            Destroy(gameObject);
        }
    }
}