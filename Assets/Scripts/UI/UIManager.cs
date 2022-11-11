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

    public float radiusMultiplier;
    public float massMultiplier;
    public float velocityMultiplier;

    [Space(5)]

    [Header("Properties text")]

    public Animator propertiesAnimator;
    public bool isPropertiesOut = true;

    public TMP_InputField bodyName;
    public TMP_InputField bodyMass;
    public TMP_InputField radius;

    public Button xUp, xDown, yUp, yDown, zUp, zDown;
    public TMP_InputField incrementInput;
    public TMP_InputField velX;
    public TMP_InputField velY;
    public TMP_InputField velZ;
    public TMP_Text velMagnitude;
    public TMP_Text largestInfluencer;

    //string oldName;
    //string oldMass;
    //string oldRadius;
    //string oldVelx;
    //string oldVely;
    //string oldVelz;

    #region Slider

    [Space(5)]

    [Header("Speed Slider")]

    public Image startPauseButton;
    public Slider simSpeedSlider;
    public Slider conicTimeStepSlider;

    public Sprite pausedButton;
    public Sprite playingButton;

    #endregion

    bool isPaused;

    Simulation sim;
    public BaseBody observedBody;

    public GameObject planetButtonContent;
    public GameObject planetButtonPrefab;

    public TMP_Text camSpeedText;

    public GameObject contextMenuPrefab;
    [HideInInspector] public GameObject spawnedContextMenu;

    public TMP_Text fpsCounter;

    private void Start()
    {
        sim = FindObjectOfType<Simulation>();

        // Setting the default value of the slider.
        simSpeedSlider.value = sim.timeStep;

        AddButtonListeners();
        SetPlanetButtons();
    }

    public void SetPlanetButtons()
    {
        for (int i = 0; i < sim.bodies.Length; i++)
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

        ManageIncrementButtons();
    }

    public float increment = 0.0001f;
    public void ManageIncrementButtons()
    {
        if (xUp.GetComponent<IncrementArrow>().isHeld)
        {
            observedBody.AddVelocity(new Vector3(observedBody.currentVelocity.x + increment, observedBody.currentVelocity.y, observedBody.currentVelocity.z));
        }
        if (xDown.GetComponent<IncrementArrow>().isHeld)
        {
            observedBody.AddVelocity(new Vector3(observedBody.currentVelocity.x - increment, observedBody.currentVelocity.y, observedBody.currentVelocity.z));
        }

        if (yUp.GetComponent<IncrementArrow>().isHeld)
        {
            observedBody.AddVelocity(new Vector3(observedBody.currentVelocity.x, observedBody.currentVelocity.y + increment, observedBody.currentVelocity.z));
        }
        if (yDown.GetComponent<IncrementArrow>().isHeld)
        {
            observedBody.AddVelocity(new Vector3(observedBody.currentVelocity.x, observedBody.currentVelocity.y - increment, observedBody.currentVelocity.z));
        }

        if (zUp.GetComponent<IncrementArrow>().isHeld)
        {
            observedBody.AddVelocity(new Vector3(observedBody.currentVelocity.x, observedBody.currentVelocity.y, observedBody.currentVelocity.z + increment));
        }
        if (zDown.GetComponent<IncrementArrow>().isHeld)
        {
            observedBody.AddVelocity(new Vector3(observedBody.currentVelocity.x, observedBody.currentVelocity.y, observedBody.currentVelocity.z - increment));
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
                    increment = float.Parse(fieldValue) / velocityMultiplier;
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

    private void ValidateEntryVector3(Vector3 vector)
    {
        observedBody.AddVelocity(vector / velocityMultiplier);
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
        Debug.Log("New value is " + newValue);
        if(fieldName == "mass")
        {
            Debug.Log("Converted is " + newValue / massMultiplier);
            observedBody.GetType().GetField(fieldName).SetValue(observedBody, (float)(newValue / (double)massMultiplier));
        }
        else if(fieldName == "radius")
        {
            observedBody.GetType().GetField(fieldName).SetValue(observedBody, (float)(newValue / (double)radiusMultiplier));
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

        if(!bodyMass.isFocused) bodyMass.text = ((double)observedBody.mass * massMultiplier).ToString();

        if(observedBody as CelestialBody)
        {
            CelestialBody celes = (CelestialBody)observedBody;
            if(!radius.isFocused) radius.text = ((double)celes.radius * radiusMultiplier).ToString();
        }
        else
        {
            radius.text = "";
        }

        if (!incrementInput.isFocused) incrementInput.text = (increment * velocityMultiplier).ToString();
        if (!velX.isFocused) velX.text = (observedBody.currentVelocity.x * velocityMultiplier ).ToString();
        if (!velY.isFocused) velY.text = (observedBody.currentVelocity.y * velocityMultiplier ).ToString();
        if (!velZ.isFocused) velZ.text = (observedBody.currentVelocity.z * velocityMultiplier ).ToString();

        velMagnitude.text = (observedBody.speedMagnitude * velocityMultiplier).ToString() + "m/s";

        largestInfluencer.text = "Largest influencer: " + observedBody.largestInfluencer.bodyName;
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
    #endregion
}
