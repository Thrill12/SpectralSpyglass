using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


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

// A small class used for the ordered list of bodies. Made this
// so that we could easily sort the bodies by their force, but might 
// end up changing this.
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
