using UnityEngine;
using Cinemachine;

public class PlayerSpawner : MonoBehaviour
{
    public Transform spawnPoint;
    
    void Start()
    {
        // Pindahkan Player ke spawnPoint (kode yang sudah ada sebelumnya)
        MovePlayerToSpawn();
        
        // Set camera target setelah Player dipindah
        SetCameraTarget();
    }
    
    void MovePlayerToSpawn()
    {
        GameObject player = GameObject.Find("Player");
        GameObject playerControl = GameObject.Find("PlayerControl");
        
        if (player != null && playerControl != null && spawnPoint != null)
        {
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            
            playerControl.transform.position = spawnPoint.position;
            player.transform.position = spawnPoint.position;

            // Pastikan tag Player ada supaya script lain dapat menemukan Player dengan tag
            try
            {
                player.tag = "Player";
                playerControl.tag = "Player";
            }
            catch { }
            
            if (cc != null) cc.enabled = true;
        }
    }
    
    void SetCameraTarget()
    {
        // Cari playerVCam (anak dari PlayerControl)
        GameObject playerControl = GameObject.Find("PlayerControl");
        
        if (playerControl != null)
        {
            // Cari camera di dalam PlayerControl
            // Jika player punya ViewController, inisialisasi ulang kameranya
            var view = playerControl.GetComponentInChildren<ViewController>();
            Transform preferredTarget = null;
            if (view != null)
            {
                view.InitCamera();
                Debug.Log("Player ViewController.InitCamera() called");
                // Prefer playerViewPoint as Cinemachine Follow/LookAt target to avoid feedback loops
                if (view.playerViewPoint != null)
                {
                    preferredTarget = view.playerViewPoint;
                }
                else if (view.playerVCamera != null)
                {
                    preferredTarget = view.playerVCamera;
                }
                else if (view.transform != null)
                {
                    preferredTarget = view.transform;
                }
            }

            // Debug: list semua Virtual Camera di scene dan cek CinemachineBrain pada Camera.main
            var allVcams = GameObject.FindObjectsOfType<Cinemachine.CinemachineVirtualCamera>();
            Debug.Log($"Found {allVcams.Length} CinemachineVirtualCamera(s) in scene");
            foreach (var c in allVcams)
            {
                Debug.Log($"Vcam: {c.gameObject.name}, Priority={c.Priority}, Active={c.gameObject.activeInHierarchy}");
            }
            var mainCam = Camera.main;
            if (mainCam != null)
            {
                var brain = mainCam.GetComponent<Cinemachine.CinemachineBrain>();
                Debug.Log("Main Camera found: " + mainCam.name + ", CinemachineBrain=" + (brain != null));
                if (brain == null)
                {
                    // Tambahkan CinemachineBrain jika belum ada (agar vcam bisa mengendalikan kamera)
                    brain = mainCam.gameObject.AddComponent<Cinemachine.CinemachineBrain>();
                    Debug.Log("CinemachineBrain added to Main Camera at runtime");
                }
            }

            // Jika ada Cinemachine di child PlayerControl, set target ke playerViewPoint jika ada
            CinemachineVirtualCamera vcam = playerControl.GetComponentInChildren<CinemachineVirtualCamera>();
            if (vcam != null)
            {
                if (preferredTarget != null)
                {
                    vcam.Follow = preferredTarget;
                    // Do NOT set LookAt to preferredTarget to avoid rotation feedback loops with ViewController
                    vcam.LookAt = null;
                    vcam.Priority = 1000; // naikan priority supaya jadi active
                    Debug.Log($"CinemachineVirtualCamera Follow set to preferredTarget ({preferredTarget.name}), LookAt cleared (priority raised)");
                    // Log detail ViewController targets if ada
                    var viewComp = playerControl.GetComponent<ViewController>();
                    if (viewComp != null)
                    {
                        Debug.Log($"ViewController.playerVCamera = {viewComp.playerVCamera?.name}, playerViewPoint = {viewComp.playerViewPoint?.name}, followingParent = {viewComp.playerVCamera?.parent?.name}");
                    }
                }
                else
                {
                    vcam.Follow = playerControl.transform;
                    vcam.LookAt = playerControl.transform;
                    vcam.Priority = 1000;
                    Debug.Log("CinemachineVirtualCamera target set to PlayerControl transform (priority raised)");
                    var viewComp = playerControl.GetComponent<ViewController>();
                    if (viewComp != null && viewComp.playerVCamera != null)
                    {
                        Debug.Log($"ViewController.playerVCamera = {viewComp.playerVCamera.name}, followingParent = {viewComp.playerVCamera.parent?.name}");
                    }
                }
            }
            else
            {
                // Jika tidak ada pada PlayerControl, coba cari vcam di scene dan set ke Player
                CinemachineVirtualCamera sceneVcam = GameObject.FindObjectOfType<CinemachineVirtualCamera>();
                if (sceneVcam != null)
                {
                    if (preferredTarget != null)
                    {
                        sceneVcam.Follow = preferredTarget;
                        sceneVcam.LookAt = preferredTarget;
                        Debug.Log("Scene CinemachineVirtualCamera target set to playerViewPoint");
                    }
                    else
                    {
                        GameObject player = GameObject.Find("Player") ?? GameObject.FindWithTag("Player");
                        if (player != null)
                        {
                            sceneVcam.Follow = player.transform;
                            sceneVcam.LookAt = player.transform;
                            Debug.Log("Scene CinemachineVirtualCamera target set to Player transform");
                        }
                    }
                }
            }
        }
    }
}