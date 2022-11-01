using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraZoom : MonoBehaviour
{
    public float minFov = 15f;
    public float maxFov = 100f;
    public float sensitivity = 10f;

    void Update()
    {
        // Currently doing zoom with FOV however need to change this due to distorting
        // user's vision. It might give users sickness as I have got feedback that
        // there can sometimes be a fisheye lens effect which causes the sickness.
        float fov = Camera.main.fieldOfView;
        fov -= Input.GetAxis("Mouse ScrollWheel") * sensitivity;
        fov = Mathf.Clamp(fov, minFov, maxFov);
        Camera.main.fieldOfView = fov;
    }
}
