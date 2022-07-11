using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CelestialBody : MonoBehaviour
{
    [HideInInspector] public const float gravConstant = 0.0001f;
    public float radius;
    public float mass;
    public Vector3 initialVelocity;

    [HideInInspector] public Rigidbody rb;

    Vector3 currentVelocity;

    private void Awake()
    {
        currentVelocity = initialVelocity;
        rb = GetComponent<Rigidbody>();
        transform.localScale = new Vector3(radius, radius, radius);
    }

    public void UpdateVelocity(CelestialBody[] bodies, float timeStep)
    {
        foreach (var item in bodies)
        {
            if (item != this)
            {
                float sqrDst = (item.rb.position - rb.position).sqrMagnitude;
                Vector3 forceDir = (item.rb.position - rb.position).normalized;
                Vector3 force = forceDir * gravConstant * mass * item.mass / sqrDst;
                Vector3 acceleration = force / mass;
                currentVelocity += acceleration * timeStep;
            }         
        }
    }

    public void UpdatePosition(float timeStep)
    {
        rb.MovePosition(rb.position + currentVelocity * timeStep);
    }
}
