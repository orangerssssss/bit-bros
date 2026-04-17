using System.Reflection;
using UnityEngine;
using Cinemachine;
using UnityEngine.SceneManagement;

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
            // Set container position/rotation
            playerControl.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);

            // Prefer using PlayerMoveController to set player position+rotation safely
            var moveCtrl = player.GetComponent<PlayerMoveController>();
            if (moveCtrl != null)
            {
                moveCtrl.SetPositionAndRotation(spawnPoint);
                Debug.Log("PlayerMoveController.SetPositionAndRotation called for spawn");
            }
            else
            {
                player.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
            }

            // Pastikan tag Player ada supaya script lain dapat menemukan Player dengan tag
            try
            {
                player.tag = "Player";
                playerControl.tag = "Player";
            }
            catch { }
            
            // Ensure persistent player and its children are set to PlayerCamp layer at runtime
            int playerCampLayer = LayerMask.NameToLayer("PlayerCamp");
            if (playerCampLayer >= 0)
            {
                SetLayerRecursive(player, playerCampLayer);
                SetLayerRecursive(playerControl, playerCampLayer);
                Debug.Log($"PlayerSpawner: set Player and PlayerControl layers to PlayerCamp ({playerCampLayer}) at runtime");
            }

            // If persistent player's Animator lacks a controller, try to copy from the scene's PlayerControl dummy
            try
            {
                var realAnimator = player.GetComponentInChildren<Animator>();
                var dummyAnimator = playerControl.GetComponentInChildren<Animator>();
                if (realAnimator != null && (realAnimator.runtimeAnimatorController == null) && dummyAnimator != null && dummyAnimator.runtimeAnimatorController != null)
                {
                    realAnimator.runtimeAnimatorController = dummyAnimator.runtimeAnimatorController;
                    Debug.Log("PlayerSpawner: copied AnimatorController from PlayerControl dummy to persistent Player at runtime");
                }
            }
            catch { }

            if (cc != null) cc.enabled = true;
        }
    }

    private void SetLayerRecursive(GameObject go, int layer)
    {
        if (go == null) return;
        try
        {
            go.layer = layer;
            foreach (Transform t in go.transform)
            {
                SetLayerRecursive(t.gameObject, layer);
            }
        }
        catch { }
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
                    // Try to set transposer binding mode to WorldSpace so the camera offset doesn't rotate with the player
                    try
                    {
                        var transposer = vcam.GetCinemachineComponent<CinemachineTransposer>();
                        if (transposer != null)
                        {
                            var field = transposer.GetType().GetField("m_BindingMode", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                            if (field != null)
                            {
                                var enumType = field.FieldType;
                                string[] tryNames = new[] { "WorldSpace", "SimpleFollowWithWorldUp", "LockToTargetWithWorldUp" };
                                foreach (var name in tryNames)
                                {
                                    try
                                    {
                                        var parsed = System.Enum.Parse(enumType, name);
                                        field.SetValue(transposer, parsed);
                                        Debug.Log("Set CinemachineTransposer.m_BindingMode to " + name);
                                        break;
                                    }
                                    catch { }
                                }
                            }
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning("Failed to set transposer binding mode: " + e.Message);
                    }
                    // Try to add CinemachineCollider to prevent camera clipping if package is available
                    try
                    {
                        var existing = vcam.GetComponent<Cinemachine.CinemachineCollider>();
                        if (existing == null)
                        {
                            vcam.gameObject.AddComponent<Cinemachine.CinemachineCollider>();
                            Debug.Log("Added CinemachineCollider to vcam: " + vcam.gameObject.name);
                        }
                    }
                    catch { }

                    // Adjust sensitivity and view distance per-scene so Imagination can feel different
                            if (view != null)
                            {
                                // Apply per-scene camera settings: Imagination uses lower sensitivity and closer camera.
                                string sceneName = SceneManager.GetActiveScene().name;
                                if (sceneName == "Imagination")
                                {
                                    view.SetSensitivity(0.9f, 0.55f);
                                    // Slightly increase camera distance for scene 2 (Imagination)
                                    view.SetViewDistance(3.5f);                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 
                                    Debug.Log($"Applied Imagination camera settings: sensitivity(0.9,0.55), viewDistance={2.2f}");
                                }
                                else
                                {
                                    // Reduce default sensitivity for other scenes (e.g., scene 1) to avoid feeling too twitchy
                                    view.SetSensitivity(1.0f, 0.6f);
                                    view.SetViewDistance(3.2f);
                                    Debug.Log("Applied default camera settings: sensitivity(1.0,0.6), viewDistance=3.2");
                                }
                            }
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