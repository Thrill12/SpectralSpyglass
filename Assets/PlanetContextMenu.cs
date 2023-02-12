using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

/// <summary>
/// Class to manage the context menu when right clicking a planet button
/// </summary>
public class PlanetContextMenu : MonoBehaviour
{
    // This index gets set whenever the object is instantiated so that the context menu knows which planet it is selected
    public int selectedBodyIndex;

    // Reference for the simulation script in order to access its variables and functions
    Simulation sim;
    // Camera controller reference
    CameraController con;
    // UI manager reference
    UIManager ui;

    private void Start()
    {
        // Setting the references for the different managers
        sim = GameObject.FindObjectOfType<Simulation>();
        con = GameObject.FindObjectOfType<CameraController>();
        ui = FindObjectOfType<UIManager>();

        // This filters out any functions which cannot be executed on "fake" bodies.
        if (sim.bodies[selectedBodyIndex].fake)
        {
            foreach (var item in GetComponentsInChildren<ContextButton>().Where(x => x.canAffectFakeBodies == false))
            {
                Destroy(item.gameObject);
            }
        }
    }

    /// <summary>
    /// Function that runs when clicking the button to "relativise", which sets the simulations bodyRelativeTo to the body selected
    /// </summary>
    public void RelativeToThis()
    {
        sim.bodyRelativeTo = sim.bodies[selectedBodyIndex];

        ContextMenuClick();
    }

    /// <summary>
    /// Simply changes which body the camera is observing
    /// </summary>
    public void ChangeToThis()
    {
        con.ChangeParent(selectedBodyIndex);
        con.freeCam = false;
        GetComponentInChildren<TMP_Text>().text = con.currentTracking.bodyName;

        ContextMenuClick();
    }

    /// <summary>
    /// Selects this new planet to see its properties in the window, but camera stays on the previous target.
    /// </summary>
    public void SelectProperties()
    {
        ui.observedBody = sim.bodies[selectedBodyIndex];
        sim.DeleteExistingOrbits();
        ContextMenuClick();
    }

    /// <summary>
    /// Sets the target in the UI manager, allowing the user to draw the target line and other things in the future
    /// </summary>
    public void SetTarget()
    {
        ui.targetBody = sim.bodies[selectedBodyIndex];
        ContextMenuClick();
    }

    /// <summary>
    /// This is a function that will get called in all of the functions, for example deleting the context menu after each click is essential
    /// </summary>
    public void ContextMenuClick()
    {
        Destroy(gameObject);
    }  

    /// <summary>
    /// Function for creating a snapshot of the selected planet, which is just a ghost stationary image of the current planet allowing us to measure things
    /// from a standing still point
    /// </summary>
    public void Snapshot()
    {
        sim.Snapshot(sim.bodies[selectedBodyIndex] as CelestialBody);
        ContextMenuClick();
    }

    /// <summary>
    /// Function for removing a body from the system.
    /// </summary>
    public void RemoveBody()
    {
        AddRemoveBody aRBody = FindObjectOfType<AddRemoveBody>();
        aRBody.RemoveBody(sim.bodies[selectedBodyIndex]);
        ContextMenuClick();
    }
}
