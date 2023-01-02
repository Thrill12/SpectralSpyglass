using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Script for managing the camera aspects of the simulation, and allowing us to travel within space
/// </summary>
public class CameraController : MonoBehaviour
{
    Simulation sim;
    [HideInInspector] public BaseBody currentTracking;
    public Camera cam;
    UIManager ui;

    // This is an empty gameObject that is at the centre of the body we are tracking. To rotate the camera in orbit mode, we simply rotate
    // this object when the camera is a child of it so we get a much easier system for rotating the camera around a planet
    [HideInInspector] public GameObject rotateTarget;
    // The sensitivity at which we move the camera around in orbit mode
    public float moveSens = 15;

    int currIndex = 0;
    // Variable to keep track of whether we are in free cam mode or not
    public bool freeCam = false;
    // The move speed of camera in free cam mode
    public float moveSpeed = 1;

    // These variables hold the min and max speed of the camera in free cam mode
    public float minSpeed = 0.001f;
    public float maxSpeed = 0.01f;

    // Unity function which runs at the start of the scene earlier than the start function
    private void Awake()
    {
        sim = FindObjectOfType<Simulation>();
        cam = GetComponentInChildren<Camera>();
        ui = FindObjectOfType<UIManager>();
    }

    // Unity function which runs at the start of the scene
    private void Start()
    {
        ChangeParent();
    }

    // Unity Loop which runs on every frame
    private void Update()
    {
        // This allows us to rotate around the observed body when in orbit mode
        HandleOrbit();

        // This enables us to turn on free cam mode and explore space freely.
        if (Input.GetMouseButtonDown(1) && !EventSystem.current.IsPointerOverGameObject())
        {
            ui.DeleteContext();
            GetComponentInChildren<Transform>().position = transform.position;
            freeCam = true;
        }

        if (freeCam)
        {         
            FreeCamMove();
        }
    }

    /// <summary>
    /// Simple function that just translates the position of the camera each frame by whatever inputs we have.
    /// WASDQE for the movement of the camera.
    /// </summary>
    private void FreeCamMove()
    {
        transform.parent = null;
        Camera.main.transform.position = transform.position;

        transform.Translate(-Vector3.up * moveSpeed * Input.GetAxis("Vertical"), Space.Self);
        transform.Translate(Vector3.right * moveSpeed * Input.GetAxis("Horizontal"), Space.Self);
        transform.Translate(Vector3.forward * moveSpeed * Input.GetAxis("UpDown"), Space.Self);

        // Instead of zooming in with the mouse wheel, in free cam we increase the speed
        float scrlWheel = Input.GetAxis("Mouse ScrollWheel") / 60;
        moveSpeed = Mathf.Clamp(moveSpeed + scrlWheel, minSpeed, maxSpeed);
    }

    /// <summary>
    /// Handling rotation around the current observed body. I think this
    /// can be extended to have more control - maybe implement
    /// a PIP sphere with rotation circles in 3 axis for easier rotation.
    /// </summary>
    private void HandleOrbit()
    {
        if (!EventSystem.current.IsPointerOverGameObject() && !freeCam)
        {
            if (Input.GetMouseButton(0))
            {
                ui.DeleteContext();
                transform.RotateAround(rotateTarget.transform.position, cam.transform.up, Input.GetAxis("Mouse X") * moveSens * Time.deltaTime);
                transform.RotateAround(rotateTarget.transform.position, cam.transform.right, -Input.GetAxis("Mouse Y") * moveSens * Time.deltaTime);
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
        else if (!EventSystem.current.IsPointerOverGameObject() && freeCam)
        {
            if (Input.GetMouseButton(1))
            {
                ui.DeleteContext();
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;

                transform.Rotate(0, 0, Input.GetAxis("Mouse X") * moveSens * Time.deltaTime);
                transform.Rotate(-Input.GetAxis("Mouse Y") * moveSens * Time.deltaTime, 0, 0);

                //transform.Rotate(-Input.GetAxis("Mouse Y") * moveSens * Time.smoothDeltaTime, 0, Input.GetAxis("Mouse X") * moveSens * Time.smoothDeltaTime);
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }

    /// <summary>
    /// This allows us to move to the body in the previous position in the array, and also accounts for reaching the end/beginning of the array
    /// </summary>
    public void PrevInArray()
    {
        currIndex--;
        CheckOverflowArrayGT();

        ChangeParent();
    }

    /// <summary>
    /// This allows us to move to the body in the next position in the array, and also accounts for reaching the end/beginning of the array
    /// </summary>
    public void NextInArray()
    {
        currIndex++;
        CheckOverflowArrayLT();

        ChangeParent();
    }

    /// <summary>
    /// Function to move the index from the bottom to the end if user got to the start of the array
    /// </summary>
    private void CheckOverflowArrayLT()
    {
        if (currIndex > sim.bodies.Count - 1)
        {
            currIndex = 0;
        }
    }

    /// <summary>
    /// Function to move the index from the bottom to the end if user got to the start of the array
    /// </summary>
    private void CheckOverflowArrayGT()
    {
        if (currIndex < 0)
        {
            currIndex = sim.bodies.Count - 1;
        }
    }

    /// <summary>
    /// Changes the parent of the camera so that it follows another planet
    /// </summary>
    private void ChangeParent()
    {
        ui.DeleteContext();
        transform.SetPositionAndRotation(transform.position, Quaternion.identity) ;
        currentTracking = sim.bodies[currIndex];
        rotateTarget = currentTracking.gameObject;

        // Making sure that the view doesn't mess up each time we switch viewings.
        // This spawns the camera so that each body takes up the same 
        // space in the camera when switched to it. Again,
        // this might change when I switch from FOV zoom to physical zoom.
        transform.SetParent(sim.bodies[currIndex].transform);
        float oldHeight = transform.position.y;

        ui.observedBody = currentTracking;
        // This sets the position of the camera using the celestial body's radius so that the camera doesn't spawn inside an object when switching to its view.
        transform.position = new Vector3(transform.parent.position.x, transform.parent.position.y, transform.parent.position.z);
        cam.gameObject.transform.position = new Vector3(transform.position.x, Mathf.Clamp(transform.position.y, transform.position.y + (currentTracking.transform.localScale.x) * 10, oldHeight), transform.position.z);  
        transform.LookAt(rotateTarget.transform.position);
        sim.DeleteExistingOrbits();
    }

    /// <summary>
    /// Simply changes the parent of the camera, which allows the camera to move alongside its parent and be stuck in a relative position relative to it
    /// </summary>
    /// <param name="index"></param>
    public void ChangeParent(int index)
    {
        currIndex = index;
        ChangeParent();
    }
}
