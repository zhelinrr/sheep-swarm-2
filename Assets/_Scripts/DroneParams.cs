using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DroneParams : MonoBehaviour
{
    public int numDrones;
    public int highSpeed;
    public int lowSpeed;
    public float verticalSpeed;
    public float avoidingHeight;
    public float chasingHeight;
    public float boundingRadius = 10;
    public float enclosingEpsilon;
    public int milestoneAvgNum = 1;
    public Vector3 droneBase;
    public Vector2 droneSpawnExtent;
    public float allowedDistOffset = 0;
    public float boundingRadiusProportion;
    public bool useHeightVariaion;
    public bool useFocusRemainingSheep;

}
