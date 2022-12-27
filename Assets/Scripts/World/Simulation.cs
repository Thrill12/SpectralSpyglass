using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using System.Threading;
using System;
using System.Diagnostics;

public class Simulation : MonoBehaviour
{
    public float gravConstant = 0.0001f;
    public Material conicMaterial;
    public Material periApogeeLineMaterial;

    public List<BaseBody> bodies = new List<BaseBody>();
    public float timeStep;
    public float conicTimeStep = 1;
    public int maxConicLookahead = 100000;
    public bool conicRelative = true;

    public BaseBody bodyRelativeTo;
    float lastSimulation;
    CameraController cam;
    UIManager ui;

    public bool areConicsDrawn = false;

    public List<List<Vector3>> futurePoints = new List<List<Vector3>>();

    List<VirtualBody> vBodies = new List<VirtualBody>();
    int refFrameIndex = 0;
    Vector3 referenceBodyInitialPosition = Vector3.zero;
    int current;
    Vector3 refBodyPosition = Vector3.zero;
    List<LineRenderer> lineRenderers = new List<LineRenderer>();
    private bool exclusive = false;

    public GameObject ghostPlanet;

    public event Action onChangePlanets;

    public List<List<Vector3>> ellipsePoints = new List<List<Vector3>>();
    LineRenderer lineDebug;
    private void Awake()
    {
        futurePoints = new List<List<Vector3>>();

        cam = FindObjectOfType<CameraController>();
        bodies = FindObjectsOfType<BaseBody>().ToList();
        ui = FindObjectOfType<UIManager>();

        lineDebugSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        lineDebug = lineDebugSphere.AddComponent<LineRenderer>();

        onChangePlanets += SetupVBodies;

        NormalSimulation();
    }

    public void Start()
    {
        bodyRelativeTo = bodies.ToList().OrderByDescending(b => b.mass).First();

        for (int i = 0; i < bodies.Count; i++)
        {
            lineRenderers.Add(bodies[i].GetComponent<LineRenderer>());
        }

        onChangePlanets.Invoke();

        lineDebug.material = periApogeeLineMaterial;
        lineDebug.positionCount = 2;
        lineDebug.startWidth = 1f;
        lineDebug.endWidth = 1f;

        UnityEngine.Debug.Log(FindVectorPercentDifference(new Vector3(1, 3, 5), new Vector3(5, 9, 6)));
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
    Stopwatch watch = new Stopwatch();

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

    private void SetupVBodies()
    {
        vBodies.Clear();
        futurePoints.Clear();
        foreach (var item in bodies.Where(x => x.fake == false))
        {
            vBodies.Add(new VirtualBody(item));
            futurePoints.Add(new List<Vector3>());
            UnityEngine.Debug.Log("Added " + item.bodyName + " to vBodies");
        }
        refFrameIndex = 0;
        referenceBodyInitialPosition = Vector3.zero;
        PrepareForConicCalculations(vBodies, ref refFrameIndex, ref referenceBodyInitialPosition);
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

        // Use an average percentage difference for the vector, calculated on the one at the beginning.
        // We will know what step to top at when calculating the orbit based on a low enough percentage difference, 
        // an arbitrary number.

        // percentDiff = (thisVector - startVector) / ((thisVector + startVector) / 2)

        //UnityEngine.Debug.Log(vBodies.Count);

        for (int i = 0; i < vBodies.Count; i++)
        {
            refBodyPosition = Vector3.zero;
            if (conicRelative)
            {
                refBodyPosition = vBodies[refFrameIndex].position;
            }

            Vector3 start = vBodies[i].position;
            Vector3 currentPosition = bodies.First(x => x.bodyName == vBodies[i].bodyName).transform.position;
            current = 0;
            //UnityEngine.Debug.Log(vBodies.Count);

            do
            {
                CalculateSpecificAcceleration(i);
                Vector3 newPosition = CalculateFuturePointReturnVector(i);
                //UnityEngine.Debug.Log("Element " + i + " added a position");
                currentPosition = newPosition;
                //UnityEngine.Debug.Log("Current percentage difference is " + FindVectorPercentDifference(start, currentPosition));
                current += 1;
            } while (current <= 50 || Vector2.Distance(start, currentPosition) > 1f);

            //while(Vector3.Distance(start, currentPosition) > 0.01f || current <= 50)
            //{
            //    CalculateSpecificAcceleration(i);
            //    Vector3 newPosition = CalculateFuturePointReturnVector(i);
            //    UnityEngine.Debug.Log("Element " + i + " added a position");
            //    currentPosition = newPosition;
            //    UnityEngine.Debug.Log("Current percentage difference is " + FindVectorPercentDifference(start, currentPosition));
            //    current += 1;
            //}
        }

        // For loop that will calculate the position of bodies for x number of steps
        // This basically runs a second simulation, although not rendering it yet
        //for (int i = 0; i < maxConicLookahead; i++)
        //{
        //    refBodyPosition = Vector3.zero;
        //    if (conicRelative)
        //    {
        //        refBodyPosition = vBodies[refFrameIndex].position;
        //    }

        //    watch.Restart();
        //    CalculateFutureAcceleration();
        //    watch.Stop();
        //    current = i;

        //    watch.Restart();
        //    CalculateAllFuturePoints();
        //    watch.Stop();
        //}

        watch.Restart();
        CalculateLineRenderer();
        watch.Stop();
        UnityEngine.Debug.Log("CalculateLineRenderer took " + watch.ElapsedMilliseconds);

        areConicsDrawn = true;
    }

    private void PrepareForConicCalculations(List<VirtualBody> vBodies, ref int refFrameIndex, ref Vector3 referenceBodyInitialPosition)
    {
        UnityEngine.Debug.Log(vBodies.Count + " is the vbodies count in prepare");
        for (int i = 0; i < vBodies.Count; i++)
        {
            UnityEngine.Debug.Log("Accessing vbodies index " + i);
            if (bodies[i].fake) continue;

            //vBodies[i] = new VirtualBody(bodies[i]);
            ////futurePoints[i] = new Vector3[maxConicLookahead];
            //futurePoints[i] = new List<Vector3>();

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

                for (int i = 0; i < futurePoints[wantedBody].Count; i++)
                {
                    line.positionCount = futurePoints[wantedBody].Count;
                    line.SetPositions(futurePoints[wantedBody].ToArray());

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
                for (int i = 0; i < futurePoints[bodyIndex].Count; i++)
                {
                    line.positionCount = futurePoints[bodyIndex].Count;
                    line.SetPositions(futurePoints[bodyIndex].ToArray());

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

    public float FindAreaOverTime(BaseBody body, float time)
    {
        // implement logic for working out the area. Will need to find the angle from the line
        // where the body started in the time, and the current planet's position
        return time;
    }

    public float GetMajorAxis(BaseBody body)
    {
        return FindSemiMajorAxis(body) * 2f;
    }

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
    public float FindSemiMajorAxis(BaseBody body)
    {
        if(futurePoints != null && futurePoints != null)
        {
            List<Vector3> bodyPoints = new List<Vector3>();

            UnityEngine.Debug.Log("Trying to access index " + bodies.IndexOf(body) + " when max index is " + futurePoints.Count);

            bodyPoints = futurePoints[bodies.IndexOf(body)];

            // My logic for working this out will be that I am looping through each point and finding the distance between it and the body.
            // The furthest point in this single rotation will be the one before the points start to get closer

            float lastDistance = 0;
            int counter = -1;

            Vector3 closestPoint = Vector3.zero;
            int closestPointIndex = 0;
            float closestPointDistance = Mathf.Infinity;

            for (int i = 0; i < bodyPoints.Count; i++)
            {
                Vector3 point = bodyPoints[i];
                float distance = Vector3.Distance(point, bodyRelativeTo.transform.position);
                if (distance < closestPointDistance)
                {
                    closestPointDistance = distance;
                    closestPoint = point;
                    closestPointIndex = bodyPoints.ToList().IndexOf(point);
                }
            }
            body.periapsisDistance = closestPointDistance;
            lineDebug.SetPosition(0, closestPoint);

            // We want to find the next furthest point which will be in the current orbital period.
            // If we didn't start from here, we might find some other furthest point in the next orbit or something.
            counter = closestPointIndex;

            Vector3 furthestPoint = Vector3.zero;

            //for (int i = counter; i < counter + (bodyPoints.Length - counter); i++)
            //{
            //    Vector3 point = bodyPoints[i];
            //    float distance = Vector3.Distance(point, bodyRelativeTo.transform.position);
            //    if (distance >= lastDistance)
            //    {
            //        lastDistance = distance;
            //        furthestPoint = point;
            //    }
            //    else
            //    {
            //        break;
            //    }
            //}

            for (int i = 0; i < bodyPoints.Count; i++)
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

            float realDistance = lastDistance - closestPointDistance;

            lineDebug.SetPosition(1, furthestPoint);

            return realDistance / 2;
        }
        else
        {
            return 0;
        }
    }

    private void CalculateFutureAcceleration()
    {
        // This loop updates the velocities of all the virtual bodies
        for (int j = 0; j < vBodies.Count; j++)
        {
            CalculateSpecificAcceleration(j);
        }
    }

    private void CalculateSpecificAcceleration(int j)
    {
        vBodies[j].velocity += CalculateConicAcceleration(j, vBodies) * conicTimeStep;
    }

    private void CalculateAllFuturePoints()
    {
        for (int j = 0; j < vBodies.Count; j++)
        {
            CalculateFuturePoint(j);
        }
    }

    private void CalculateFuturePoint(int j)
    {
        CalculateFuturePointReturnVector(j);
    }

    private Vector3 CalculateFuturePointReturnVector(int j)
    {
        if(j < futurePoints.Count)
        {
            if (current < futurePoints[j].Count)
            {
                UnityEngine.Debug.Log("calculating point " + current + " in body " + j);
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

                futurePoints[j].Add(newBodyPos);

                return newBodyPos;
            }
            else
            {
                return Vector3.zero;
            }
        }
        else
        {
            return Vector3.zero;
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

        CalculateConics();
    }

    public void Update()
    {
        
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
        timeStep = ui.simSpeedSlider.maxValue * 0.1f;
    }

    // Function to default to a fast simulation
    public void FastSimulation()
    {
        timeStep = ui.simSpeedSlider.maxValue * 0.8f;
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
