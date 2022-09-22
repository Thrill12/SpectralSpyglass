using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    Simulation sim;
    [HideInInspector] public BaseBody currentTracking;
    public Camera cam;

    [HideInInspector] public GameObject rotateTarget;
    public float moveSens = 15;

    int currIndex = 0;

    // Unity function which runs at the start of the scene earlier than the start function
    private void Awake()
    {
        sim = FindObjectOfType<Simulation>();
        cam = GetComponentInChildren<Camera>();
    }

    // Unity function which runs at the start of the scene
    private void Start()
    {
        ChangeParent();
    }

    // Unity Loop which runs on every frame
    private void Update()
    {
        HandleSwitchingPlanets();
        HandleOrbit();
    }

    // Function for zooming in the camera. Another alternative was to use the FOV of the camera,
    // However I ended up using the position of it because it didn't alter the view of the planets
    // like the FOV would.
    //private void HandleZoom()
    //{
    //    float scrlWheel = Input.GetAxis("Mouse ScrollWheel");

    //    float zoomVector = -scrlWheel * Vector3.Distance(transform.position, transform.parent.position);

    //    Vector3 dir = transform.position - transform.parent.position;

    //    transform.Translate(dir.normalized * zoomVector);
    //}

    //// OLD Function for handling the orbit of the camera around a body by dragging the mouse.
    //private void HandleOrbit()
    //{
    //    float x = Input.GetAxis("Horizontal");
    //    float y = Input.GetAxis("Vertical");

    //    transform.RotateAround(transform.parent.position, transform.parent.forward, y * orbitSpeed);
    //    transform.RotateAround(transform.parent.position, transform.parent.up, x * orbitSpeed);
    //}

    // Handling rotation around the current observed body. I think this
    // will need to change eventually to have more control - maybe implement
    // a PIP sphere with rotation circles in 3 axis for easier rotation.
    private void HandleOrbit()
    {
        if (Input.GetMouseButton(0))
        {
            transform.RotateAround(rotateTarget.transform.position, cam.transform.up, -Input.GetAxis("Mouse X") * moveSens * Time.smoothDeltaTime);
            transform.RotateAround(rotateTarget.transform.position, cam.transform.right, Input.GetAxis("Mouse Y") * moveSens * Time.smoothDeltaTime);
        }
    }

    // Function for switching which planet the user is observing
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

    // Function to move the index from the bottom to the end if user got to the start of the array
    private void CheckOverflowArrayLT()
    {
        if (currIndex > sim.bodies.Length - 1)
        {
            currIndex = 0;
        }
    }

    // Function to move the index from the bottom to the end if user got to the start of the array
    private void CheckOverflowArrayGT()
    {
        if (currIndex < 0)
        {
            currIndex = sim.bodies.Length - 1;
        }
    }

    // Changes the parent of the camera so that it follows another planet
    private void ChangeParent()
    {
        transform.SetPositionAndRotation(transform.position, Quaternion.identity) ;
        currentTracking = sim.bodies[currIndex];
        rotateTarget = currentTracking.gameObject;

        // Making sure that the view doesn't mess up each time we switch viewings.
        // This spawns the camera so that each body takes up the same 
        // space in the camera when switched to it. Again,
        // this might change when I switch from FOV zoom to physical zoom.
        transform.SetParent(sim.bodies[currIndex].transform);
        float oldHeight = transform.position.y;

        // This sets the position of the camera using the celestial body's radius so that the camera doesn't spawn inside an object when switching to its view.
        transform.position = new Vector3(transform.parent.position.x, transform.parent.position.y, transform.parent.position.z);
        cam.gameObject.transform.position = new Vector3(transform.position.x, Mathf.Clamp(transform.position.y, transform.position.y + (currentTracking.transform.localScale.x) * 10, oldHeight), transform.position.z);  
        transform.LookAt(rotateTarget.transform.position);
    }
}
