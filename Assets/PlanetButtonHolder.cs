using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlanetButtonHolder : MonoBehaviour, IPointerClickHandler
{
    [HideInInspector] public int bodyIndex;

    public void OnPointerClick(PointerEventData eventData)
    {
        if(eventData.button == PointerEventData.InputButton.Left)
        {
            FindObjectOfType<CameraController>().ChangeParent(bodyIndex);
            FindObjectOfType<CameraController>().freeCam = false;
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            if(FindObjectOfType<UIManager>().spawnedContextMenu == null)
            {
                GameObject context = Instantiate(FindObjectOfType<UIManager>().contextMenuPrefab, Input.mousePosition, Quaternion.identity);
                context.transform.parent = FindObjectOfType<Canvas>().transform;
                context.transform.position = Input.mousePosition;
                context.GetComponent<PlanetContextMenu>().selectedBodyIndex = bodyIndex;

                FindObjectOfType<UIManager>().spawnedContextMenu = context;
            }
            else
            {
                Destroy(FindObjectOfType<UIManager>().spawnedContextMenu.gameObject);
            }
            
        }
    }
}
