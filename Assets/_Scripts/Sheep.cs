using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Unity.PlasticSCM.Editor.WebApi;
using Unity.VisualScripting;
using UnityEditor.ShaderKeywordFilter;
using UnityEngine;
using Random = UnityEngine.Random;
using OpenCover.Framework.Model;
using UnityEditor;
using UnityEngine.Experimental.Rendering;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine.EventSystems;

public class Sheep : MonoBehaviour
{

    public Flock flock;
    public SwarmParams swarmParams;
    public GameObject aggregateVectorObject;

    public bool inPen;
    public float speed;
    public Vector3 direction;
    public bool isAvoidingObject;
    public float distanceToCentre;
    public float threatMomentum;

    //  available to see in inspector
    [SerializeField] Vector3 vCohesion = Vector2.zero;
    [SerializeField] Vector3 vSeparation = Vector2.zero;
    [SerializeField] Vector3 vAlignment = Vector2.zero;
    [SerializeField] Vector3 vObjectAvoidance = Vector2.zero;
    [SerializeField] Vector3 vPredatorAvoidance = Vector2.zero;
    [SerializeField] Vector3 vAggregate = Vector2.zero;
    [SerializeField] Vector3 vRandom = Vector2.zero;

    public Rigidbody rb;

    public Dictionary<string, List<GameObject>> objectsInVision = new();


    //debug
    [SerializeField] bool canSeeThreat;
    [SerializeField] bool underThreat;
    [SerializeField] Vector3 lastPosition;
    [SerializeField] float readSpeed;
    [SerializeField] List<GameObject> threatsInVisionInspection;
    [SerializeField] List<GameObject> sheepInVisionInspection;
    [SerializeField] List<Vector3> avoidanceRaysInspection;
    #region properties
    public Ray[] ObstacleAvoidanceRays
    {
        get {
            var dirs = new Vector3[] { 
                Vector3.forward, 
                Vector3.left,
                Vector3.right,
                new Vector3(Mathf.Cos(Mathf.PI/8), 0, Mathf.Sin(Mathf.PI/8)),
                new Vector3(Mathf.Cos(2*Mathf.PI/8), 0, Mathf.Sin(2*Mathf.PI/8)),
                new Vector3(Mathf.Cos(3*Mathf.PI/8), 0, Mathf.Sin(3*Mathf.PI/8)),
                new Vector3(Mathf.Cos(5*Mathf.PI/8), 0, Mathf.Sin(5*Mathf.PI/8)),
                new Vector3(Mathf.Cos(6*Mathf.PI/8), 0, Mathf.Sin(6*Mathf.PI/8)),
                new Vector3(Mathf.Cos(7*Mathf.PI/8), 0, Mathf.Sin(7*Mathf.PI/8)),
                new Vector3(Mathf.Cos(9*Mathf.PI/8), 0, Mathf.Sin(9*Mathf.PI/8)),
                new Vector3(Mathf.Cos(15*Mathf.PI/8), 0, Mathf.Sin(15*Mathf.PI/8)),
            };

            var rays = new Ray[dirs.Length];
            for (int i = 0; i < rays.Length; i++) {
                rays[i] = new Ray(transform.position, dirs[i]);
            }
            return rays;
        }
    }

    public bool FeelsThreatened
    {
        get { return canSeeThreat || underThreat || threatMomentum > 0; }
    }

    #endregion

    public override string ToString()
    {
            return gameObject.name + ": " + transform.position.ToString();
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        objectsInVision = new Dictionary<string, List<GameObject>>();
        objectsInVision["threat"] = new List<GameObject>();
        objectsInVision["sheep"] = new List<GameObject>();
        objectsInVision["sheepInRange"] = new List<GameObject>();

    }

    Vector3 SetYZero(Vector3 v) {
        return new Vector3(v.x, 0, v.z);
    }

    private void FixedUpdate()
    {
        threatMomentum -= Time.deltaTime;
        if (Random.Range(0f, 1f) < 0.1f)
        {
            ResetVision();
        }
        ApplyRules();
        vAggregate.y = 0;

        /*transform.rotation = Quaternion.Slerp(transform.rotation,
                                        Quaternion.LookRotation(SetYZero(vAggregate - transform.position)),
                                        swarmParams.maxRotationSpeed * Time.deltaTime);*/


        if (FeelsThreatened || threatMomentum > 0)
            vAggregate = vAggregate.normalized * swarmParams.maxSpeed;
        else
            vAggregate = vAggregate.normalized * swarmParams.lowSpeed;
        speed = vAggregate.magnitude;
        direction = vAggregate.normalized;

        var moveDirection = new Vector3(vAggregate.x, 0, vAggregate.z).normalized;
        var velocity = speed * moveDirection;
        rb.velocity = Vector3.zero;
        rb.AddForce(velocity, ForceMode.VelocityChange);

        if (moveDirection != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(moveDirection, Vector3.up);

        //debug 
        if (aggregateVectorObject != null)
        {
            aggregateVectorObject.SetActive(true);
            aggregateVectorObject.transform.position = vAggregate * 3;
        }
        readSpeed = (transform.position - lastPosition).magnitude / Time.fixedDeltaTime;
        lastPosition = transform.position;
    }

    int CompareCloserOfTwo(GameObject a, GameObject b)
    {

        if (a == null && b == null) return 0;
        if (a == null)
            if (b != null)
                return -1;
        if (b == null)
            return 1;

        float aDistance = (a.transform.position - transform.position).magnitude;
        float bDistance = (b.transform.position - transform.position).magnitude;

        if (aDistance < bDistance) return -1;
        if (aDistance > bDistance) return 1;
        return 0;
    }

    void ResetVision() { 
        objectsInVision["threat"] = new List<GameObject>();
        objectsInVision["sheep"] = new List<GameObject>();
        objectsInVision["sheepInRange"] = new List<GameObject>();
        Threat[] t = FindObjectsOfType<Threat>();
        canSeeThreat = false;
        foreach (var threat in t)
        {
            if ((transform.position - threat.transform.position).magnitude > swarmParams.threatDetectionDistance)
                continue;
            objectsInVision["threat"].Add(threat.gameObject);
            canSeeThreat = true;
        }


        List<GameObject> sheepInRange = flock.sheepHerd.ToList();
        sheepInRange.Remove(this.gameObject);

        List<GameObject> temp = sheepInRange.ConvertAll((item) => item);
        foreach (var thing in sheepInRange)
        {
            if ((thing.transform.position - transform.position).magnitude > swarmParams.sheepNeighborDistance)
                temp.Remove(thing);
        }
        sheepInRange = temp;
        objectsInVision["sheepInRange"] = sheepInRange;


        // adds closest sheep into sheep in vision
        //List<GameObject> sheep = flock.sheepHerd.ToList();
        List<GameObject> sheep = sheepInRange.ConvertAll((item) => item);
        sheep.Sort(CompareCloserOfTwo);
        List<GameObject> sheepsInVision = new List<GameObject>();
        if (sheep.Count > swarmParams.neighborNumber)
            sheepsInVision.InsertRange(0, sheep.GetRange(0, swarmParams.neighborNumber));
        else
            sheepsInVision.AddRange(sheep);

        objectsInVision["sheep"] =  sheepsInVision;
        // debug
        threatsInVisionInspection = objectsInVision["threat"];
        sheepInVisionInspection = objectsInVision["sheep"];
    }

    void ApplyRules()
    {

        foreach (var neighbour in objectsInVision["sheep"]) {
            if (neighbour.GetComponent<Sheep>().canSeeThreat)
            {
                underThreat = true;
                threatMomentum = Random.Range(0f, swarmParams.maxThreatMomentum);
            }
            else underThreat = false;
        }
        vCohesion = Vector2.zero;
        vSeparation = Vector2.zero;
        vAlignment = Vector2.zero;
        vObjectAvoidance = Vector2.zero;
        vPredatorAvoidance = Vector2.zero;
        vAggregate = Vector2.zero;

        if (flock.sheepHerd.Length < 0) return;

        Vector3 vCentre = Vector3.zero;
        int numSheep = 0;
        int numSheepSeparation = 0;
        Vector3 sumDirection = Vector3.zero;

        // 3 boid forces
        foreach (var sheep in flock.sheepHerd)
        {
            if (numSheep >= swarmParams.neighborNumber) break;
            if (sheep.Equals(gameObject)) continue;

            float distance = (sheep.transform.position - transform.position).magnitude;

            if (distance > swarmParams.sheepNeighborDistance) continue;
            numSheep++;
            vCentre += sheep.transform.position;
            if (distance < swarmParams.separationDistanceBoid)
            {
                if(FeelsThreatened)
                {

                    vSeparation -= swarmParams.separationDistanceUnderThreat /
                        (swarmParams.separationDistanceUnderThreat - distance + 1) *
                        (sheep.transform.position - transform.position).normalized;
                    numSheepSeparation++;
                } else {

                    vSeparation -= swarmParams.separationDistanceBoid /
                        (swarmParams.separationDistanceBoid - distance + 1) *
                        (sheep.transform.position - transform.position).normalized;
                    numSheepSeparation++;
                }
            }

            sumDirection += sheep.GetComponent<Sheep>().direction;
        }
        if (numSheep != 0)  vCentre /= numSheep;
            else vCentre = transform.position;


        if (!FeelsThreatened)
            vCohesion = swarmParams.cohesionWeightBoid * (vCentre - transform.position).normalized;
        else
            vCohesion = swarmParams.cohesionWeightUnderThreat * (vCentre - transform.position).normalized;

        if (numSheepSeparation != 0)
            vSeparation /= numSheepSeparation;
        if (FeelsThreatened)
            vSeparation = vSeparation * swarmParams.separationWeightUnderThreat;
        else
            vSeparation = vSeparation * swarmParams.separationWeightBoid;

        vAlignment = sumDirection.normalized; 
        if (FeelsThreatened)
            vAlignment = vAlignment * swarmParams.alignmentWeightUnderThreat;
        else
            vAlignment = vAlignment * swarmParams.alignmentWeightBoid;

        //threat avoidance
        int numPredators = 0;
        if (objectsInVision.ContainsKey("threat")) {
            foreach (var threat in objectsInVision["threat"])
            {
                var diff = threat.transform.position - transform.position;
                var mag = diff.magnitude;
                vPredatorAvoidance -=  diff;
                numPredators++;
            }
        }
        if (numPredators > 0)
        {
            vPredatorAvoidance = vPredatorAvoidance * swarmParams.predationAvoidanceWeightBoid;
        }

        vObjectAvoidance = Vector3.zero;

        avoidanceRaysInspection = new List<Vector3>();
        // obstacle avoidance behaviour

        if (Physics.Raycast(transform.position, transform.forward, 
            swarmParams.obstacleAvoidanceCheckDistance, 
            LayerMask.GetMask("obstacle")))
        {
            isAvoidingObject = true;
            Vector3 sumMoveVec = Vector3.zero;
            foreach (Ray ray in ObstacleAvoidanceRays)
            {
                if (!Physics.Raycast(transform.position, ray.direction,
                    swarmParams.obstacleAvoidanceCheckDistance, 
                    LayerMask.GetMask("obstacle")))
                {
                    sumMoveVec = ray.direction;
                    avoidanceRaysInspection.Add(ray.direction);
                    break;
                }
            }
            vObjectAvoidance = swarmParams.obstacleAvoidanceWeightBoid * sumMoveVec.normalized;
        }
        else {
            isAvoidingObject = false;
        }

        Vector3 vRandomMovement = Vector3.zero;
        if (!FeelsThreatened && Random.Range(0f, 1f) < swarmParams.randomMovementChance)
            vRandomMovement = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)) * swarmParams.randomMovementWeight;
        if(FeelsThreatened && Random.Range(0f, 1f) < swarmParams.randomMovementChanceUnderThreat)
            vRandomMovement = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)) * swarmParams.randomMovementWeightUnderThreat;
        vRandom = vRandomMovement;


        vAggregate = vCohesion + vSeparation + vAlignment + vObjectAvoidance + vPredatorAvoidance + vRandomMovement;

        vAggregate.y = transform.position.y;
    }

}

