using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(TrailRenderer), typeof(LineRenderer))]
public class BaseBody : MonoBehaviour
{
    public Simulation sim;
    [SerializeField] public float mass;
    [SerializeField] public string bodyName;
    
    public BaseBody largestInfluencer;

    // Unity component which contains functions to set and get the position of an object
    [HideInInspector] public Rigidbody rb;

    public Vector3 currentVelocity;
    public TrailRenderer trail;

    public bool fake = false;

    [HideInInspector] public Vector3 addedVelocity;

    public List<BodyVec> orderedBodies = new List<BodyVec>();

    UIManager ui;

    public float speedMagnitude;

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

    // Update is called once per frame
    void Update()
    {
        
    }

    public void CalculateSpeedMagnitude()
    {
        float x = currentVelocity.x;
        float y = currentVelocity.y;
        float z = currentVelocity.z;

        speedMagnitude = Mathf.Sqrt(x * x + y * y + z * z);
    }

    // Function to calculate the velocity needed to be applied to the object per physics tick
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

    // Function to move the body to its correct position based on its current position and its velocity
    public void UpdatePosition(float timeStep)
    {
        if (!fake)
        {
            CalculateSpeedMagnitude();
            rb.MovePosition(rb.position + currentVelocity * timeStep);
        }
    }

    public void SetVelocity(Vector3 vel)
    {
        currentVelocity += vel - currentVelocity;
    }

    public void AddVelocity(float magnitude, RelativeDirection direction)
    {
        Vector3 dir = FindVectorFromRelativeDirection(direction);
        Vector3 combined = dir * magnitude;
        SetVelocity(combined + currentVelocity);
    }

    private Vector3 FindVectorFromRelativeDirection(RelativeDirection direction)
    {
        //transform.right -> radial out
        //-transform.right -> radial in
        //transform.up -> normal
        //-transform.up -> antinormal
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

public enum RelativeDirection
{
    Prograde,
    Retrograde,
    Normal,
    Antinormal,
    Radial,
    Antiradial
}
