using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

public class InPenDataRecorder : MonoBehaviour
{
    [SerializeField] float dataCollectionInterval;
     float dataCollectionTimer;
    [SerializeField] List<Sheep> sheepInPen;
    [SerializeField] List<PenDataPoint> penDataPoints;
    [SerializeField] Dictionary<string, PenDataPoint> specialPoints;
    [SerializeField] Dictionary<string, float> specialPointsDef;
    [SerializeField] bool startedData;
    [SerializeField] float sheepInPercentage;

    Main main;
    SwarmParams sheepParams;

    private void Awake()
    {
        specialPointsDef = new Dictionary<string, float>();
        specialPointsDef["first"] = 0;
        specialPointsDef["50%"] = 0.5f;
        specialPointsDef["90%"] = 0.9f;
        specialPointsDef["99%"] = 0.99f;
    }

    private void Start()
    {
        main = FindFirstObjectByType<Main>();
        sheepParams = FindFirstObjectByType<SwarmParams>();
        sheepInPen = new List<Sheep>();
        penDataPoints = new List<PenDataPoint>();
        specialPoints = new Dictionary<string, PenDataPoint>();
    }

    private void Update()
    {
        PrintSpecialData();
        //print(specialPoints.Count);
        if (!startedData) return;

        dataCollectionTimer += Time.deltaTime;
        if (dataCollectionTimer > dataCollectionInterval) {
            dataCollectionTimer -= dataCollectionInterval;
            penDataPoints.Add(new PenDataPoint()
            {
                time = main.elapsedTime,
                number = sheepInPen.Count
            });
            
        }

    }

    void OnTriggerEnter(Collider c) {
        if (!c.CompareTag("Sheep")) return;

        print("Sheep Enter");
        if (!startedData) startedData = true;
        Sheep sheep = c.GetComponent<Sheep>();
        sheep.inPen = true;
        if (sheepInPen.Contains(sheep)) { 
            return;
        }

        sheepInPen.Add(sheep);
        if (penDataPoints.Count > 0) {
            if (!specialPoints.Keys.Contains("first"))
                specialPoints["first"] = new PenDataPoint()
                {
                    time = main.elapsedTime,
                    number = sheepInPen.Count
                };
            startedData = true;
        }
        sheepInPercentage = (float)sheepInPen.Count / (float)sheepParams.sheepNum;

        foreach (var v in specialPointsDef) {

            if (sheepInPercentage >= v.Value && !specialPoints.Keys.Contains(v.Key)) {
                specialPoints[v.Key] = new PenDataPoint()
                {
                    time = main.elapsedTime,
                    number = penDataPoints.Count
                };
            }
        }

    }
    void OnTriggerExit(Collider c)
    {
        if (!c.CompareTag("Sheep")) return;

        Sheep sheep = c.GetComponent<Sheep>();
        if (sheepInPen.Contains(sheep))
        {
            return;
        }

        sheepInPen.Remove(sheep);
    }

    private void PrintSpecialData()
    {
        string s = "";
        foreach (var v in specialPoints) {
            s += $" [{v.Key}:{v.Value.time}]";
        }
        print(s);
    }

}

public struct PenDataPoint {
    public float time;
    public int number;

}