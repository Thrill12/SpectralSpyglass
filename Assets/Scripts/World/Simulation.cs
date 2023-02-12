using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using System.Threading;
using System;
using System.Diagnostics;

/// <summary>
/// Class that handles the actual simulation backend of the program
/// </summary>
public class Simulation : MonoBehaviour
{
    /// <summary>
    /// The gravitational constant is an important constant that is used to model accurately what happens in the real world
    /// IRL, this value is 6.67e-11, however when scaling down with the specific scale factors I have chosen, this
    /// transforms into 6.67e-8. I have set this value in the inspector and not hard coded it for testing reasons
    /// </summary>
    public float gravConstant = 0.0001f;
    // Material used to give conics colour
    public Material conicMaterial;
    // Material used to give the line splitting the orbit in half colour
    public Material periApogeeLineMaterial;

    // List of bodies currently in the simulation
    public List<BaseBody> bodies = new List<BaseBody>();
    // Value that represents how fast the simulation is being simulated at
    public float timeStep;
    // Value that represents how fast the conic simulation is being simulated at
    public float conicTimeStep = 1;
    // The maximum number of points the conics may have per planet. I have set this value in the inspector
    // at a reasonable number to account for performance issues when using larger numbers but also high
    // enough to give a relatively smooth line, at least until the conic time step variable is too high
    public int maxConicLookahead = 100000;
    // Whether the conics have to be drawn relative to a body
    public bool conicRelative = true;

    // We need to keep track of the body we are relative to as this plays an important role in calculations
    // such as the SOI radius
    public BaseBody bodyRelativeTo;
    // Small value to keep track of the timestep before pausing so we can return to it immediately
    float lastSimulation;
    // Reference to the camera controller script to see which object we are observing
    CameraController cam;
    // Reference to the UI manager script
    UIManager ui;

    // Bool to keep track of whether we want the conics to be drawn.
    public bool areConicsDrawn = false;
    // A 2D array with the first dimension representing each planet, and the 2nd dimension representing
    // each planet's future points. This will be as long as maxConicLookahead
    public Vector3[][] futurePoints;

    // Array for holding the data for the 2nd background simulation of VirtualBodies, which
    // gets used to calculate and draw the conic trajectories
    VirtualBody[] vBodies;
    // This allows us to calculate the points relative to its own planet and to keep track of what point we are on
    int refFrameIndex = 0;
    // This allows us to calculate an offset from the original planet so that we can display the line next to the body
    // and not in the wrong coordinates
    Vector3 referenceBodyInitialPosition = Vector3.zero;
    // This allows us to see what point we are currently drawing. In the loop this gets used in, when we reach
    // maxConicLookahead in this variabe, it gets reset to 0 and moves on to the next body
    int current;
    // The position of the vBody as a reference, again used in calculating the offset of the conic points
    Vector3 refBodyPosition = Vector3.zero;
    // List of lineRenderers on each of the bodies, for ease of access. Indexes will represent the same planets they do
    // in the bodies array
    List<LineRenderer> lineRenderers = new List<LineRenderer>();
    // This bool allows us to know whether we have to draw the conics for only one planet or for all of them.
    // Helpful when the orbit of a planet is interrupted by other orbits
    private bool exclusive = false;

    // The object template to spawn whenever taking a snapshot
    public GameObject ghostPlanet;

    // This handles calling all the specified Actions whenever we change planets
    public event Action onChangePlanets;

    // Another line renderer component for debugging purposes
    LineRenderer lineDebug;

    // Unity function that gets called even before the start function - this is used because we need
    // some of it in the start function
    // and it was cleaner writing that in here rather than at the beginning of the start function
    private void Awake()
    {
        cam = FindObjectOfType<CameraController>();
        bodies = FindObjectsOfType<BaseBody>().ToList();
        ui = FindObjectOfType<UIManager>();

        lineDebugSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        lineDebug = lineDebugSphere.AddComponent<LineRenderer>();

        // Here we add an action to when the bodies change to SetupVBodies.
        onChangePlanets += SetupVBodies;

        // We automatically start the simulation on the predefined normal speed
        NormalSimulation();
    }

    public void Start()
    {
        // The default body we are relative to is the body with the biggest mass using LINq to figure it out,
        // but the user can relativise to any body they want to afterwards
        bodyRelativeTo = bodies.ToList().OrderByDescending(b => b.mass).First();

        UpdatePlanets();

        // Setting up the properties for the debugging line renderer
        lineDebug.material = periApogeeLineMaterial;
        lineDebug.positionCount = 2;
        lineDebug.startWidth = 1f;
        lineDebug.endWidth = 1f;
    }

    public void UpdatePlanets()
    {
        // We call all the actions set in that event so that it can be set up at least once
        onChangePlanets.Invoke();

        lineRenderers.Clear();

        // We set the line renderers components to be accessed later on in the script
        for (int i = 0; i < bodies.Count; i++)
        {
            lineRenderers.Add(bodies[i].GetComponent<LineRenderer>());
        }
    }

    /// <summary>
    /// We toggle the visualization of conics
    /// </summary>
    public void ToggleConics()
    {
        areConicsDrawn = !areConicsDrawn;
    }

    /// <summary>
    /// We toggle whether the conics are exclusive 
    /// </summary>
    public void ToggleExclusive()
    {
        exclusive = !exclusive;

        DeleteExistingOrbits();
    }

    // A small stopwatch class used in testing the performance of conic calculations
    Stopwatch watch = new Stopwatch();

    /// <summary>
    /// An idea I had to dynamically generate the conics to limit them to one full orbit instead of overshooting an orbit.
    /// This would have allowed me to make much more accurate orbital elements calculations, however it is not currently
    /// in use.
    /// </summary>
    /// <param name="vec1"></param>
    /// <param name="vec2"></param>
    /// <returns></returns>
    public float FindVectorPercentDifference(Vector3 vec1, Vector3 vec2)
    {
        float dX = vec2.x - vec1.x;
        float dY = vec2.y - vec1.y;
        float dZ = vec2.z - vec1.z;

        float percX = dX / vec1.x;
        float percY = dY / vec1.y;
        float percZ = dZ / vec1.z;

        float averagePerc = (percX + percY + percZ) / 3f;

        return averagePerc;
    }

    /// <summary>
    /// Function to set up the virtualBodies for conic calculations. It is called whenever the number of planets 
    /// in the simulation changes
    /// </summary>
    private void SetupVBodies()
    {
        futurePoints = new Vector3[bodies.Count][];
        vBodies = new VirtualBody[bodies.Count];
        
        refFrameIndex = 0;
        referenceBodyInitialPosition = Vector3.zero;
        PrepareForConicCalculations(vBodies, ref refFrameIndex, ref referenceBodyInitialPosition);
    }

    /// <summary>
    /// This is the function that handles the creation of the conics. Inside, there are two ways of possibly doing this,
    /// however the old way that is commented out is much much slower than the implementation that takes inspiration
    /// from Sebastian Lague. His version is a lot cleaner, and required little adaptation to fit into my code.
    /// </summary>
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

        SetupVBodies();
        // Getting the index of the body we are currently tracking
        int wantedBody = bodies.ToList().IndexOf(cam.currentTracking);

        //Instead of trying to implement a really complicated algorithm and trying to solve
        //the N-Body problem mathematically, we can run a second, "fake" simulation of the 
        //current system, and then trace the path that that simulation takes.
        //The simulation runs faster than the real simulation, giving the illusion of predicting the 
        //path of the solar system.
        //vBodies = new VirtualBody[bodies.Where(x => x.fake == false).Count()].ToList();                

        // Taking inspiration from Sebastian Lague's implementation,

        // This for loop creates an array of "fake" bodies, which are simulated.
        // What differes these from the original bodies is that these are not rendered,
        // only their trails are displayed so that the user can see their trajectories        
        // However, there is a flaw in his way of displaying orbits. Currently, there is no way of 
        // "locking" the orbit display so that it only displays one full orbit, instead it 
        // bleeds into the next orbit and next and so on, creating a confusing muddle of lines.
        //
        // This is in someway alleviated by the exclusive mode we have where we only display the orbits of the planet
        // we are looking at, but when trying to calculate orbital properties we need to find another way of displaying the orbits.
        //
        // Use an average percentage difference for the vector, calculated on the one at the beginning.
        // We will know what step to top at when calculating the orbit based on a low enough percentage difference, 
        // an arbitrary number.
        //
        // Above is my planned method to fix the overshooting of the conic lines, however I could not get it working in the alloted
        // time. I could come back to it later to try and fix it if I am still going to work on this after my coursework project.

        // Loop running until the maxConicLookahead number. This calculates the future points and accelerations of each virtual body for each step 
        // until we hit the maxConicLookahead
        for (int conicStep = 0; conicStep < maxConicLookahead; conicStep++)
        {
            Vector3 referenceBodyPosition;
            referenceBodyPosition = CalculateReferenceMaterial();

            CalculateVelocitiesVirtualBodies();

            CalculateNextFuturePointsVirtualBodies(conicStep, referenceBodyPosition);
        }
        
        // Here, we must call to render the future points we have calculated using each body's line renderer we set at the start
        watch.Restart();
        CalculateLineRenderer();
        watch.Stop();
        UnityEngine.Debug.Log("CalculateLineRenderer took " + watch.ElapsedMilliseconds);

        areConicsDrawn = true;
    }

    /// <summary>
    /// Function that is important in figuring out vectors for the relative side of the orbits, such as displaying them relative to which body, etc
    /// </summary>
    /// <returns></returns>
    private Vector3 CalculateReferenceMaterial()
    {
        Vector3 referenceBodyPosition;
        if (conicRelative)
        {
            referenceBodyPosition = vBodies[refFrameIndex].position;
        }
        else
        {
            referenceBodyPosition = Vector3.zero;
        }

        return referenceBodyPosition;
    }

    /// <summary>
    /// Function that calculates the respective velocities of each virtual body based on each other
    /// </summary>
    private void CalculateVelocitiesVirtualBodies()
    {
        for (int j = 0; j < vBodies.Length; j++)
        {
            vBodies[j].velocity += CalculateConicAcceleration(j, vBodies) * conicTimeStep;
        }
    }

    /// <summary>
    /// Function that will calculate one extra future point for each virtual body in the array. Doing this many times should give a smooth line that will
    /// predict the trajectory of the bodies. I am doing this the less preferred way, as in doing them in parallel instead of in series.
    /// </summary>
    /// <param name="conicStep"></param>
    /// <param name="referenceBodyPosition"></param>
    private void CalculateNextFuturePointsVirtualBodies(int conicStep, Vector3 referenceBodyPosition)
    {
        for (int i = 0; i < vBodies.Length; i++)
        {
            Vector3 newPos = vBodies[i].position + vBodies[i].velocity * conicTimeStep;
            vBodies[i].position = newPos;
            // If we want relative conics, we have to apply an offset based on whatever body we are relative to and our initial real body's position
            if (conicRelative)
            {
                var refFrameOffset = referenceBodyPosition - referenceBodyInitialPosition;
                newPos -= refFrameOffset;
            }

            if (conicRelative && i == refFrameIndex)
            {
                newPos = referenceBodyInitialPosition;
            }

            futurePoints[i][conicStep] = newPos;
        }
    }

    /// <summary>
    /// Prepares the script for the conic calculations, resetting the virtualBodies array to make sure we have the right number of vBodies, and setting
    /// the relative properties such as the index and the initial position
    /// </summary>
    /// <param name="vBodies"></param>
    /// <param name="refFrameIndex"></param>
    /// <param name="referenceBodyInitialPosition"></param>
    private void PrepareForConicCalculations(VirtualBody[] vBodies, ref int refFrameIndex, ref Vector3 referenceBodyInitialPosition)
    {
        UnityEngine.Debug.Log(vBodies.Length + " is the vbodies count in prepare");
        for (int i = 0; i < vBodies.Length; i++)
        {
            UnityEngine.Debug.Log("Accessing vbodies index " + i);
            if (bodies[i].fake) continue;

            vBodies[i] = new VirtualBody(bodies[i]);
            futurePoints[i] = new Vector3[maxConicLookahead];

            if (bodies[i] == bodyRelativeTo && conicRelative)
            {
                refFrameIndex = i;
                referenceBodyInitialPosition = vBodies[i].position;
            }
        }
    }

    /// <summary>
    /// Function that goes through each lineRenderer and draws its planet's orbit. Also accounts if we want it in exclusive mode or not
    /// </summary>
    private void CalculateLineRenderer()
    {
        // In this loop, we are actually rendering the points we saved from each virtual
        // simulated body.

        if (exclusive)
        {
            int wantedBody = bodies.ToList().IndexOf(ui.observedBody);
            for (int bodyIndex = 0; bodyIndex < vBodies.Length; bodyIndex++)
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
                    line.SetPositions(futurePoints[wantedBody].ToArray());
                }
            }
        }
        else
        {
            for (int bodyIndex = 0; bodyIndex < vBodies.Length; bodyIndex++)
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
                }
            }
        }
        
    }

    /// <summary>
    /// Finds the average distance of all of the future points towards to body that it is relative to. This can be used in some equations, however none as of yet implemented
    /// </summary>
    /// <param name="body"></param>
    /// <returns></returns>
    public float FindAverageDistanceFromBody(BaseBody body)
    {
        if(bodyRelativeTo != null)
        {
            int bodyIndex = bodies.IndexOf(body);

            Vector3[] thisFuture = futurePoints[bodyIndex].ToArray();

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

    /// <summary>
    /// Will display a visualization of Kepler's 3rd law (that the area traversed by a body per unit of time is equal no matter where in the orbit it is placed
    /// </summary>
    /// <param name="body"></param>
    /// <param name="time"></param>
    /// <returns></returns>
    public float FindAreaOverTime(BaseBody body, float time)
    {
        // implement logic for working out the area. Will need to find the angle from the line
        // where the body started in the time, and the current planet's position
        return time;
    }

    /// <summary>
    /// Finds the major axis, which is the full length from the most separated points on the orbit
    /// </summary>
    /// <param name="body"></param>
    /// <returns></returns>
    public float GetMajorAxis(BaseBody body)
    {
        return FindSemiMajorAxis(body) * 2f;
    }

    /// <summary>
    /// Finds the period of the orbit given all of its future points. This is semi inaccurate due to the conics overshooting and me having to come up with a 
    /// weak way to circumvent that.
    /// </summary>
    /// <param name="body"></param>
    /// <returns></returns>
    public float FindPeriodForBody(BaseBody body)
    {
        if(bodyRelativeTo != null)
        {
            float semiMajorAxis = FindSemiMajorAxis(body);
            float twoPi = Mathf.PI * 2f;
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

    /// <summary>
    /// Finds the radius of the spheres of influence for each body. It is important to take into account the body we are currently relative to, as
    /// that changes the Sphere of Influence completely.
    /// </summary>
    /// <param name="body"></param>
    /// <returns></returns>
    public float FindSOIRadius(BaseBody body)
    {
        if(bodyRelativeTo != null)
        {
            // in Unity scale. i would need to divide this scale by the mass scale in order to find an appropriate scale for it
            float semiMajorAxis = FindSemiMajorAxis(body);
            //Debug.Log("SMA is " + semiMajorAxis);
            float fraction = body.mass / bodyRelativeTo.mass;
            float twoFifths = 2f / 5f;
            float radius = semiMajorAxis * Mathf.Pow(fraction, twoFifths);
            //Debug.Log("SOI is " + radius);
            return radius;
        }
        else
        {
            return 0;
        }
    }

    GameObject lineDebugSphere;
    // SemiMajor axis is one half of the largest diameter of the body's orbit
    /// <summary>
    /// One of the most important elements of an orbit, it can be used to calculate almost everything else related to its orbit. It is vital we get a good enough estimation
    /// of this to give realistic results in our calculations and visualizations.
    /// </summary>
    /// <param name="body"></param>
    /// <returns></returns>
    public float FindSemiMajorAxis(BaseBody body)
    {
        if(futurePoints != null && body != null)
        {
            UnityEngine.Debug.Log("Trying to access index " + bodies.IndexOf(body) + " when max index is " + futurePoints.Length);

        //    Vector3[] bodyPoints = futurePoints[bodies.IndexOf(body)];

        //    // My logic for working this out will be that I am looping through each point and finding the distance between it and the body.
        //    // The furthest point in this single rotation will be the one before the points start to get closer

        //    float lastDistance = 0;
        //    int counter = -1;

        //    Vector3 closestPoint = Vector3.zero;
        //    int closestPointIndex = 0;
        //    float closestPointDistance = Mathf.Infinity;

        //    for (int i = 0; i < bodyPoints.Length; i++)
        //    {
        //        Vector3 point = bodyPoints[i];
        //        float distance = Vector3.Distance(point, bodyRelativeTo.transform.position);
        //        if (distance < closestPointDistance)
        //        {
        //            closestPointDistance = distance;
        //            closestPoint = point;
        //            closestPointIndex = bodyPoints.ToList().IndexOf(point);
        //        }
        //    }
        //    body.periapsisDistance = closestPointDistance;
        //    lineDebug.SetPosition(0, closestPoint);

        //    // We want to find the next furthest point which will be in the current orbital period.
        //    // If we didn't start from here, we might find some other furthest point in the next orbit or something.
        //    counter = closestPointIndex;

        //    Vector3 furthestPoint = Vector3.zero;

        //    for (int i = 0; i < bodyPoints.Length; i++)
        //    {
        //        Vector3 point = bodyPoints[i];
        //        float distance = Vector3.Distance(point, bodyRelativeTo.transform.position);
        //        if (distance >= lastDistance)
        //        {
        //            lastDistance = distance;
        //            furthestPoint = point;
        //        }
        //    }

        //    body.apoapsisDistance = lastDistance;

        //    float realDistance = lastDistance - closestPointDistance;

        //    lineDebug.SetPosition(1, furthestPoint);

        //    lineDebug.enabled = false;

        //    return realDistance / 2;
        //}
        //else
        //{
        //    return 0;
        //}

        // To find the semi major axis, we take the distance from the center of the ellipse to the point furthest on its orbit.
        // To find the center of the ellipse, we can take an average of all of the X, Y and Z coordinates.

        if(futurePoints != null)
        {
            Vector3[] bodyPoints = futurePoints[bodies.IndexOf(body)];

            float xSum = 0, ySum = 0, zSum = 0;
            float xAvg = 0, yAvg = 0, zAvg = 0;

            foreach (Vector3 item in bodyPoints)
            {
                xSum += item.x;
                ySum += item.y;
                zSum += item.z;
            }

            xAvg = xSum / bodyPoints.Length;
            yAvg = ySum / bodyPoints.Length;
            zAvg = zSum / bodyPoints.Length;

            Vector3 centerPoint = new Vector3(xAvg, yAvg, zAvg);
            body.pointOfOrbitCentre = centerPoint;
            float lastDistance = 0;

            Vector3 furthestPoint = centerPoint;

            for (int i = 0; i < bodyPoints.Length; i++)
            {
                Vector3 point = bodyPoints[i];
                float distance = Vector3.Distance(point, centerPoint);
                if (distance >= lastDistance)
                {
                    lastDistance = distance;
                    furthestPoint = point;
                }
            }

            body.furthestPointFromCentre = furthestPoint;

            lineDebug.SetPosition(0, centerPoint);
            lineDebug.SetPosition(1, furthestPoint);
            lineDebug.enabled = true;

            return lastDistance;
        }
        else
        {
            return 0;
        }
    }

    public void CalculateApoapsis(BaseBody body)
    {
        float lastDistance = 0;
        Vector3[] bodyPoints = futurePoints[bodies.IndexOf(body)];

        Vector3 furthestPoint = body.transform.position;

        for (int i = 0; i < bodyPoints.Length; i++)
        {
            Vector3 point = bodyPoints[i];
            float distance = Vector3.Distance(point, bodyRelativeTo.transform.position);
            if (distance >= lastDistance)
            {
                lastDistance = distance;
                furthestPoint = point;
            }
        }

        body.apoapsisDistance = lastDistance;
    }

    public void CalculatePeriapsis(BaseBody body)
    {
        Vector3[] bodyPoints = futurePoints[bodies.IndexOf(body)];
        float closestPointDistance = Mathf.Infinity;

        for (int i = 0; i < bodyPoints.Length; i++)
        {
            Vector3 point = bodyPoints[i];
            float distance = Vector3.Distance(point, bodyRelativeTo.transform.position);
            if (distance < closestPointDistance)
            {
                closestPointDistance = distance;
            }
        }

        body.periapsisDistance = closestPointDistance;
    }

    // This deletes the line data for the currente orbit in order to be able to toggle
    // the display.
    /// <summary>
    /// This hides the conic trajectories and deletes the points from all of the line renderers
    /// </summary>
    public void HideOrbits()
    {
        DeleteExistingOrbits();

        areConicsDrawn = false;
    }

    /// <summary>
    /// Executes same purpose as the above function, but it is used also in the exclusive mode of the conics
    /// </summary>
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
    /// <summary>
    /// Calculates the acceleration of a specific virtual body with int j. It calculates the overall net force on the body based on all of the other bodies in the arrays.
    /// </summary>
    /// <param name="j"></param>
    /// <param name="vBodies"></param>
    /// <returns></returns>
    Vector3 CalculateConicAcceleration(int j, VirtualBody[] vBodies)
    {
        Vector3 acceleration = Vector3.zero;
        for (int i = 0; i < vBodies.Length; i++)
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
    /// <summary>
    /// Fixed update is a unity function which runs inline with the physics engine of Unity, so that we can sync up perfectly. If we used update with forces,
    /// they might get out of sync and cause unexpected errors. The rigidbody component relies on Fixed Update, hence us setting its velocity and position in 
    /// fixed update.
    /// </summary>
    private void FixedUpdate()
    {
        for (int i = 0; i < bodies.Count; i++)
        {
            bodies[i].UpdateVelocity(bodies, timeStep);
        }

        for (int i = 0; i < bodies.Count; i++)
        {
            bodies[i].UpdatePosition(timeStep);
            CalculateApoapsis(bodies[i]);
            CalculatePeriapsis(bodies[i]);
        }

        CalculateConics();
    }

    /// <summary>
    /// Either draws conics or hides them
    /// </summary>
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
    /// <summary>
    /// Uses the saved value from last time we played the simulation to resume to the last timestep we used.
    /// </summary>
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
    /// <summary>
    /// Predefined value for the normal speed simulation, which is 10% of the maximum time step value we can have in the slider.
    /// </summary>
    private void NormalSimulation()
    {
        timeStep = ui.simSpeedSlider.maxValue * 0.1f;
    }

    // Function to default to a fast simulation
    /// <summary>
    /// Predefined value for the normal speed simulation, which is 80% of the maximum time step value we can have in the slider.
    /// </summary>
    public void FastSimulation()
    {
        timeStep = ui.simSpeedSlider.maxValue * 0.8f;
    }

    // Function that gets called when the slider for the speed gets changed
    /// <summary>
    /// Called when the slider changes value from the user's inputs.
    /// </summary>
    /// <param name="newValue"></param>
    public void SlideValueChange(float newValue)
    {
        timeStep = newValue;
    }

    /// <summary>
    /// Creates a ghost image of the planet, keeping it still and allows us to measure distances from one specific point using the Target distance.
    /// </summary>
    /// <param name="bodyToSnap"></param>
    public void Snapshot(CelestialBody bodyToSnap)
    {
        GameObject snapshot = Instantiate(ghostPlanet, bodyToSnap.transform.position, Quaternion.identity);
        snapshot.GetComponent<BaseBody>().bodyName = bodyToSnap.bodyName + " [Snapshot]";
        snapshot.GetComponent<CelestialBody>().radius = bodyToSnap.radius;
        snapshot.GetComponent<CelestialBody>().mass = bodyToSnap.mass;
        snapshot.GetComponent<BaseBody>().fake = true;
        bodies.Add(snapshot.GetComponent<BaseBody>());

        snapshot.GetComponent<BaseBody>().currentVelocity = Vector3.zero;

        // It is important to call onChangePlanets here as the snapshot is a selectable "body" yet it is "fake" and so no forces apply to it, and other
        // bodies don't take it into account when calculating their velocities.
        onChangePlanets.Invoke();
    }
}
