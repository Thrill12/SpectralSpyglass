using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    string oldName;
    string oldMass;
    string oldRadius;
    string oldVelx;
    string oldVely;
    string oldVelz;

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
    }

    private void Update()
    {
        observedBody = cameraController.currentTracking;

        if (sim.timeStep != 0)
        {
            isPaused = false;
            startPauseButton.sprite = pausedButton;

            DisplayProperties();
        }
        else
        {
            isPaused = true;
            startPauseButton.sprite = playingButton;          
        }

        if (isDirty())
        {
            SetProperties();
        }
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

    // Need to replace this with a function to decide which elements to spawn in the properties window based on which type of body
    // the object is

    // Here we are displaying all the properties in the window.
    public void DisplayProperties()
    {
        if (cameraController.currentTracking == null) return;

        bodyName.text = observedBody.bodyName;
        bodyMass.text = (observedBody.mass * massMultiplier).ToString();

        if(observedBody as CelestialBody)
        {
            CelestialBody celes = (CelestialBody)observedBody;
            radius.text = (celes.radius * radiusMultiplier).ToString();
        }
        else
        {
            radius.text = "";
        }

        velX.text = (observedBody.currentVelocity.x * velocityMultiplier ).ToString();
        velY.text = (observedBody.currentVelocity.y * velocityMultiplier ).ToString();
        velZ.text = (observedBody.currentVelocity.z * velocityMultiplier ).ToString();

        largestInfluencer.text = "Largest influencer: " + observedBody.largestInfluencer.bodyName;
    }

    public bool isDirty()
    {
        if(bodyName.text != oldName)
        {
            oldName = bodyName.text;
            return true;
        }

        if(bodyMass.text != oldMass)
        {
            oldMass = bodyMass.text;
            return true;
        }

        if (radius.text != oldRadius)
        {
            oldRadius = radius.text;
            return true;
        }

        if(velX.text != oldVelx)
        {
            oldVelx = velX.text;
            return true;
        }

        if (velY.text != oldVely)
        {
            oldVely = velY.text;
            return true;
        }

        if (velZ.text != oldVelz)
        {
            oldVelz = velZ.text;
            return true;
        }

        return false;
    }

    public void SetProperties()
    {
        observedBody.bodyName = bodyName.text;
        observedBody.mass = (float)Convert.ToDouble(bodyMass.text) / massMultiplier;
        
        if(observedBody as CelestialBody)
        {
            CelestialBody c = (CelestialBody)observedBody;
            c.radius = (float)Convert.ToDouble(radius.text) / radiusMultiplier;
        }

        observedBody.AddVelocity( new Vector3((float)Convert.ToDouble(velX.text), (float)Convert.ToDouble(velY), (float)Convert.ToDouble(velZ)));
    }

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
