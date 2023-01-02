using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


/// <summary>
/// Subclass of the BaseBody class, it is used for large celestial bodies that are spherical in nature, due to their radius.
/// I split up the radius into this different class in case I wanted some non spherical objects in the simulation, such as a space station, however
/// that is out of the scope of my current coursework project.
/// </summary>
public class CelestialBody : BaseBody
{
    [SerializeField] public float radius;
    [SerializeField] public Vector3 initialVelocity;  
    
    private void Awake()
    {       
        currentVelocity = initialVelocity; 
    }

    private void FixedUpdate()
    {
        transform.localScale = new Vector3(radius, radius, radius);
        trail.startWidth = radius / 4;
    }
}

/// <summary>
/// A small class used for the ordered list of bodies. Made this
/// so that we could easily sort the bodies by their force.
/// </summary>
[System.Serializable]
public struct BodyVec
{
    public BaseBody body;
    [HideInInspector] public Vector3 forceVec;
    public float magnitude;
    public float distance;

    public BodyVec(BaseBody body, Vector3 forceVec, BaseBody otherBody)
    {
        this.body = body;
        this.forceVec = forceVec;
        magnitude = forceVec.sqrMagnitude;
        distance = Vector3.Distance(this.body.transform.position, otherBody.transform.position);
    }
}
