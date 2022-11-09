using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlanetButtonHolder : MonoBehaviour
{
    [HideInInspector] public int bodyIndex;

    public void ChangeToThis()
    {
        CameraController con = GameObject.FindObjectOfType<CameraController>();

        con.ChangeParent(bodyIndex);
        con.freeCam = false;
        GetComponentInChildren<TMP_Text>().text = con.currentTracking.bodyName;
    }
}
