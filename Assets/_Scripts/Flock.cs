using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.Networking.PlayerConnection;
using UnityEngine;

public class Flock : MonoBehaviour
{

     public GameObject[] sheepHerd;
    public List<GameObject> sheepOutside;
    //[SerializeField] List<GameObject> sheepInVision;
    [SerializeField] public SwarmParams swarmParams;

    [SerializeField] Bounds spawnBounds;
    [SerializeField] GameObject sheepPrefab;

    public GameObject centreObject;

    // debug
    [SerializeField] Vector3 _flockCenter;

    public Vector3 FlockCentre {
        get
        {
            if (sheepHerd.Length == 0) return Vector3.zero;

            Vector3 centre = Vector3.zero;
            for (int i = 0; i < sheepHerd.Length; i++)
            {
                centre += sheepHerd[i].transform.position;
            }
            centre /= sheepHerd.Length; 
            for (int i = 0; i < sheepHerd.Length; i++)
            {
                Sheep s = sheepHerd[i].GetComponent<Sheep>();
                s.distanceToCentre = (centre - s.transform.position).magnitude;
            }
            return centre;
         }
    }


    public List<GameObject> SortFurthestByDistance(int length) {
        if (sheepHerd.Length < length) return sheepHerd.ToList();
        List<GameObject> gos = new();
        for (int i = 0; i < length; i++) gos.Add(sheepHerd[i]);
        gos.Sort((a, b) =>
        {
            if (a.GetComponent<Sheep>().distanceToCentre > b.GetComponent<Sheep>().distanceToCentre) {
                return -1;
            } else return 1;

        });

        for (int i = length; i< sheepHerd.Length; i++) 
        {
            for (int j = 0; j< length; j++)
            {
                if (sheepHerd[i].GetComponent<Sheep>().distanceToCentre > gos[j].GetComponent<Sheep>().distanceToCentre) {
                    for (int k = length - 1; k >= j; k--)
                    {
                        if (k != 0) gos[k] = gos[k - 1];
                    }
                    gos[j] = sheepHerd[i];
                    break;
                }
            }
        }
        return gos;

    }
    public GameObject ClosestToCentre()
    {
        if (sheepHerd.Length == 0) { 
            return FindObjectOfType<Sheep>().gameObject;
        }
        GameObject closest = sheepHerd[0];
        foreach (GameObject gobj in sheepHerd)
        {
            if (gobj.GetComponent<Sheep>().distanceToCentre < closest.GetComponent<Sheep>().distanceToCentre)
            {
                closest = gobj;
            }
        }
        return closest;
    }

    public float BoundingRadius(float ratio = 1f) {
        float currentRatio = 0;
        float currentRadius = 0;

        var flockCentre = FlockCentre;
        //sort by distance  to the center
        
        List<GameObject> sheepList = new List<GameObject>(sheepHerd);
        sheepList.Sort(CompareCloserToCentreOfTwo);
        for (int i = 0; i < sheepList.Count; i++) { 
            currentRatio = i / sheepList.Count;
            currentRadius = Vector3.Distance(sheepList[i].transform.position, flockCentre);
        }

        return currentRadius  ;
    }

    public List<SheepSubgroup> Subgrouping() {

        List<SheepSubgroup> sheepSubgroups = new List<SheepSubgroup>();

        List<Sheep> closed = new();
        List<Sheep> open = new();

        List<Sheep> pending = sheepHerd.ToList().ConvertAll((item) => item.GetComponent<Sheep>());

        //print(pending.Count);

        foreach (var sheep in pending) {
            if (closed.Contains(sheep)) continue;
            List<Sheep> connected = FindConnected(sheep);
            closed.AddRange(connected);
            sheepSubgroups.Add(new SheepSubgroup(connected, sheepHerd.Length));
        }

        string s = "";
        foreach (var g in sheepSubgroups) {
            s += g.ToString() + "; " ;
        }
        //print(s);

        return sheepSubgroups;
    }

    List<Sheep> FindConnected(Sheep sheep) {

        List<Sheep> closed = new();
        List<Sheep> open = new();


        open.Add(sheep);

        ScanRecursive(sheep, open, closed);
        return closed;
    }

    // Returns true if not all sheep are connected to central-most sheep
    // returns false if there is at least 1 sheep that are not connected to the centre sheep
    public bool NeedsRecollection() {

        List<Sheep> closed = new();
        List<Sheep> open = new();
        Sheep current = ClosestToCentre().GetComponent<Sheep>();
        open.Add(current);

        ScanRecursive(current, open, closed); 

        return !(closed.Count == sheepHerd.Length);
        
    }

    private void ScanRecursive(Sheep current, List<Sheep> open, List<Sheep> closed) {

        open.Remove(current);

        var connected = current.objectsInVision["sheepInRange"];
        foreach (var obj in connected)
        {
            if (!open.Contains(obj.GetComponent<Sheep>()) && !closed.Contains(obj.GetComponent<Sheep>())) {
                open.Add(obj.GetComponent<Sheep>());
            }
        }

        closed.Add(current);
        if (open.Count == 0) return;
        ScanRecursive (open[0], open, closed);

    }

    int CompareCloserToCentreOfTwo(GameObject a, GameObject b)
    {

        if (a == null && b == null) return 0;
        if (a == null)
            if (b != null)
                return -1;
        if (b == null)
            return 1;
        var aDist = (a.transform.position - FlockCentre).sqrMagnitude;
        var bDist = (b.transform.position - FlockCentre).sqrMagnitude;

        if (aDist < bDist) return -1;
        if (aDist > bDist) return 1;
        return 0;
    }
    // Start is called before the first frame update
    void Start()
    {
        sheepHerd = new GameObject[swarmParams.sheepNum];

        for (int i = 0; i < swarmParams.sheepNum; i++)
        {
            GameObject gobj = Instantiate(sheepPrefab, transform);
            gobj.transform.name = $"Sheep {i}";

            Vector3 pos = Random2dPositionInStartBound;
            //print(pos);
            gobj.transform.position = pos;
            sheepHerd[i] = gobj;

            Sheep sheep = gobj.GetComponent<Sheep>();
            sheep.flock = this;
            sheep.swarmParams = swarmParams;
        }
        sheepOutside = sheepHerd.ToList();
    }


    [SerializeField] float subgroupingInterval;
    [SerializeField] float subgroupingTimer;
    // Update is called once per frame
    void Update()
    {

        sheepOutside = new List<GameObject>();
        foreach (var gob in sheepHerd)
        {
            if (!gob.GetComponent<Sheep>().inPen) { 
                sheepOutside.Add(gob);
            }
        }

        subgroupingTimer += Time.deltaTime;
        centreObject.transform.position = FlockCentre;
        if(subgroupingTimer > subgroupingInterval)
        {
            Subgrouping();
            subgroupingTimer -= subgroupingInterval;
        }

        // debug
        _flockCenter = FlockCentre;
    }

    private Vector3 Random2dPositionInStartBound
    {
        get {
            return new Vector3(UnityEngine.Random.Range(spawnBounds.min.x, spawnBounds.max.x),
                                                   transform.position.y,
                                                   UnityEngine.Random.Range(spawnBounds.min.z, spawnBounds.max.z)
                                                   );
        }
    }


}
public class SheepSubgroup
{
    public List<Sheep> list = new();
    public float percentage;

    public SheepSubgroup(List<GameObject> sheepList, int totalSheepNum)
    {
        foreach (GameObject b in sheepList)
        {
            list.Add(b.GetComponent<Sheep>());
        }
        percentage = (float)list.Count / (float)totalSheepNum;
    }
    public SheepSubgroup(List<Sheep> sheepList, int totalSheepNum)
    {
        foreach (Sheep b in sheepList)
        {
            list.Add(b);
        }
        percentage = list.Count / totalSheepNum;
    }

    public override string ToString() {
        return $"subgroup size: {{ {list.Count}, {percentage} }}";
    }

    public Vector3 GroupCenter {
        get
        {
            if (list.Count == 0) return Vector3.zero;

            Vector3 centre = Vector3.zero;
            for (int i = 0; i < list.Count; i++)
            {
                centre += list[i].transform.position;
            }
            centre /= list.Count;
            for (int i = 0; i < list.Count; i++)
            {
                Sheep s = list[i].GetComponent<Sheep>();
                s.distanceToCentre = (centre - s.transform.position).magnitude;
            }
            return centre;
        }
    }

    public static List<SheepSubgroup> SortGroupsBySize(in List<SheepSubgroup> groups)
    {
        List<SheepSubgroup> g = new List<SheepSubgroup>();
        groups.Sort(CompareSize);
        return groups;
    }

    static int CompareSize(SheepSubgroup a, SheepSubgroup b)
    {
        if (a == null && b == null) return 0;
        if (a == null)
            if (b != null)
                return -1;
        if (b == null)
            return 1;

        float anum = a.list.Count;
        float bnum = b.list.Count;

        if (anum < bnum) return -1;
        if (anum > bnum) return 1;
        return 0;

    }
}