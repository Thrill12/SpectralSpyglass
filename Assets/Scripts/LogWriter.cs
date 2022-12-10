using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class LogWriter : MonoBehaviour
{
    public static LogWriter instance;

    private string path;
    private StreamWriter logFile;

    private void Awake()
    {
        instance = this;
        path = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments) + @"\CosmicSpyglass";

        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        string newPath = path + @"\CS " + DateTime.UtcNow.Hour + DateTime.UtcNow.Minute + DateTime.UtcNow.Second + ".txt";
        logFile = new StreamWriter(newPath, true);
    }

    //string newPath = path + @"\CS " + DateTime.UtcNow.Hour + DateTime.UtcNow.Minute + DateTime.UtcNow.Second;
    public void Log(string logString)
    {
        logFile.WriteLine(logString);
    }
}
