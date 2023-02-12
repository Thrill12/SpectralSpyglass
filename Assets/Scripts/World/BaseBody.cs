using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// The Base class for objects in the system. This needs to be here because we need common functions for all of the bodies in the system.
/// </summary>
// The require component here simply adds the missing components if the object doesn't have them, as they are all needed for the simulation.
[RequireComponent(typeof(Rigidbody), typeof(TrailRenderer), typeof(LineRenderer))]
public class BaseBody : MonoBehaviour
{
    // We need a reference of the simulation script in order to access the gravitation constant and any other helpful utility functions we may have in the future
    public Simulation sim;
    [SerializeField] public float mass;
    [SerializeField] public string bodyName;
    
    // The body, other than itself, that applies the most force to this object
    public BaseBody largestInfluencer;

    // Unity component which contains functions to set and get the position of an object
    [HideInInspector] public Rigidbody rb;
    // Variable to hold the calculated current velocity of the planet - this gets changed whenever it gets updated going over all of the other planets in the system
    public Vector3 currentVelocity;
    // Variable to hold the past trail of the object.
    public TrailRenderer trail;

    // This is set as fake whenever we don't want a body to affect or be affected by the simulation at all
    public bool fake = false;

    // Variable to easily calculate the largest influencer, which is the body that applies the most force on the body
    public List<BodyVec> orderedBodies = new List<BodyVec>();

    UIManager ui;

    // Small variables used to hold details about its orbit, speed etc.
    public float speedMagnitude;
    public float periapsisDistance;
    public float apoapsisDistance;
    public Vector3 pointOfOrbitCentre;
    public Vector3 furthestPointFromCentre;

    // Start is called before the first frame update
    void Start()
    {
        ui = GameObject.FindObjectOfType<UIManager>();
        sim = GameObject.FindObjectOfType<Simulation>();
        rb = GetComponent<Rigidbody>();
        trail = GetComponent<TrailRenderer>();

        // Some small visual improvements over the base trails. This
        // allows each trail to be smaller than the body we are observing and
        // not obstruct the user's vision too much.
        trail.startColor = new Color(255, 255, 255, 255);
        trail.endColor = new Color(255, 255, 255, 0);
        trail.startWidth = transform.localScale.x / 4;
        trail.endWidth = 0;
    }

    /// <summary>
    /// Simple formula to find the resulting magnitude of velocity from its 3D vector
    /// </summary>
    public void CalculateSpeedMagnitude()
    {
        float x = currentVelocity.x;
        float y = currentVelocity.y;
        float z = currentVelocity.z;

        speedMagnitude = Mathf.Sqrt(x * x + y * y + z * z);
    }

    /// <summary>
    /// Function to calculate the velocity needed to be applied to the object per physics tick.
    /// This uses Newton's Gravitational Formula, F = -(GMm)/(r^2)
    /// </summary>
    /// <param name="bodies"></param>
    /// <param name="timeStep"></param>
    public void UpdateVelocity(List<BaseBody> bodies, float timeStep)
    {
        if (fake) return;

        transform.localRotation = Quaternion.LookRotation(currentVelocity);
        

        orderedBodies.Clear();
        foreach (var item in bodies)
        {
            if (item != this)
            {
                if (item.fake) continue;
                // Newton's Gravitational Formula
                // F = G((m1*m2)/r^2)
                // F = force
                // G = gravConstant
                // m1 = mass
                // m2 = item.mass
                // r = sqrDst
                float sqrDst = (item.rb.position - rb.position).sqrMagnitude;
                Vector3 forceDir = (item.rb.position - rb.position).normalized;
                Vector3 force = forceDir * ((sim.gravConstant * mass * item.mass) / sqrDst);

                LogWriter.instance.Log("|" + DateTime.Now.ToString("HH:mm:ss") +"|" + bodyName + "||" + sim.gravConstant + " * (" + mass + " * " + item.mass + ") / " + sqrDst + " = " + force.magnitude + " for " + item.bodyName);

                Vector3 acceleration = force / mass;
                currentVelocity += acceleration * timeStep;

                BodyVec vecBody = new BodyVec(item, acceleration, this);

                // Adding the bodies into a list in order to determine the largest influencer to simplify some equations.
                // A small planet on the other side of the system will not really affect the sun or a much bigger planet on the other side of the solar system
                orderedBodies.Add(vecBody);
                orderedBodies = orderedBodies.OrderByDescending(x => x.magnitude).ToList();
            }
        }

        // Currently not ordering the list for some reason - need more research.
        // Current suspicion is that the inspector somehow intervenes and stops it from properly sorting
        // however not sure.
        largestInfluencer = orderedBodies[0].body;
    }

    /// <summary>
    /// Function to move the body to its correct position based on its current position and its velocity
    /// </summary>
    /// <param name="timeStep"></param>
    public void UpdatePosition(float timeStep)
    {
        if (!fake)
        {
            CalculateSpeedMagnitude();
            rb.MovePosition(rb.position + currentVelocity * timeStep);
        }
    }

    /// <summary>
    /// This is the function that gets used when adding velocity from the increment buttons
    /// </summary>
    /// <param name="vel"></param>
    public void SetVelocity(Vector3 vel)
    {
        currentVelocity += vel - currentVelocity;
    }

    /// <summary>
    /// This adds the specified amount of speed in a specified direction - used in the "rocket science" components of adding velocity
    /// </summary>
    /// <param name="magnitude"></param>
    /// <param name="direction"></param>
    public void AddVelocity(float magnitude, RelativeDirection direction)
    {
        Vector3 dir = FindVectorFromRelativeDirection(direction);
        Vector3 combined = dir * magnitude;
        SetVelocity(combined + currentVelocity);
    }

    /// <summary>
    /// Returns a Vector3 from our RelativeDirection enum, allowing us where in "rocket science" terms to apply the velocity
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    private Vector3 FindVectorFromRelativeDirection(RelativeDirection direction)
    {
        //transform.right -> radial out
        //-transform.right -> radial in
        //transform.up -> normal
        //-transform.up -> antinormal
        //"Progrades"
        switch (direction)
        {
            case RelativeDirection.Prograde:
                return currentVelocity.normalized;
            case RelativeDirection.Retrograde:
                return -currentVelocity.normalized;
            case RelativeDirection.Normal:
                return transform.up;
            case RelativeDirection.Antinormal:
                return -transform.up;
            case RelativeDirection.Radial:
                return -transform.right;
            case RelativeDirection.Antiradial:
                return transform.right;
        }
        return Vector3.zero;
    }
}

/// <summary>
/// The enum used for holding the different "rocket science" velocity components
/// </summary>
public enum RelativeDirection
{
    Prograde,
    Retrograde,
    Normal,
    Antinormal,
    Radial,
    Antiradial
}
