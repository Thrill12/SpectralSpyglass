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

    private void NormalSimulation()
    {
        timeStep = 0.01f;
    }

    public void FastSimulation()
    {
        timeStep = 0.5f;
    }

    public void SlideValueChange(float newValue)
    {
        timeStep = newValue;
    }
}
