using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VirtualBody
{
    public Vector3 position;
    public Vector3 velocity;
    public float mass;
    public VirtualBody(BaseBody body)
    {
        position = body.transform.position;
        velocity = body.currentVelocity;
        mass = body.mass;
    }
}
