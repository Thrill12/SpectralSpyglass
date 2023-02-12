using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddRemoveBody : MonoBehaviour
{
    public GameObject bodyPrefab;

    Simulation sim;
    CameraController camController;
    UIManager ui;

    // Setting the references for the scripts we need
    private void Start()
    {
        sim = FindObjectOfType<Simulation>();
        camController = FindObjectOfType<CameraController>();
        ui = FindObjectOfType<UIManager>();
    }

    /// <summary>
    /// Function for adding a preset object to the simulation, at the position the camera is currently in.
    /// It triggers the update planets event which sets up everything UI wise.
    /// </summary>
    public void AddBody()
    {
        GameObject newPlanet = GameObject.Instantiate(bodyPrefab, camController.cam.transform.position, Quaternion.identity);
        BaseBody planetBody = newPlanet.GetComponent<BaseBody>();
        sim.bodies.Add(planetBody);
        sim.UpdatePlanets();
    }

    /// <summary>
    /// Function to remove a body, used from the context menu. It makes a few checks to see
    /// whether it is the body we are relative to, if the camera is currently orbiting it, etc.
    /// Only removes it if we have more than one body in the system
    /// </summary>
    /// <param name="body"></param>
    public void RemoveBody(BaseBody body)
    {
        if(sim.bodies.Count > 1)
        {
            BaseBody bodyToRemove = sim.bodies[sim.bodies.IndexOf(body)];

            sim.bodies.Remove(bodyToRemove);

            if(sim.bodyRelativeTo == bodyToRemove)
            {
                sim.bodyRelativeTo = sim.bodies[0];
            }

            if (camController.currentTracking == bodyToRemove)
            {
                camController.ChangeParent(0);
            }

            Destroy(body.gameObject);

            sim.UpdatePlanets();
        }
        else
        {
            //Should not remove last body.
        }
    }
}
