using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

public class UIManager : MonoBehaviour
{
    public CameraController cameraController;

    [Header("Scale Multipliers")]

    public float distanceScaleFactor;
    public float massScaleFactor;

    [Space(5)]

    [Header("Editable properties")]

    public Animator propertiesAnimator;
    public bool isPropertiesOut = true;

    public TMP_InputField bodyName;
    public TMP_InputField bodyMass;
    public TMP_InputField radius;

    public Button xUp, xDown, yUp, yDown, zUp, zDown;
    public Button prograde, retrograde, radial, antiradial, normal, antinormal;
    public Button relativeVelButton;
    public TMP_InputField incrementInput;
    public TMP_InputField velX;
    public TMP_InputField velY;
    public TMP_InputField velZ;
    public TMP_Text velMagnitude;
    public TMP_Text largestInfluencer;    

    [Space(5)]

    [Header("Orbital Properties")]

    public GameObject orbitProperties;
    public TMP_Text semiMajorAxisText;
    public TMP_Text periodText;
    public TMP_Text perigeeText;
    public TMP_Text apogeeText;

    public bool isChangingXYZVelocity;

    public GameObject xyzVelocity;
    public GameObject relativeVelocity;

    public GameObject soiToggle;
    public GameObject soiPrefab;
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

    public Image startPauseButton;
    public Slider simSpeedSlider;
    public Sprite pausedButton;
    public Sprite playingButton;

    [Space(5)]

    [Header("Conics")]

    public Slider conicTimeStepSlider;
    public TMP_Text conicRelativeTo;
    public TMP_Text conicSOIText;
    public GameObject conicRelativeToggle;

    #endregion

    [Space(5)]

    [Header("Body UI")]
  
    public BaseBody observedBody;
    public BaseBody targetBody;

    public GameObject planetButtonContent;
    public GameObject planetButtonPrefab;

    public TMP_Text camSpeedText;

    public GameObject contextMenuPrefab;
    [HideInInspector] public GameObject spawnedContextMenu;

    public TMP_Text fpsCounter;

    [Space(5)]

    [Header("In-World UI")]

    public Color selectedColor;
    public Color targetColor;
    public Material targetLineMaterial;
    public GameObject targetPrefab;
    public GameObject instantiatedMarker;
    public GameObject clearTargetButton;

    [Space(5)]

    [Header("GameObjects")]

    public GameObject observer;
    public GameObject instantiatedObserver;

    Simulation sim;
    bool isPaused;
    private void Start()
    {
        sim = FindObjectOfType<Simulation>();

        // Setting the default value of the slider.
        simSpeedSlider.value = sim.timeStep;

        AddButtonListeners();
        //SetPlanetButtons();

        if (instantiatedSOI == null)
        {
            instantiatedSOI = GameObject.Instantiate(soiPrefab, Vector3.zero, Quaternion.identity);
        }

        ToggleSOI();

        sim.onChangePlanets += SetPlanetButtons;
    }

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

    private void Update()
    {
        DisplayProperties();

        fpsCounter.text = ((int)(1 / Time.smoothDeltaTime)).ToString();

        if (cameraController.freeCam)
        {
            camSpeedText.enabled = true;
            camSpeedText.text = Mathf.RoundToInt(cameraController.moveSpeed * 10000) + "x";
        }
        else
        {
            camSpeedText.enabled = false;
        }

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

        if(targetBody != null)
        {
            clearTargetButton.SetActive(true);
        }
        else
        {
            clearTargetButton.SetActive(false);
        }
        ConnectToTarget();
        ManageIncrementButtons();
    }

    public void ConnectToTarget()
    {
        if(targetBody!= null)
        {
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

    private void AddButtonListeners()
    {
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

    public void ClearTarget()
    {
        targetBody = null;
        instantiatedObserver.GetComponent<LineRenderer>().enabled = false;
        Destroy(instantiatedObserver.gameObject);
        Destroy(instantiatedMarker.gameObject);
    }

    private void ValidateEntryVector3(Vector3 vector)
    {
        observedBody.SetVelocity(vector / distanceScaleFactor);
    }

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

    public void ValidateEntryString(string value, string fieldName)
    {
        value.Trim();

        observedBody.GetType().GetField(fieldName).SetValue(observedBody, value);
    }

    // Code for managing the speed slider of the simulation.
    #region SpeedSlider
    public void StartPauseButton()
    {
        sim.ToggleSimulation();
        ChangeSlider(sim.timeStep);
    }

    public void FastSim()
    {
        sim.FastSimulation();
        ChangeSlider(sim.timeStep);
    }

    public void SetValueForTimestepFromSlider()
    {
        sim.SlideValueChange(simSpeedSlider.value);
    }

    public void ChangeSlider(float val)
    {
        simSpeedSlider.value = val;
    }

    public void ChangeConicLookaheadSlider()
    {
        sim.conicTimeStep = conicTimeStepSlider.value;
    }
    #endregion

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

    // Here we are displaying all the properties in the window.
    public void DisplayProperties()
    {
        if (observedBody == null) return;

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

    public void DeleteContext()
    {
        Destroy(spawnedContextMenu.gameObject);
    }

    //public bool isDirty()
    //{
    //    if(bodyName.text != oldName)
    //    {
    //        oldName = bodyName.text;
    //        return true;
    //    }

    //    if(bodyMass.text != oldMass)
    //    {
    //        oldMass = bodyMass.text;
    //        return true;
    //    }

    //    if (radius.text != oldRadius)
    //    {
    //        oldRadius = radius.text;
    //        return true;
    //    }

    //    if(velX.text != oldVelx)
    //    {
    //        oldVelx = velX.text;
    //        return true;
    //    }

    //    if (velY.text != oldVely)
    //    {
    //        oldVely = velY.text;
    //        return true;
    //    }

    //    if (velZ.text != oldVelz)
    //    {
    //        oldVelz = velZ.text;
    //        return true;
    //    }

    //    return false;
    //}

    //public void SetProperties()
    //{
    //    observedBody.bodyName = bodyName.text;
    //    observedBody.mass = (float)Convert.ToDouble(bodyMass.text) / massMultiplier;
        
    //    if(observedBody as CelestialBody)
    //    {
    //        CelestialBody c = (CelestialBody)observedBody;
    //        c.radius = (float)Convert.ToDouble(radius.text) / radiusMultiplier;
    //    }

    //    observedBody.AddVelocity( new Vector3((float)Convert.ToDouble(velX.text), (float)Convert.ToDouble(velY), (float)Convert.ToDouble(velZ)));
    //}

    // Region for the different toggles we will have in the tool.
    #region Toggles
    public void ToggleBodyTrails()
    {
        foreach (var item in GameObject.FindObjectsOfType<CelestialBody>())
        {
            item.gameObject.GetComponent<TrailRenderer>().enabled = !item.gameObject.GetComponent<TrailRenderer>().enabled;
        }
    }

    public void CreateConic()
    {
        sim.ToggleConics();
    }

    public void ToggleConicRelative()
    {
        sim.conicRelative = !sim.conicRelative;
    }

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
