using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class CelestialBody : BaseBody
{
    public float radius;
    public Vector3 initialVelocity;  
    
    private void Awake()
    {       
        currentVelocity = initialVelocity;
        
        transform.localScale = new Vector3(radius, radius, radius);     
    }
}

// A small class used for the ordered list of bodies. Made this
// so that we could easily sort the bodies by their force, but might 
// end up changing this.
[System.Serializable]
public class BodyVec
{
    public BaseBody body;
    [HideInInspector] public Vector3 forceVec;
    public float magnitude;

    public BodyVec(BaseBody body, Vector3 forceVec)
    {
        this.body = body;
        this.forceVec = forceVec;
        magnitude = forceVec.sqrMagnitude;
    }
}
