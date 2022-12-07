using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AlwaysFaceCamera : MonoBehaviour
{
    Camera camera;

    private void Start()
    {
        camera = GetComponent<Canvas>().worldCamera;
    }

    void Update()
    {
        transform.LookAt(camera.transform, camera.gameObject.transform.up);
    }
}
