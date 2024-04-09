using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UIElements;

public class DroneManager : MonoBehaviour
{

	[SerializeField] DroneParams droneParams;

	[SerializeField] List<Node> mileStones;
	[SerializeField] List<GameObject> droneObjects = new();
	[SerializeField] List<Drone> drones = new();

	[SerializeField] public float boundingRadius;

	public Grid grid;

	public int milestoneAvgNum = 1;

    public GameObject dronePrefab;
    public Flock flock;
	public DroneCommandState droneCommandState;


	private Vector3 flockCentre;

	// debug
	[SerializeField] List<Vector3> arcPositions = new();
	[SerializeField] Vector3 steeringDirection_;
	[SerializeField] List<Vector3> droneTargetPositions = new();

	public float ArcAngle { get { return (float)Math.PI * droneParams.numDrones / (droneParams.numDrones + 1); } }

	// Start is called before the first frame update
	void Start()
	{

		droneParams = GetComponent<DroneParams>();

		droneCommandState = DroneCommandState.Collect;
		Func<Vector3> randSpawnLoc = () =>
		{
			var div = new Vector2(UnityEngine.Random.Range(-droneParams.droneSpawnExtent.x / 2, droneParams.droneSpawnExtent.x / 2),
														UnityEngine.Random.Range(-droneParams.droneSpawnExtent.y / 2, droneParams.droneSpawnExtent.y / 2));
			return droneParams.droneBase + new Vector3(div.x, 0, div.y);
		};

		for (int i = 0; i < droneParams.numDrones; i++)
		{

			GameObject gobj = Instantiate(dronePrefab, transform);
			gobj.transform.name = $"Drone {i}";

			Vector3 pos = randSpawnLoc();
			gobj.transform.position = pos;

			Drone drone = gobj.GetComponent<Drone>();
			drone.droneParams = droneParams;
			drone.useHighSpeed = true;
			drone.droneManager = this;

			droneObjects.Add(gobj);
			drones.Add(drone);
		}
	}

	[SerializeField] float updateIntervalSecond = 0.5f;
	[SerializeField] float updateTimerSecond = 0;
	// Update is called once per frame
	void Update()
	{

		if (droneParams.useFocusRemainingSheep) {
			print(flock.sheepOutside.Count);
			flock.sheepHerd = flock.sheepOutside.ToArray();
        }

		if (flock.sheepHerd.Length == 0) {
			return;
		}

            updateTimerSecond += Time.deltaTime;
		if (updateTimerSecond < updateIntervalSecond) {
			return;
		}
		updateTimerSecond -= updateIntervalSecond;

		bool n = flock.NeedsRecollection();
		//print(n);

        if (n)
		{
			droneCommandState = DroneCommandState.Collect;
		}
		else {
			droneCommandState = DroneCommandState.Drive;
		}

		boundingRadius = flock.BoundingRadius(droneParams.boundingRadiusProportion);

		ApplyRules();

	}

	private void ApplyRules()
	{

		flockCentre = flock.FlockCentre;
        // issue command for every drone
        List<Vector3> dronePositions = new();
        //List<Vector3> droneTargetPositions = new();
         droneTargetPositions = new();

        for (int i = 0; i < drones.Count; i++)
		{
			dronePositions.Add(drones[i].transform.position);
		}

		if (grid.path == null) return;
        if (grid.path.Count == 0)return ;

		// determines herding direction
        mileStones = grid.path;
        var steeringDirection = Vector3.zero;

        if (mileStones.Count < milestoneAvgNum)
        {
            milestoneAvgNum = 1;
        }
        for(int i = 0; i < milestoneAvgNum; i++)
			{
            steeringDirection += mileStones[i].worldPosition - flock.FlockCentre;
        }

        if (droneCommandState == DroneCommandState.Drive)
		{
			

            droneParams.boundingRadius = flock.BoundingRadius(0.99f);
			var steeringDirection2d = new Vector2(steeringDirection.x, steeringDirection.z);

			arcPositions = GreedyDistanceAllocation(
				ArcForm(droneParams.boundingRadius, ArcAngle, steeringDirection2d, droneParams.numDrones, droneParams.enclosingEpsilon), 
				dronePositions);
			

			for (int i = 0; i < drones.Count; i++)
			{
				droneTargetPositions.Add(flock.FlockCentre + arcPositions[i]);// + droneParams.droneBase.y * Vector3.up);
				//Debug.Log($"drone {i}: {drones[i].targetPoint}");
			}
			// debug
			steeringDirection_ = steeringDirection;
		}

		else if (droneCommandState == DroneCommandState.Collect) {
			var sheeps = flock.SortFurthestByDistance(droneParams.numDrones);


			if (sheeps.Count < droneParams.numDrones) {
				for (int i = sheeps.Count; i < droneParams.numDrones; i++) {
					sheeps.Add(sheeps[sheeps.Count - 1]);
				}
			}
			for (int i = 0; i < sheeps.Count; i++)
			{
				var droneDirection = sheeps[i].transform.position - flockCentre;
				var horizontalDistance = Math.Sqrt(flock.swarmParams.threatDetectionDistance * flock.swarmParams.threatDetectionDistance - droneParams.droneBase.y * droneParams.droneBase.y);

                droneTargetPositions.Add(flockCentre + droneDirection.normalized * (droneDirection.magnitude + (float)horizontalDistance*0.95f));

				droneTargetPositions[i] = new Vector3(droneTargetPositions[i].x, droneParams.droneBase.y, droneTargetPositions[i].z);
			}
			
            droneTargetPositions = GreedyDistanceAllocation(droneTargetPositions, dronePositions);

		}

        for (int i = 0; i < drones.Count; i++)
        {
			drones[i].targetPoint = droneTargetPositions[i]; 

        }
    }
	
    public List<Vector3> ArcForm(float radius, float arcAngleRadians, Vector2 steerDirection2d, int numDrones, float epsilon) {

		//Debug.Log(steerDirection2d);
		Vector3 steerDirection = new Vector3 (steerDirection2d.x, 0, steerDirection2d.y);

		float diameterOffset = (float)(1 - arcAngleRadians / Math.PI);
		Vector3 startDirection = Vector3.Slerp(Vector3.Cross(steerDirection, Vector3.up), -steerDirection, diameterOffset);
		Vector3 endDirection = Vector3.Slerp(-Vector3.Cross(steerDirection, Vector3.up), -steerDirection, diameterOffset);

		float[] distr = new float[numDrones];
		Vector3[] directionDistr = new Vector3[numDrones];
		for (int i = 0; i < numDrones; i++) {
			distr[i] = (float)i / (numDrones - 1);
			directionDistr[i] = Vector3.Slerp(startDirection, endDirection, distr[i]).normalized;
			directionDistr[i] *=  radius + epsilon;
		}

		var output = new List<Vector3>();
		output.AddRange(directionDistr);
		return output;
	}

	public List<Vector3> GreedyDistanceAllocation(List<Vector3> targets, List<Vector3> droneLocations) {
		if (droneLocations.Count > targets.Count) {

			for (int i = targets.Count; i < droneLocations.Count; i++)
			{
				targets.Add(targets[targets.Count - 1]);
			}

		}
		/*        string targetsContent = "";
				foreach (var target in targets)
				{
					targetsContent += target.ToString() + "||";
				}
				print(targetsContent);*/


		print($"length targets: {targets.Count}, length drones: {droneLocations.Count}");

		List<Vector3> droneToLocation = new();
		if (targets.Count > 1)
			for (int i = 0; i < targets.Count; i++)
			{

				int currentTarget = -1;
				float sqrDistance = float.PositiveInfinity;

				for (int j = 0; j < targets.Count; j++)
				{
					if (droneToLocation.Contains(targets[j]))
					{
						continue;
					}

					float currentSqrDistance = (targets[j] - droneLocations[i]).sqrMagnitude;
					if (currentSqrDistance < sqrDistance)
					{
						currentTarget = j;
						sqrDistance = currentSqrDistance;
					}
				}
				if (currentTarget == -1) { break; }
				droneToLocation.Add(targets[currentTarget]);
			}
		else {
            droneToLocation = targets;
		}

		//print(droneToLocation.Count);
		if (droneToLocation.Count < droneParams.numDrones) {
            for (int i = droneToLocation.Count; i < droneParams.numDrones; i++)
            {
				droneToLocation.Add(droneLocations[droneLocations.Count - 1]);
            }
        }
		return droneToLocation;
	}

}

public enum DroneCommandState{ 
	Collect,
	Drive,
	DriveIntoPen
}
