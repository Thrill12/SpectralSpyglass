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

    public TMP_InputField velX;
    public TMP_InputField velY;
    public TMP_InputField velZ;
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

    public Sprite pausedButton;
    public Sprite playingButton;

    #endregion

    bool isPaused;

    Simulation sim;
    BaseBody observedBody;

    private void Start()
    {
        sim = FindObjectOfType<Simulation>();

        // Setting the default value of the slider.
        simSpeedSlider.value = sim.timeStep;

        AddButtonListeners();
    }

    private void Update()
    {
        observedBody = cameraController.currentTracking;

        DisplayProperties();

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
        if (cameraController.currentTracking == null) return;

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

        if (!velX.isFocused) velX.text = (observedBody.currentVelocity.x * velocityMultiplier ).ToString();
        if (!velY.isFocused) velY.text = (observedBody.currentVelocity.y * velocityMultiplier ).ToString();
        if (!velZ.isFocused) velZ.text = (observedBody.currentVelocity.z * velocityMultiplier ).ToString();

        largestInfluencer.text = "Largest influencer: " + observedBody.largestInfluencer.bodyName;
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
    #endregion
}
