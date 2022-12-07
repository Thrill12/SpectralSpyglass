using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToDoList : MonoBehaviour
{
    public List<ToDoElement> ToDos;
}

[System.Serializable]
public class ToDoElement
{
    [TextArea]
    public string description;
    public bool done;
}
