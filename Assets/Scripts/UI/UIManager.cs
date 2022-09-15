using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public CameraController cameraController;

    [Header("Properties text")]

    public Animator propertiesAnimator;
    public bool isPropertiesOut = true;

    public TMP_Text bodyName;
    public TMP_Text bodyMass;
    public TMP_Text radius;
    public TMP_Text velX;
    public TMP_Text velY;
    public TMP_Text velZ;
    public TMP_Text largestInfluencer;

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

    private void Start()
    {
        sim = FindObjectOfType<Simulation>();

        // Setting the default value of the slider.
        simSpeedSlider.value = sim.timeStep;
    }

    private void Update()
    {
        if(sim.timeStep != 0)
        {
            isPaused = false;
            startPauseButton.sprite = pausedButton;

            if (!isPropertiesOut)
            {
                DisplayProperties();
            }
        }
        else
        {
            isPaused = true;
            startPauseButton.sprite = playingButton;          
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

    // Here we are displaying all the properties in the window.
    public void DisplayProperties()
    {
        if (cameraController.currentTracking == null) return;

        CelestialBody observedBody = cameraController.currentTracking;
        bodyName.text = observedBody.bodyName;
        bodyMass.text = "Mass: " + observedBody.mass.ToString();
        radius.text = "Radius: " + observedBody.radius.ToString();
        velX.text = "X: " + observedBody.currentVelocity.x.ToString();
        velY.text = "Y: " + observedBody.currentVelocity.y.ToString();
        velZ.text = "Z: " + observedBody.currentVelocity.z.ToString();
        largestInfluencer.text = "Largest influencer: " + observedBody.largestInfluencer.bodyName;
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
    #endregion
}
