using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HUDCamera : MonoBehaviour
{
    public GameObject followCamera;

    public void Update()
    {
        Quaternion followCameraRotation = followCamera.transform.rotation;
        Vector3 eulerAngle = followCameraRotation.eulerAngles;
        eulerAngle.x = 90;
        transform.rotation = Quaternion.Euler(eulerAngle);
    }
}
