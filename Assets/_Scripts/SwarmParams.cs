using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SwarmParams : MonoBehaviour
{

    public float maxSpeed = 1.0f;
    public float lowSpeed = 1f;
    public int sheepNum = 10;
    public int threatDetectionDistance; 
    public int sheepNeighborDistance;

    public int neighborNumber = 4;

    public float cohesionWeightBoid = 1;
    public float alignmentWeightBoid = 1;
    public float separationWeightBoid = 1; 
    public float cohesionWeightUnderThreat = 1;
    public float alignmentWeightUnderThreat = 1;
    public float separationWeightUnderThreat = 1;
    public float separationDistanceBoid;
    public float separationDistanceUnderThreat;
    public float obstacleAvoidanceWeightBoid = 1;
    public float obstacleAvoidanceCheckDistance;
    public float predationAvoidanceWeightBoid = 1;
    public float randomMovementWeight;
    public float randomMovementChance;
    public float randomMovementWeightUnderThreat;
    public float randomMovementChanceUnderThreat;
    public float maxThreatMomentum;
}
