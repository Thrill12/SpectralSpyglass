using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CameraController : MonoBehaviour
{
    Simulation sim;
    [HideInInspector] public BaseBody currentTracking;
    public Camera cam;
    UIManager ui;

    [HideInInspector] public GameObject rotateTarget;
    public float moveSens = 15;

    int currIndex = 0;

    public bool freeCam = false;
    public float moveSpeed = 1;

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
        HandleOrbit();

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

    private void FreeCamMove()
    {
        transform.parent = null;
        Camera.main.transform.position = transform.position;

        transform.Translate(-Vector3.up * moveSpeed * Input.GetAxis("Vertical"), Space.Self);
        transform.Translate(Vector3.right * moveSpeed * Input.GetAxis("Horizontal"), Space.Self);
        transform.Translate(Vector3.forward * moveSpeed * Input.GetAxis("UpDown"), Space.Self);

        float scrlWheel = Input.GetAxis("Mouse ScrollWheel") / 120;
        moveSpeed = Mathf.Clamp(moveSpeed + scrlWheel, minSpeed, maxSpeed);
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

    public void PrevInArray()
    {
        currIndex--;
        CheckOverflowArrayGT();

        ChangeParent();
    }

    public void NextInArray()
    {
        currIndex++;
        CheckOverflowArrayLT();

        ChangeParent();
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

    public void ChangeParent(int index)
    {
        currIndex = index;
        ChangeParent();
    }
}
