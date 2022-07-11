using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Simulation : MonoBehaviour
{
    [HideInInspector]
    public CelestialBody[] bodies;
    public float timeStep;

    private void Awake()
    {
        bodies = FindObjectsOfType<CelestialBody>();
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
}
