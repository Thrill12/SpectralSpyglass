using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(TrailRenderer), typeof(LineRenderer))]
public class BaseBody : MonoBehaviour
{
    public Simulation sim;
    public float mass;
    public string bodyName;
    
    public BaseBody largestInfluencer;

    // Unity component which contains functions to set and get the position of an object
    [HideInInspector] public Rigidbody rb;

    [HideInInspector] public Vector3 currentVelocity;
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
    public void UpdateVelocity(BaseBody[] bodies, float timeStep)
    {
        orderedBodies.Clear();
        foreach (var item in bodies)
        {
            if (item != this && !item.fake)
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
                Vector3 force = (forceDir * sim.gravConstant * mass * item.mass / sqrDst);
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
        CalculateSpeedMagnitude();
        rb.MovePosition(rb.position + currentVelocity * timeStep);
    }

    public void AddVelocity(Vector3 vel)
    {
        currentVelocity += vel - currentVelocity;
    }
}
