using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CelestialBody : MonoBehaviour
{
    public string bodyName;
    [HideInInspector] public const float gravConstant = 0.0001f;
    public float radius;
    public float mass;
    public Vector3 initialVelocity;

    public CelestialBody largestInfluencer;

    // Unity component which contains functions to set and get the position of an object
    [HideInInspector] public Rigidbody rb;

    [HideInInspector] public Vector3 currentVelocity;

    TrailRenderer trail;

    public List<BodyVec> orderedBodies = new List<BodyVec>();

    private void Awake()
    {
        trail = GetComponent<TrailRenderer>();
        currentVelocity = initialVelocity;
        rb = GetComponent<Rigidbody>();
        transform.localScale = new Vector3(radius, radius, radius);

        // Some small visual improvements over the base trails. This
        // allows each trail to be smaller than the body we are observing and
        // not obstruct the user's vision too much.
        trail.startColor = new Color(255, 255, 255, 255);
        trail.endColor = new Color(255, 255, 255, 0);
        trail.startWidth = radius / 2;
        trail.endWidth = 0;
    }

    // Function to calculate the velocity needed to be applied to the object per physics tick
    public void UpdateVelocity(CelestialBody[] bodies, float timeStep)
    {
        orderedBodies.Clear();
        foreach (var item in bodies)
        {
            if (item != this)
            {
                // Newton's Gravitational Formula
                // F = G((m1*m2)/r^2)
                // F = force
                // G = gravConstant
                // m1 = mass
                // m2 = item.mass
                // r = sqrDst
                float sqrDst = (item.rb.position - rb.position).sqrMagnitude;
                Vector3 forceDir = (item.rb.position - rb.position).normalized;
                Vector3 force = forceDir * gravConstant * mass * item.mass / sqrDst;
                Vector3 acceleration = force / mass;
                currentVelocity += acceleration * timeStep;

                // Adding the bodies into a list in order to determine the largest influencer to simplify some equations.
                // A small planet on the other side of the system will not really affect the sun or a much bigger planet on the other side of the solar system
                orderedBodies.Add(new BodyVec(item, force));
            }         
        }

        // Currently not ordering the list for some reason - need more research.
        // Current suspicions is that the inspector somehow intervenes and stops it from properly sorting
        // however not sure.
        orderedBodies.OrderByDescending(x => x.forceVec.sqrMagnitude).ToList();
        largestInfluencer = orderedBodies[0].body;     
    }

    // Function to move the body to its correct position based on its current position and its velocity
    public void UpdatePosition(float timeStep)
    {
        rb.MovePosition(rb.position + currentVelocity * timeStep);
    }
}

// A small class used for the ordered list of bodies. Made this
// so that we could easily sort the bodies by their force, but might 
// end up changing this.
[System.Serializable]
public class BodyVec
{
    public CelestialBody body;
    [HideInInspector] public Vector3 forceVec;
    public float magnitude;

    public BodyVec(CelestialBody body, Vector3 forceVec)
    {
        this.body = body;
        this.forceVec = forceVec;
        magnitude = forceVec.sqrMagnitude;
    }
}
