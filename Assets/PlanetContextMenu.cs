using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlanetContextMenu : MonoBehaviour
{
    public int selectedBodyIndex;

    Simulation sim;
    CameraController con;
    UIManager ui;

    private void Start()
    {
        sim = GameObject.FindObjectOfType<Simulation>();
        con = GameObject.FindObjectOfType<CameraController>();
        ui = FindObjectOfType<UIManager>();
    }

    public void RelativeToThis()
    {
        sim.bodyRelativeTo = sim.bodies[selectedBodyIndex];

        ContextMenuClick();
    }

    public void ChangeToThis()
    {
        con.ChangeParent(selectedBodyIndex);
        con.freeCam = false;
        GetComponentInChildren<TMP_Text>().text = con.currentTracking.bodyName;

        ContextMenuClick();
    }

    public void SelectProperties()
    {
        ui.observedBody = sim.bodies[selectedBodyIndex];

        ContextMenuClick();
    }

    public void ContextMenuClick()
    {
        Destroy(gameObject);
    }
}
