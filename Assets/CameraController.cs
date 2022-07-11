using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    Simulation sim;
    CelestialBody currentTracking;
    public Camera cam;

    int currIndex = 0;

    private void Awake()
    {
        sim = FindObjectOfType<Simulation>();
        cam = GetComponent<Camera>();
    }

    private void Start()
    {
        ChangeParent();
    }

    private void Update()
    {
        HandleZoom();
        HandleSwitchingPlanets();
    }

    private void HandleZoom()
    {
        float scrlWheel = Input.GetAxis("Mouse ScrollWheel");

        float zoomVector = -scrlWheel * Vector3.Distance(transform.position, transform.parent.position);

        transform.SetPositionAndRotation(new Vector3(transform.position.x, transform.position.y + zoomVector, transform.position.z), transform.rotation);
    }

    private void HandleSwitchingPlanets()
    {
        if (Input.GetKeyDown(KeyCode.D))
        {
            currIndex--;
            CheckOverflowArrayGT();

            ChangeParent();
        }
        else if (Input.GetKeyDown(KeyCode.A))
        {
            currIndex++;
            CheckOverflowArrayLT();

            ChangeParent();
        }
    }

    private void CheckOverflowArrayLT()
    {
        if (currIndex > sim.bodies.Length - 1)
        {
            currIndex = 0;
        }
    }

    private void CheckOverflowArrayGT()
    {
        if (currIndex < 0)
        {
            currIndex = sim.bodies.Length - 1;
        }
    }

    private void ChangeParent()
    {
        currentTracking = sim.bodies[currIndex];
        transform.SetParent(sim.bodies[currIndex].transform);
        transform.position = new Vector3(transform.parent.position.x, currentTracking.transform.position.y + currentTracking.radius * 10, transform.parent.position.z);
    }
}
