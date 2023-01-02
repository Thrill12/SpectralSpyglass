using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class for calculating the future trajectories of bodies in the system. Holds simple variables such as mass, position, velocity, name.
/// </summary>
public class VirtualBody
{
    public string bodyName;
    public Vector3 position;
    public Vector3 velocity;
    public float mass;
    // Constructor in order to be able to easily create a new virtual body based on a real body. We don't need all of the body's variables, only the essential ones for our
    // calculations.
    public VirtualBody(BaseBody body)
    {
        this.bodyName = body.bodyName;
        position = body.transform.position;
        velocity = body.currentVelocity;
        mass = body.mass;
    }
}
