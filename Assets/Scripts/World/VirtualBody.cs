using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VirtualBody
{
    public string bodyName;
    public Vector3 position;
    public Vector3 velocity;
    public float mass;
    public VirtualBody(BaseBody body)
    {
        this.bodyName = body.bodyName;
        position = body.transform.position;
        velocity = body.currentVelocity;
        mass = body.mass;
    }
}
