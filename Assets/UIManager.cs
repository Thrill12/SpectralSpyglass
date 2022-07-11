using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public Image startPauseButton;
    public Slider simSpeedSlider;

    [Space(5)]

    public Sprite pausedButton;
    public Sprite playingButton;

    bool isPaused;

    Simulation sim;

    private void Start()
    {
        sim = FindObjectOfType<Simulation>();
    }

    private void Update()
    {
        if(sim.timeStep != 0)
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
}
