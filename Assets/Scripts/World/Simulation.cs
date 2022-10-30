using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using System.Threading;

public class Simulation : MonoBehaviour
{
    public float timeBetweenConicCalculations;
    [HideInInspector] public float gravConstant = 0.0001f;
    public Material conicMaterial;
    [HideInInspector]
    public BaseBody[] bodies;
    public float timeStep;
    public float conicTimeStep = 1;
    public int conicLookahead;
    public bool conicRelative = true;

    BaseBody massiveBody;
    float lastSimulation;
    CameraController cam;

    bool areConicsDrawn = false;

    public Vector3[][] futurePoints;

    List<VirtualBody> vBodies = new List<VirtualBody>();
    int refFrameIndex = 0;
    Vector3 referenceBodyInitialPosition = Vector3.zero;
    int current;
    Vector3 refBodyPosition = Vector3.zero;
    List<LineRenderer> lineRenderers = new List<LineRenderer>();
    private bool exclusive = false;
    private void Awake()
    {
        cam = FindObjectOfType<CameraController>();
        bodies = FindObjectsOfType<BaseBody>();
        NormalSimulation();
    }

    public void Start()
    {
        massiveBody = bodies.ToList().OrderByDescending(b => b.mass).First();
        Debug.Log(massiveBody.bodyName);

        for (int i = 0; i < bodies.Length; i++)
        {
            lineRenderers.Add(bodies[i].GetComponent<LineRenderer>());
        }
    }

    public void ToggleConics()
    {
        areConicsDrawn = !areConicsDrawn;
    }

    public void ToggleExclusive()
    {
        exclusive = !exclusive;

        if (exclusive)
        {
            massiveBody = cam.currentTracking.largestInfluencer;
        }
        else
        {
            massiveBody = bodies.ToList().OrderByDescending(b => b.mass).First();
        }

        DeleteExistingOrbits();
    }

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
        //https://github.com/SebLague/Solar-System/blob/Episode_01/Assets/Scripts/Debug/OrbitDebugDisplay.cs

        vBodies = new VirtualBody[bodies.Length].ToList();
        futurePoints = new Vector3[bodies.Length][];

        refFrameIndex = 0;
        referenceBodyInitialPosition = Vector3.zero;

        // Taking inspiration from Sebastian Lague's implementation,
        // However will be adapted to take into account SOIs of the planets

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

            //ThreadStart accThreadStart = new ThreadStart(CalculateFutureAcceleration);
            CalculateFutureAcceleration();
            current = i;

            //Thread accThread = new Thread(accThreadStart);
            //accThread.Start();

            //ThreadStart fpThreadStart = new ThreadStart(CalculateFuturePoints);
            CalculateFuturePoints();

            //Thread fpThread = new Thread(fpThreadStart);
            //fpThread.Start();
        }

        //ThreadStart rendThreadStart = new ThreadStart(CalculateLineRenderer);
        //Thread rendThread = new Thread(rendThreadStart);
        //rendThread.Start();

        CalculateLineRenderer();

        areConicsDrawn = true;
    }

    private void PrepareForConicCalculations(List<VirtualBody> vBodies, ref int refFrameIndex, ref Vector3 referenceBodyInitialPosition)
    {
        for (int i = 0; i < vBodies.Count; i++)
        {
            vBodies[i] = new VirtualBody(bodies[i]);
            futurePoints[i] = new Vector3[conicLookahead];

            if (bodies[i] == massiveBody && conicRelative)
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
            int wantedBody = bodies.ToList().IndexOf(cam.currentTracking);
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

    // This delets the line data for the currente orbit in order to be able to toggle
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

    // Unity function which runs at around 50 times per second in time with the physics update
    // Here, we calculate the velocities and the positions of the objects.
    // We do it here so we don't have to do it an unnecessary amount of times in the normal
    // Update function.
    private void FixedUpdate()
    {
        for (int i = 0; i < bodies.Length; i++)
        {
            bodies[i].UpdateVelocity(bodies, timeStep);
        }

        for (int i = 0; i < bodies.Length; i++)
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
}
