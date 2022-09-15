using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Simulation : MonoBehaviour
{
    [HideInInspector]
    public CelestialBody[] bodies;
    public float timeStep;

    float lastSimulation;

    private void Awake()
    {
        bodies = FindObjectsOfType<CelestialBody>();
        NormalSimulation();
    }

    // Unity function which runs at around 60 times per second in time with the physics update
    // Here, we calculate the velocities and the positions of the objects.
    // We do it here so we don't have to do it an unnecessary amount of times in the normal
    // Update function.
    private void FixedUpdate()
    {
        for (int i = 0; i < bodies.Length; i++)
        {
            bodies[i].UpdateVelocity(bodies, timeStep);
        }

        for (int i = 0; i < bodies.Length; i++)
        {
            bodies[i].UpdatePosition(timeStep);
        }
    }

    // Function to enable or disable the simulation, used in the pause/play button
    public void ToggleSimulation()
    {
        if(timeStep != 0)
        {
            lastSimulation = timeStep;
            timeStep = 0;
        }
        else
        {
            timeStep = lastSimulation;
            lastSimulation = 0;
        }    
    }

    // Function to default to a normal simulation
    private void NormalSimulation()
    {
        timeStep = 0.001f;
    }

    // Function to default to a fast simulation
    public void FastSimulation()
    {
        timeStep = 0.5f;
    }

    // Function that gets called when the slider for the speed gets changed
    public void SlideValueChange(float newValue)
    {
        timeStep = newValue;
    }
}
