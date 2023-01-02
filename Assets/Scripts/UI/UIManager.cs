using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

// This is a class to manage the UI elements of the simulation and combining it with the simulation script
public class UIManager : MonoBehaviour
{
    // Reference to the camera script we use in order to move the camera, change the observed body, etc.
    public CameraController cameraController;

    // This is a unity feature which allows us to have a small title in the "Inspector", which is where we assign public variables.
    [Header("Scale Multipliers")]

    // The scale factors I use to change from unity units to real life units
    // The distance one is also used for speed since they are proportional
    public float distanceScaleFactor;
    public float massScaleFactor;

    // This is a unity feature which allows us to leave a small space in the "Inspector" of 5 pixels tall
    [Space(5)]

    [Header("Editable properties")]

    // This is a component which allows me to animate the properties window popping in and out of the side of the screen
    public Animator propertiesAnimator;
    // This bool helps us track whether we have extended the properties window. It is true by default
    public bool isPropertiesOut = true;

    // TMP_InputField is a class/component which allows us to have an input field for the user, and gather events in order to run scripts based on it.
    // These are used as input fields and not normal text boxes because we want the user to be able to update them.
    public TMP_InputField bodyName;
    public TMP_InputField bodyMass;
    public TMP_InputField radius;

    // The button class is a component which triggers events based on the button's states, eg. OnPressed, OnHold, etc. The assigning of functions for each
    // state happens in the inpsector.
    //
    // These buttons are used to handle events to increase/decrease velocity in purely XYZ elements, which is the non-default state of them.
    public Button xUp, xDown, yUp, yDown, zUp, zDown;
    // These buttons are used to handle events in "rocket science" terms. Prograde means towards its current velocity
    // Retrograde means opposite its current velocity.
    // Radial means perpendicular to the prograde line towards the outside of the ellipse
    // anti-radial means the same but towards the inside of the ellipse
    // Normal means perpendicular to the radial and prograde lines going "up" essentially
    // Anti-Normal means the opposite direction of normal
    public Button prograde, retrograde, radial, antiradial, normal, antinormal;
    // This button allows us to toggle between XYZ speed components and "rocket science" speed components.
    public Button relativeVelButton;
    // This allows us to change by how much each press of the button will add/change the velocity.
    public TMP_InputField incrementInput;
    // This is the display of the current velocity of each axis when using XYZ components
    public TMP_InputField velX;
    public TMP_InputField velY;
    public TMP_InputField velZ;
    // This is used in both systems, to show the net velocity of the observed body.
    public TMP_Text velMagnitude;
    // This is a deprecated feature
    public TMP_Text largestInfluencer;    

    [Space(5)]

    [Header("Orbital Properties")]

    // This is the object that hosts the orbit properties, we turn it off/on to display the orbital properties
    public GameObject orbitProperties;
    // Here are the text fields used to display the orbital properties
    public TMP_Text semiMajorAxisText;
    public TMP_Text periodText;
    public TMP_Text perigeeText;
    public TMP_Text apogeeText;

    // Bool to keep track of whether we are using XYZ velocity components or the "rocket science" components
    public bool isChangingXYZVelocity;

    // The game object used to toggle XYZ terms on/off
    public GameObject xyzVelocity;
    // The game object used to toggle "rocket science" terms on/off
    public GameObject relativeVelocity;

    // Button to toggle the SOI visualization
    public GameObject soiToggle;
    // Object to spawn when displaying a SOI
    public GameObject soiPrefab;
    // Variable to keep tracked of the instantiated SOI visualization object
    private GameObject instantiatedSOI;

    // In what I would call relative velocity terms, adding delta v into different directions is not just
    // on the x y or z axis. In orbital mechanics and real life rocket trajectory planning,
    // we plan burns using prograde/retrograde, normal/antinormal, radial/antiradial.
    //
    // Prograde -> towards direction of velocity
    // Retrograde -> opposite direction of velocity
    // Normal -> perpendicular up to the orbital plane
    // Antinormal -> perpendicular down to the orbital plane
    // Radial -> points inside the orbit
    // Antiradial -. points outside the orbit

    #region Slider

    [Space(5)]

    [Header("Speed Slider")]
    // image compnent that changes depending on if the simulation is paused or not
    public Image startPauseButton;
    // The slider managing the changing of the timestep of the simulation, or how fast the simulation is going
    public Slider simSpeedSlider;
    // These two are the different textures used for the playing image and the stopped image
    public Sprite pausedButton;
    public Sprite playingButton;

    [Space(5)]

    [Header("Conics")]

    // This is the slider that allows us to change how far into the future the conics go
    public Slider conicTimeStepSlider;
    // This shows us what the conics are relative to, if at all.
    public TMP_Text conicRelativeTo;
    // This shows how large the sphere of influence of a specific planet relative to the specified body above
    public TMP_Text conicSOIText;
    // Toggle for allowing us to display the conics in purely spatial terms not relative to any bodies.
    public GameObject conicRelativeToggle;

    #endregion

    [Space(5)]

    [Header("Body UI")]
  
    // This is the body that the camera is currently observing, when not in free cam
    public BaseBody observedBody;
    // This is the body that the user has been set as target when right clicking
    public BaseBody targetBody;

    // This is instantiated and becomes a button on the left side of the screen for selecting a planet
    public GameObject planetButtonContent;
    // This is the template for the object to spawn. Its details get changed when the object gets instantiated
    public GameObject planetButtonPrefab;

    // This is the text field for displaying the speed of the free cam
    public TMP_Text camSpeedText;

    // This is the template for the context menu when right clicking a planet on the left hand side panel. It gets populated
    // With all the compatible functions of each planet. For example, you can't take a snapshot of a snapshot so the button doesn't 
    // appear to snapshot a snapshot
    public GameObject contextMenuPrefab;
    // Variable to keep track of the spawned in context menu so that we can destroy it when the user picks an action or clicks away
    [HideInInspector] public GameObject spawnedContextMenu;

    // Text field to display the current frames per second, for debugging reasons
    public TMP_Text fpsCounter;

    [Space(5)]

    [Header("In-World UI")]

    // Color to use when creating the line between the observed body and the target
    public Color targetColor;
    // The material used when creating the target line
    public Material targetLineMaterial;
    // The object used to display the distance on a target line. I need to find a way to make it dynamically scale with the zoom level
    public GameObject targetPrefab;
    // Variable to keep track of the marker so that we can delete it
    public GameObject instantiatedMarker;
    // Button so that we can clear the target through the UI
    public GameObject clearTargetButton;

    [Space(5)]

    [Header("GameObjects")]
    // Object that acts as the beginning of the target line. I use it to hold the line renderer component to draw the line in the world
    public GameObject observer;
    // Variable to keep track of the observer so we can clean it up after the target is gone
    public GameObject instantiatedObserver;

    // Reference to the simulation script where we can get many of the variables needed.
    Simulation sim;
    // Bool to keep track if the simulation is paused or not
    bool isPaused;

    // Unity function that gets called at the beginning of the script - the first iteration of the update method
    private void Start()
    {
        // Getting the reference for the simulation  variable
        sim = FindObjectOfType<Simulation>();

        // Setting the default value of the slider.
        simSpeedSlider.value = sim.timeStep;

        // Here we setup the buttons and what function each button triggers
        AddButtonListeners();
        //SetPlanetButtons();

        // This spawns in the SOI object, however it gets toggled off by the following function immediately
        if (instantiatedSOI == null)
        {
            instantiatedSOI = GameObject.Instantiate(soiPrefab, Vector3.zero, Quaternion.identity);
        }

        // Toggles vizualization of the SOI 
        ToggleSOI();

        // An event handler that will get called whenever we change the planets - this will be 
        // useful if I allow the user to add their own planets in the future, so proper
        // setting up can be done
        sim.onChangePlanets += SetPlanetButtons;
    }

    /// <summary>
    /// This instantiates the buttons on the left hand side of the screen and is used when setting up the planets
    /// in the event. It clears the existing buttons, then spawns in a new one for each body
    /// </summary>
    public void SetPlanetButtons()
    {
        for (int i = 0; i < planetButtonContent.transform.childCount; i++)
        {
            Destroy(planetButtonContent.transform.GetChild(i).gameObject);
        }
        for (int i = 0; i < sim.bodies.Count; i++)
        {
            GameObject pButt = GameObject.Instantiate(planetButtonPrefab, planetButtonContent.transform);
            pButt.GetComponentInChildren<PlanetButtonHolder>().bodyIndex = i;
            pButt.GetComponentInChildren<TMP_Text>().text = sim.bodies[i].bodyName;
        }
    }

    // Unity function that gets called every frame, and allows us to do continuous stuff - this is called
    // the "Game Loop"
    private void Update()
    {
        // We want to update the properties window in real time
        DisplayProperties();

        // Time.smoothDeltaTime is a smoothed version of Time.deltaTime, which is the time it took
        // to render this frame from last frame. We do the reciprocal of it as it is kind of similar
        // to the physics equation v = fY, where v is wave speed, f is frequency and Y is wavelength
        // The v in this case is measured in frames / second, and our Time.deltaTime would act as the lambda
        fpsCounter.text = ((int)(1 / Time.smoothDeltaTime)).ToString();

        // Displays the speed text at the top of the screen for the free cam
        if (cameraController.freeCam)
        {
            camSpeedText.enabled = true;
            camSpeedText.text = Mathf.RoundToInt(cameraController.moveSpeed * 10000) + "x";
        }
        else
        {
            camSpeedText.enabled = false;
        }

        // Figures out if the simulation is paused or not and changes the sprite of the pause button
        // respectively
        if (sim.timeStep != 0)
        {
            isPaused = false;
            startPauseButton.sprite = pausedButton;   
        }
        else
        {
            isPaused = true;
            startPauseButton.sprite = playingButton;          
        }

        // This only shows the button to ClearTarget if we have a target. it would not make sense to have the button
        // show up if we had no target to clear
        if(targetBody != null)
        {
            clearTargetButton.SetActive(true);
        }
        else
        {
            clearTargetButton.SetActive(false);
        }

        // This draws the line to the target from the current observed body
        ConnectToTarget();

        // Function to add velocities when the increment buttons are held.
        ManageIncrementButtons();
    }

    /// <summary>
    /// Function to draw a line between the observed body and the target specified by the user
    /// </summary>
    public void ConnectToTarget()
    {
        if(targetBody!= null)
        {
            // The line renderer has a lot of helpful functions we can use and properties to set.
            // After doing that, it renders the line, however we have to keep updating it every frame as it
            // has to draw from scratch every time and it does not have a way of automatically updating
            // if one of the points moves

            // We have to first set the amount of points we want, then to set the points, and then the line renderer
            // can render the line we want. This component is also used in creating the trajectories of planets in conics
            LineRenderer rend;
            if (instantiatedObserver == null)
            {
                instantiatedObserver = GameObject.Instantiate(observer, observedBody.transform);
                rend = instantiatedObserver.gameObject.AddComponent<LineRenderer>();
                rend.positionCount = 2;
                rend.material = targetLineMaterial;
                rend.material.color = targetColor;
                rend.startWidth = 0.01f;
                rend.endWidth = 0.01f;

                Vector3 dir = targetBody.gameObject.transform.position - observedBody.gameObject.transform.position;
                Vector3 midPoint = targetBody.transform.position + (dir / 2);

                instantiatedMarker = GameObject.Instantiate(targetPrefab, midPoint, Quaternion.identity);
                instantiatedMarker.GetComponentInChildren<Canvas>().worldCamera = Camera.main.GetComponentInChildren<Camera>();
            }
            else
            {
                rend = instantiatedObserver.GetComponent<LineRenderer>();
                rend.SetPosition(0, observedBody.gameObject.transform.position);
                rend.SetPosition(1, targetBody.gameObject.transform.position);
                rend.enabled = true;
                rend.sortingOrder = -5;
                rend.sortingLayerName = "World UI";

                Vector3 dir = observedBody.gameObject.transform.position - targetBody.gameObject.transform.position;
                Vector3 midPoint = observedBody.transform.position - (dir / 2);
                instantiatedMarker.transform.position = midPoint;
                instantiatedMarker.transform.up = cameraController.cam.transform.up;

                CelestialBody body = observedBody as CelestialBody;
                CelestialBody target = targetBody as CelestialBody;

                var avgScale = (0.01f) / 2;

                instantiatedMarker.transform.localScale = new Vector3(-avgScale, avgScale, avgScale);
                float scaledDistance = dir.magnitude * distanceScaleFactor;

                //instantiated marker needs to be pointed up towards the z axis of the camera parent...
                //instantiatedMarker.transform.LookAt(cameraController.gameObject.transform, cameraController.gameObject.transform.up);

                TMP_Text dText = instantiatedMarker.GetComponentInChildren<TMP_Text>();
                dText.text = Math.Round((scaledDistance / 1000), 2) + "km";
            }
        }
    }

    

    public float increment = 0.0001f;
    /// <summary>
    /// Function to manage incrementing the speed of the observed body using each of the buttons. Depending on whether 
    /// we are using XYZ or "rocket science" components, it adds velocity on different vectors.
    /// </summary>
    public void ManageIncrementButtons()
    {
        if (isChangingXYZVelocity)
        {
            //Normal directions
            if (xUp.GetComponent<IncrementArrow>().isHeld)
            {
                observedBody.SetVelocity(new Vector3(observedBody.currentVelocity.x + increment, observedBody.currentVelocity.y, observedBody.currentVelocity.z));
            }
            if (xDown.GetComponent<IncrementArrow>().isHeld)
            {
                observedBody.SetVelocity(new Vector3(observedBody.currentVelocity.x - increment, observedBody.currentVelocity.y, observedBody.currentVelocity.z));
            }

            if (yUp.GetComponent<IncrementArrow>().isHeld)
            {
                observedBody.SetVelocity(new Vector3(observedBody.currentVelocity.x, observedBody.currentVelocity.y + increment, observedBody.currentVelocity.z));
            }
            if (yDown.GetComponent<IncrementArrow>().isHeld)
            {
                observedBody.SetVelocity(new Vector3(observedBody.currentVelocity.x, observedBody.currentVelocity.y - increment, observedBody.currentVelocity.z));
            }

            if (zUp.GetComponent<IncrementArrow>().isHeld)
            {
                observedBody.SetVelocity(new Vector3(observedBody.currentVelocity.x, observedBody.currentVelocity.y, observedBody.currentVelocity.z + increment));
            }
            if (zDown.GetComponent<IncrementArrow>().isHeld)
            {
                observedBody.SetVelocity(new Vector3(observedBody.currentVelocity.x, observedBody.currentVelocity.y, observedBody.currentVelocity.z - increment));
            }
        }
        else
        {
            //transform.right -> radial out
            //-transform.right -> radial in
            //transform.up -> normal
            //-transform.up -> antinormal
            //"Progrades"
            if (prograde.GetComponent<IncrementArrow>().isHeld)
            {
                observedBody.AddVelocity(increment, RelativeDirection.Prograde);
            }
            if (retrograde.GetComponent<IncrementArrow>().isHeld)
            {
                observedBody.AddVelocity(increment, RelativeDirection.Retrograde);
            }

            if (normal.GetComponent<IncrementArrow>().isHeld)
            {
                observedBody.AddVelocity(increment, RelativeDirection.Normal);
            }
            if (antinormal.GetComponent<IncrementArrow>().isHeld)
            {
                observedBody.AddVelocity(increment, RelativeDirection.Antinormal);
            }

            if (radial.GetComponent<IncrementArrow>().isHeld)
            {
                observedBody.AddVelocity(increment, RelativeDirection.Radial);
            }
            if (antiradial.GetComponent<IncrementArrow>().isHeld)
            {
                observedBody.AddVelocity(increment, RelativeDirection.Antiradial);
            }
        }
        
    }

    /// <summary>
    /// Function to set up the different buttons and each of their functions
    /// </summary>
    private void AddButtonListeners()
    {
        // This OnEndEdit.AddListener function is called whenever the user finishes editing the TMP_InputField by any method. The lambda 
        // Gets called, which triggers the if function and if the method by which the user finished editing the input field is pressing
        // a submit button, whatever that may be set to on the user's machine, we change the value in the script and call the ValidateEntryDouble function.
        // This is the same for everything else button-wise, where we are validating different things that the user can input. This is an important step
        // to take in order to ensure that the program doesn't crash when the user inputs erroneous data.
        bodyMass.onEndEdit.AddListener(fieldValue =>
        {
            if (Input.GetButton("Submit"))
            {
                Debug.Log(fieldValue);
                ValidateEntryDouble(fieldValue, "mass");
            }
        });

        incrementInput.onEndEdit.AddListener(fieldValue =>
        {
            if (Input.GetButton("Submit"))
            {
                float fOut;
                if (float.TryParse(fieldValue, out fOut))
                {
                    increment = float.Parse(fieldValue) / distanceScaleFactor;
                }
            }
        });

        radius.onEndEdit.AddListener(fieldValue =>
        {
            if (Input.GetButton("Submit"))
            {
                ValidateEntryDouble(fieldValue, "radius");
            }
        });

        bodyName.onEndEdit.AddListener(fieldValue =>
        {
            if (Input.GetButton("Submit"))
            {
                ValidateEntryString(fieldValue, "bodyName");
            }
        });

        velX.onEndEdit.AddListener(fieldValue =>
        {
            if (Input.GetButton("Submit"))
            {
                if(IsValidFloat(velX.text) && IsValidFloat(velY.text) && IsValidFloat(velZ.text))
                {
                    ValidateEntryVector3(new Vector3(float.Parse(velX.text), float.Parse(velY.text), float.Parse(velZ.text)));
                }
            }
        });

        velY.onEndEdit.AddListener(fieldValue =>
        {
            if (Input.GetButton("Submit"))
            {
                if (IsValidFloat(velX.text) && IsValidFloat(velY.text) && IsValidFloat(velZ.text))
                {
                    ValidateEntryVector3(new Vector3(float.Parse(velX.text), float.Parse(velY.text), float.Parse(velZ.text)));
                }
            }
        });

        velZ.onEndEdit.AddListener(fieldValue =>
        {
            if (Input.GetButton("Submit"))
            {
                if (IsValidFloat(velX.text) && IsValidFloat(velY.text) && IsValidFloat(velZ.text))
                {
                    ValidateEntryVector3(new Vector3(float.Parse(velX.text), float.Parse(velY.text), float.Parse(velZ.text)));
                }
            }
        });
    }

    /// <summary>
    /// This function spawns the target icon along with the text in the middle of the target line.
    /// </summary>
    /// <param name="bodyToTrack"></param>
    /// <param name="target"></param>
    public void SpawnTargetIcon(BaseBody bodyToTrack, bool target)
    {
        instantiatedMarker = Instantiate(targetPrefab, bodyToTrack.transform);
        if (target) 
        {
            instantiatedMarker.GetComponentInChildren<Image>().color = targetColor;
        }
        else
        {
            instantiatedMarker.GetComponentInChildren<Image>().color = targetColor;
        }
        instantiatedMarker.GetComponent<Canvas>().worldCamera = Camera.main.GetComponentInChildren<Camera>();
    }

    /// <summary>
    /// Functino to clear the target and clean it up, allowing us to select other targets if we wish
    /// </summary>
    public void ClearTarget()
    {
        targetBody = null;
        instantiatedObserver.GetComponent<LineRenderer>().enabled = false;
        Destroy(instantiatedObserver.gameObject);
        Destroy(instantiatedMarker.gameObject);
    }

    /// <summary>
    /// A validation function that allows us to set the velocity directly of the observed body
    /// </summary>
    /// <param name="vector"></param>
    private void ValidateEntryVector3(Vector3 vector)
    {
        observedBody.SetVelocity(vector / distanceScaleFactor);
    }

    /// <summary>
    /// Function to detect whether the inputted value is a valid float. We are using an object type here because we want to be able to check if any type we give it
    /// can be translated into a float, for example strings or integers or some doubles can all be translated into a float.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private bool IsValidFloat(object input)
    {
        float fOut;

        if(float.TryParse((string)input, out fOut))
        {
            return true;
        }
        double dOut;
        if(double.TryParse((string)input, out dOut))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// This is a function that validates a double value and automatically assigns it to the required field using a great C# feature, reflection.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="fieldName"></param>
    // It basically looks for a field name equal to the string you give it, and from then you can assign its value, etc.
    public void ValidateEntryDouble(object value, string fieldName)
    {
        if (!IsValidFloat(value))
        {
            return;
        }

        double newValue = (double.Parse((string)value));
        if(fieldName == "mass")
        {
            observedBody.GetType().GetField(fieldName).SetValue(observedBody, (float)(newValue / (double)massScaleFactor));
        }
        else if(fieldName == "radius")
        {
            observedBody.GetType().GetField(fieldName).SetValue(observedBody, (float)(newValue / (double)distanceScaleFactor));
        }
    }

    /// <summary>
    /// This doesn't exactly validate the input as it is a string and anything is acceptable, however we are trimming the string in 
    /// </summary>
    /// <param name="value"></param>
    /// <param name="fieldName"></param>
    // order to remove any empty spaces.
    public void ValidateEntryString(string value, string fieldName)
    {
        value.Trim();

        observedBody.GetType().GetField(fieldName).SetValue(observedBody, value);
    }

    // Code for managing the speed slider of the simulation.
    #region SpeedSlider
    /// <summary>
    /// Function to start or pause the simulation, toggling it on and off
    /// </summary>
    public void StartPauseButton()
    {
        sim.ToggleSimulation();
        ChangeSlider(sim.timeStep);
    }

    /// <summary>
    /// Preset function to change the simulation to a fast setting
    /// </summary>
    public void FastSim()
    {
        sim.FastSimulation();
        ChangeSlider(sim.timeStep);
    }

    /// <summary>
    /// This is the function that actually changes the time step in the simulation based on what the user has just 
    /// inputted in the slider
    /// </summary>
    public void SetValueForTimestepFromSlider()
    {
        sim.SlideValueChange(simSpeedSlider.value);
    }

    /// <summary>
    /// This is the function to manage changing the value of the slider of time step from a script without having the user interact
    /// with it. We might want this for example when resuming the simulation with the pause button to set it immediately to 
    /// a value and sync up the slider with that value
    /// </summary>
    /// <param name="val"></param>
    public void ChangeSlider(float val)
    {
        simSpeedSlider.value = val;
    }

    /// <summary>
    /// Function that gets called whenever we change the conic look ahead slider. This changes the conic time
    /// step which allows us to "see further into the future" in the conic trajectories
    /// </summary>
    public void ChangeConicLookaheadSlider()
    {
        sim.conicTimeStep = conicTimeStepSlider.value;
    }
    #endregion

    /// <summary>
    /// Function to toggle the properties window and animate it popping in and out
    /// </summary>
    public void TogglePropertiesWindow()
    {
        if (isPropertiesOut)
        {
            propertiesAnimator.SetTrigger("SlideOut");
            isPropertiesOut = false;
        }
        else
        {
            propertiesAnimator.SetTrigger("SlideIn");
            isPropertiesOut = true;
        }
    }

    /// <summary>
    /// Function for displaying the various properties we have in the properties panel.
    /// </summary>
    public void DisplayProperties()
    {
        if (observedBody == null) return;

        // The isFocused property on hte TMP_InputField is used to detect whether the user has clicked and is
        // currently editing that input field. This allows us to only update the text when it is not focusing as 
        // to not confuse the reader
        if(!bodyName.isFocused) bodyName.text = observedBody.bodyName;

        if(!bodyMass.isFocused) bodyMass.text = ((double)observedBody.mass * massScaleFactor).ToString();

        if(observedBody as CelestialBody)
        {
            CelestialBody celes = (CelestialBody)observedBody;
            if(!radius.isFocused) radius.text = ((double)celes.radius * distanceScaleFactor).ToString();
        }
        else
        {
            radius.text = "";
        }

        if (!incrementInput.isFocused) incrementInput.text = (increment * distanceScaleFactor).ToString();
        if (!velX.isFocused) velX.text = (observedBody.currentVelocity.x * distanceScaleFactor).ToString();
        if (!velY.isFocused) velY.text = (observedBody.currentVelocity.y * distanceScaleFactor).ToString();
        if (!velZ.isFocused) velZ.text = (observedBody.currentVelocity.z * distanceScaleFactor).ToString();

        velMagnitude.text = (observedBody.speedMagnitude * distanceScaleFactor / 1000).ToString() + "km/s";

        UpdateSOI();

        // This allows us to draw the orbit properties window on the screen
        if (sim.areConicsDrawn)
        {
            conicSOIText.text = "SOI: " + Math.Round(distanceScaleFactor * sim.FindSOIRadius(observedBody as CelestialBody) / 1000, 2) + " km";
            orbitProperties.SetActive(true);
            semiMajorAxisText.text = "a: " + Math.Round(sim.FindSemiMajorAxis(observedBody) * distanceScaleFactor / 1000, 2) + " km";
            periodText.text = "T: " + Math.Round(sim.FindPeriodForBody(observedBody), 2) + " s";
            perigeeText.text = "Per: " + Math.Round(observedBody.periapsisDistance * distanceScaleFactor / 1000, 2) + " km";
            apogeeText.text = "Apo: " + Math.Round(observedBody.apoapsisDistance * distanceScaleFactor / 1000, 2)  + " km";

            // Conic UI

            conicRelativeTo.gameObject.SetActive(true);
            conicRelativeToggle.gameObject.SetActive(true);
            conicTimeStepSlider.gameObject.SetActive(true);
            soiToggle.gameObject.SetActive(true);
            ChangeConicLookaheadSlider();

            conicRelativeTo.text = "Path relative to: " + sim.bodyRelativeTo.bodyName;
        }
        else
        {
            conicSOIText.text = "Turn on trajectories for more details";
            orbitProperties.SetActive(false);

            // Conic UI

            conicRelativeTo.gameObject.SetActive(false);
            conicRelativeToggle.gameObject.SetActive(false);
            conicTimeStepSlider.gameObject.SetActive(false);
            soiToggle.gameObject.SetActive(false);
        }

        if (largestInfluencer != null) largestInfluencer.text = "Largest influencer: " + observedBody.largestInfluencer.bodyName;
    }

    /// <summary>
    /// Cleans up the context menu when used when right clicking on a planet in the left hand side panel
    /// </summary>
    public void DeleteContext()
    {
        Destroy(spawnedContextMenu.gameObject);
    }

    #region Toggles
    /// <summary>
    /// Toggles the visualization of past positions of the bodies as blue trails behind them
    /// </summary>
    public void ToggleBodyTrails()
    {
        foreach (var item in GameObject.FindObjectsOfType<CelestialBody>())
        {
            item.gameObject.GetComponent<TrailRenderer>().enabled = !item.gameObject.GetComponent<TrailRenderer>().enabled;
        }
    }

    /// <summary>
    /// Toggles the creation of conics in the simulation script
    /// </summary>
    public void CreateConic()
    {
        sim.ToggleConics();
    }

    /// <summary>
    /// Toggles whether the conics are relative to a body or not
    /// </summary>
    public void ToggleConicRelative()
    {
        sim.conicRelative = !sim.conicRelative;
    }

    /// <summary>
    /// Toggles between the XYZ velocity system mentioned above and the "rocket science" veloity system. The "rocket science" system
    /// is the default because it makes more intuitive sense for the user to give more speed to the object forwards, etc.
    /// </summary>
    public void ToggleRelativeVelocity()
    {
        if (relativeVelocity.activeInHierarchy)
        {
            isChangingXYZVelocity = true;
            xyzVelocity.SetActive(true);
            relativeVelocity.SetActive(false);
            relativeVelButton.GetComponentInChildren<TMP_Text>().text = "Position Velocity";
        }
        else
        {
            isChangingXYZVelocity = false;
            xyzVelocity.SetActive(false);
            relativeVelocity.SetActive(true);
            relativeVelButton.GetComponentInChildren<TMP_Text>().text = "Relative Velocity";
        }
    }

    /// <summary>
    /// Toggles the visualization of the SOI of the object we are currently observing
    /// </summary>
    public void ToggleSOI()
    {
        if (instantiatedSOI.activeInHierarchy)
        {
            instantiatedSOI.SetActive(false);
        }
        else
        {
            instantiatedSOI.SetActive(true);
        }
    }

    /// <summary>
    /// This is a utility function that updates the scale and position of the SOI object so that it matches with the data displayed to the user.
    /// </summary>
    private void UpdateSOI()
    {
        if(instantiatedSOI != null)
        {
            float radius = sim.FindSOIRadius(observedBody);

            instantiatedSOI.transform.localScale = new Vector3(radius, radius, radius);
            instantiatedSOI.transform.position = observedBody.gameObject.transform.position;
        }
    }
    #endregion
}
