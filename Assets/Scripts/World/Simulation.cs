using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using System.Threading;
using System;

public class Simulation : MonoBehaviour
{
    public float timeBetweenConicCalculations;
    [HideInInspector] public float gravConstant = 0.0001f;
    public Material conicMaterial;
    [HideInInspector]
    public List<BaseBody> bodies = new List<BaseBody>();
    public float timeStep;
    public float conicTimeStep = 1;
    public int conicLookahead;
    public bool conicRelative = true;

    public BaseBody bodyRelativeTo;
    float lastSimulation;
    CameraController cam;
    UIManager ui;

    public bool areConicsDrawn = false;

    public Vector3[][] futurePoints;

    List<VirtualBody> vBodies = new List<VirtualBody>();
    int refFrameIndex = 0;
    Vector3 referenceBodyInitialPosition = Vector3.zero;
    int current;
    Vector3 refBodyPosition = Vector3.zero;
    List<LineRenderer> lineRenderers = new List<LineRenderer>();
    private bool exclusive = false;

    public GameObject ghostPlanet;

    public event Action onChangePlanets;

    public Vector3[][] ellipsePoints;

    private void Awake()
    {
        cam = FindObjectOfType<CameraController>();
        bodies = FindObjectsOfType<BaseBody>().ToList();
        ui = FindObjectOfType<UIManager>();

        NormalSimulation();
    }

    public void Start()
    {
        bodyRelativeTo = bodies.ToList().OrderByDescending(b => b.mass).First();
        Debug.Log(bodyRelativeTo.bodyName);

        for (int i = 0; i < bodies.Count; i++)
        {
            lineRenderers.Add(bodies[i].GetComponent<LineRenderer>());
        }

        onChangePlanets.Invoke();
    }

    public void ToggleConics()
    {
        areConicsDrawn = !areConicsDrawn;
    }

    public void ToggleExclusive()
    {
        exclusive = !exclusive;

        DeleteExistingOrbits();
    }

    int apogeeIndex;
    int perigeeIndex;
    public void CreateConic()
    {
        #region Old Way
        //List<Vector3> conicPoints = new List<Vector3>();

        //Vector3 lastPos = body.transform.position;

        //GameObject copyObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        //copyObj.transform.position = lastPos;
        //copyObj.AddComponent<CelestialBody>();

        //CelestialBody copy = (CelestialBody)copyObj.GetComponent<BaseBody>();

        //copy.radius = body.radius;
        //copy.mass = body.radius;
        //copy.initialVelocity = body.rb.velocity;
        //copy.bodyName = body.bodyName;
        //copy.trail.widthCurve = body.trail.widthCurve;
        //copy.trail.material = conicMaterial;
        //copy.trail.time = body.trail.time;
        //copy.trail.startWidth = body.radius / 2;
        //copy.trail.endWidth = 0;

        //List<CelestialBody> bodiesCopy = bodies.ToList();
        //bodiesCopy.Remove(body);

        //for (float i = 0; i < 10; i += 0.001f)
        //{
        //    copyObj.GetComponent<TrailRenderer>().material = conicMaterial;
        //    copy.UpdateVelocity(bodiesCopy.ToArray(), 0.1f);
        //    copy.UpdatePosition(0.1f);
        //}

        //BaseBody mirrorBody = GameObject.CreatePrimitive(PrimitiveType.Sphere).AddComponent<BaseBody>();
        //mirrorBody.transform.position = body.transform.position;

        //mirrorBody.gameObject.AddComponent<Rigidbody>();
        //mirrorBody.gameObject.AddComponent<TrailRenderer>();
        //mirrorBody.GetComponent<MeshRenderer>().enabled = false;

        //mirrorBody.trail = mirrorBody.GetComponent<TrailRenderer>();
        //mirrorBody.rb = mirrorBody.GetComponent<Rigidbody>();

        //mirrorBody.trail.material = conicMaterial;
        //mirrorBody.trail.time = body.trail.time;
        //mirrorBody.trail.startWidth = body.transform.localScale.x / 2;
        //mirrorBody.trail.endWidth = 0;
        //mirrorBody.fake = true;

        //for (float i = 0; i < 10; i += 0.001f)
        //{
        //    mirrorBody.GetComponent<TrailRenderer>().material = conicMaterial;
        //    mirrorBody.UpdateVelocity(bodies, 1f);
        //    mirrorBody.UpdatePosition(1f);
        //}
        #endregion
        int wantedBody = bodies.ToList().IndexOf(cam.currentTracking);

        //Instead of trying to implement a really complicated algorithm and trying to solve
        //the N-Body problem mathematically, we can run a second, "fake" simulation of the 
        //current system, and then trace the path that that simulation takes.
        //The simulation runs faster than the real simulation, giving the illusion of predicting the 
        //path of the solar system.
        vBodies = new VirtualBody[bodies.Where(x => x.fake == false).Count()].ToList();
        futurePoints = new Vector3[bodies.Count][];
        ellipsePoints = new Vector3[bodies.Count][];

        refFrameIndex = 0;
        referenceBodyInitialPosition = Vector3.zero;

        // Taking inspiration from Sebastian Lague's implementation,

        // This for loop creates an array of "fake" bodies, which are simulated.
        // What differes these from the original bodies is that these are not rendered,
        // only their trails are displayed so that the user can see their trajectories
        PrepareForConicCalculations(vBodies, ref refFrameIndex, ref referenceBodyInitialPosition);

        // For loop that will calculate the position of bodies for x number of steps
        // This basically runs a second simulation, although not rendering it yet
        for (int i = 0; i < conicLookahead; i++)
        {
            refBodyPosition = Vector3.zero;
            if (conicRelative)
            {
                refBodyPosition = vBodies[refFrameIndex].position;
            }

            CalculateFutureAcceleration();
            current = i;

            CalculateFuturePoints();
        }

        CalculateLineRenderer();

        areConicsDrawn = true;
    }

    private void PrepareForConicCalculations(List<VirtualBody> vBodies, ref int refFrameIndex, ref Vector3 referenceBodyInitialPosition)
    {
        for (int i = 0; i < vBodies.Count; i++)
        {
            if (bodies[i].fake) continue;

            vBodies[i] = new VirtualBody(bodies[i]);
            futurePoints[i] = new Vector3[conicLookahead];

            if (bodies[i] == bodyRelativeTo && conicRelative)
            {
                refFrameIndex = i;
                referenceBodyInitialPosition = vBodies[i].position;
            }
        }
    }

    private void CalculateLineRenderer()
    {
        // In this loop, we are actually rendering the points we saved from each virtual
        // simulated body.

        if (exclusive)
        {
            int wantedBody = bodies.ToList().IndexOf(ui.observedBody);
            for (int bodyIndex = 0; bodyIndex < vBodies.Count; bodyIndex++)
            {
                LineRenderer line = lineRenderers[wantedBody];
                line.enabled = true;

                line.material = conicMaterial;
                line.startColor = Color.yellow;
                line.endColor = Color.yellow;
                line.widthMultiplier = bodies[wantedBody].transform.localScale.x / 4;
                // This loop actually draws a line based on the points that we saved earlier.
                for (int i = 0; i < futurePoints[wantedBody].Length; i++)
                {
                    line.positionCount = futurePoints[wantedBody].Length;
                    line.SetPositions(futurePoints[wantedBody]);

                    //Debug.Log(line.positionCount + " pos " + line.enabled);
                }
            }
        }
        else
        {
            for (int bodyIndex = 0; bodyIndex < vBodies.Count; bodyIndex++)
            {
                LineRenderer line = lineRenderers[bodyIndex];
                line.enabled = true;

                line.material = conicMaterial;
                line.startColor = Color.yellow;
                line.endColor = Color.yellow;
                line.widthMultiplier = bodies[bodyIndex].transform.localScale.x / 4;
                // This loop actually draws a line based on the points that we saved earlier.
                for (int i = 0; i < futurePoints[bodyIndex].Length; i++)
                {
                    line.positionCount = futurePoints[bodyIndex].Length;
                    line.SetPositions(futurePoints[bodyIndex]);

                    //Debug.Log(line.positionCount + " pos " + line.enabled);
                }
            }
        }
        
    }

    public float FindAverageDistanceFromBody(BaseBody body)
    {
        if(bodyRelativeTo != null)
        {
            int bodyIndex = bodies.IndexOf(body);

            Vector3[] thisFuture = futurePoints[bodyIndex];

            float sumOfDistances = 0;

            for (int i = 0; i < thisFuture.Length; i++)
            {
                sumOfDistances += (bodyRelativeTo.gameObject.transform.position - body.gameObject.transform.position).magnitude;
            }

            float avgDistance = sumOfDistances / thisFuture.Length;

            return avgDistance;
        }
        else
        {
            return 0;
        }
    }

    public float FindPeriodForBody(BaseBody body)
    {
        if(bodyRelativeTo != null)
        {
            float semiMajorAxis = FindSemiMajorAxis(body);
            float twoPi = Mathf.PI * 2;
            float bottomFraction = gravConstant * (body.mass + bodyRelativeTo.mass);

            float fraction = Mathf.Pow(semiMajorAxis, 3) / bottomFraction;
            float period = twoPi * Mathf.Sqrt(fraction);
            return period;
        }
        else
        {
            return 0;
        }
    }

    public float FindSOIRadius(BaseBody body)
    {
        if(bodyRelativeTo != null)
        {
            // in Unity scale. i would need to divide this scale by the mass scale in order to find an appropriate scale for it
            float semiMajorAxis = FindSemiMajorAxis(body);
            Debug.Log("SOI is " + semiMajorAxis);
            float fraction = body.mass / bodyRelativeTo.mass;
            float radius = semiMajorAxis * Mathf.Pow(fraction, (2 / 5));
            return radius;
        }
        else
        {
            return 0;
        }
    }

    // SemiMajor axis is one half of the largest diameter of the body's orbit
    public float FindSemiMajorAxis(BaseBody body)
    {
        if(futurePoints != null)
        {
            Vector3[] bodyPoints = futurePoints[bodies.IndexOf(body)];

            // My logic for working this out will be that I am looping through each point and finding the distance between it and the body.
            // The furthest point in this single rotation will be the one before the points start to get closer

            bool hasFoundOneCloser = false;
            float lastDistance = 0;
            int counter = -1;
            Debug.Log(bodyPoints.Length);
            while (hasFoundOneCloser == false)
            {
                counter++;
                Vector3 point = bodyPoints[counter];
                Debug.Log(point);
                float distance = Vector3.Distance(body.gameObject.transform.position, bodyRelativeTo.gameObject.transform.position);
                if (distance >= lastDistance)
                {
                    Debug.Log("Current distance is " + distance + " at counter " + counter);
                    lastDistance = distance;
                    
                }
                else
                {
                    hasFoundOneCloser = true;
                }
            }

            Debug.Log("SMA is " + (lastDistance / 2));
            return lastDistance / 2;
        }
        else
        {
            return 0;
        }
    }

    private void CalculateFutureAcceleration(/*VirtualBody[] vBodies*/)
    {
        // This loop updates the velocities of all the virtual bodies
        for (int j = 0; j < vBodies.Count; j++)
        {
            vBodies[j].velocity += CalculateConicAcceleration(j, vBodies) * conicTimeStep;
        }
    }

    private void CalculateFuturePoints()
    {
        for (int j = 0; j < vBodies.Count; j++)
        {
            VirtualBody body = vBodies[j];
            Vector3 newBodyPos = vBodies[j].position + vBodies[j].velocity * conicTimeStep;
            vBodies[j].position = newBodyPos;

            if (conicRelative)
            {
                Vector3 frameOffset = refBodyPosition - referenceBodyInitialPosition;
                newBodyPos -= frameOffset;
            }
            else if (conicRelative && current == refFrameIndex)
            {
                newBodyPos = referenceBodyInitialPosition;
            }

            futurePoints[j][current] = newBodyPos;
        }
    }

    // This deletes the line data for the currente orbit in order to be able to toggle
    // the display.
    public void HideOrbits()
    {
        DeleteExistingOrbits();

        areConicsDrawn = false;
    }

    public void DeleteExistingOrbits()
    {
        BaseBody[] bodies = FindObjectsOfType<BaseBody>();

        for (int i = 0; i < bodies.Length; i++)
        {
            var line = bodies[i].GetComponent<LineRenderer>();
            line.positionCount = 0;

            line.enabled = false;
        }
    }

    // Function to calculate the acceleration of a single body
    Vector3 CalculateConicAcceleration(int j, List<VirtualBody> vBodies)
    {
        Vector3 acceleration = Vector3.zero;
        for (int i = 0; i < vBodies.Count; i++)
        {
            if (i == j) continue;

            Vector3 fDir = (vBodies[i].position - vBodies[j].position).normalized;
            float sqrDst = (vBodies[i].position - vBodies[j].position).sqrMagnitude;
            acceleration += fDir * gravConstant * vBodies[i].mass / sqrDst;
        }
        return acceleration;
    }

    // Unity function which runs a
    // around 50 times per second in time with the physics update
    // Here, we calculate the velocities and the positions of the objects.
    // We do it here so we don't have to do it an unnecessary amount of times in the normal
    // Update function.
    private void FixedUpdate()
    {
        for (int i = 0; i < bodies.Count; i++)
        {
            bodies[i].UpdateVelocity(bodies, timeStep);
        }

        for (int i = 0; i < bodies.Count; i++)
        {
            bodies[i].UpdatePosition(timeStep);
        }        
    }

    public void Update()
    {
        CalculateConics();
    }

    public void CalculateConics()
    {
        if (areConicsDrawn)
        {
            CreateConic();
        }
        else
        {
            HideOrbits();
        }
    }

    // Function to enable or disable the simulation, used in the pause/play button
    public void ToggleSimulation()
    {
        if(timeStep != 0)
        {
            lastSimulation = timeStep;
            timeStep = 0;
        }
        else
        {
            timeStep = lastSimulation;
            lastSimulation = 0;
        }    
    }

    // Function to default to a normal simulation
    private void NormalSimulation()
    {
        timeStep = 0.001f;
    }

    // Function to default to a fast simulation
    public void FastSimulation()
    {
        timeStep = 0.5f;
    }

    // Function that gets called when the slider for the speed gets changed
    public void SlideValueChange(float newValue)
    {
        timeStep = newValue;
    }

    public void Snapshot(CelestialBody bodyToSnap)
    {
        GameObject snapshot = Instantiate(ghostPlanet, bodyToSnap.transform.position, Quaternion.identity);
        snapshot.GetComponent<BaseBody>().bodyName = bodyToSnap.bodyName + " [Snapshot]";
        snapshot.GetComponent<CelestialBody>().radius = bodyToSnap.radius;
        snapshot.GetComponent<CelestialBody>().mass = bodyToSnap.mass;
        snapshot.GetComponent<BaseBody>().fake = true;
        bodies.Add(snapshot.GetComponent<BaseBody>());

        snapshot.GetComponent<BaseBody>().currentVelocity = Vector3.zero;

        onChangePlanets.Invoke();
    }
}
