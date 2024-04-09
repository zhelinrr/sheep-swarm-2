using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] Flock flock;
    [SerializeField] Camera mainCamera;
    [SerializeField] GameObject player;

    [SerializeField] float minDistance, maxDistance;
    [SerializeField] float distanceFromGround;
    [SerializeField] float rotation;
    [SerializeField] float manualTranslateSpeed;
    [SerializeField] float manualRotateSpeed;
    [SerializeField] float manualScaleSpeed;
    [SerializeField] bool useManualControl;

    [SerializeField] Vector3 moveDelta;
    Vector3 lastMousePosition = Vector3.zero;

    // Start is called before the first frame update
    void Start()
    {
        mainCamera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        if (useManualControl) ManualControl();
        else // look at flock centre
        {

            Vector3 position = Vector3.zero;
            position.x = flock.FlockCentre.x;
            position.y = mainCamera.transform.position.y;
            position.z = flock.FlockCentre.z;

            mainCamera.transform.position = position;
        }

    }

    void PositionCamera() {
        Vector3 pos = mainCamera.transform.position;
        pos.y = distanceFromGround;
        mainCamera.transform.position = new Vector3();

        PointTowardsObjectsCenter();
    }

    void PointTowardsObjectsCenter() {
        Vector3 center = GetObjectsCenter();
        float maxDistance = GetMaxObjectDistanceFrom(center);
        // todo implement camera

        Ray cameraAlign = new Ray(center, new Vector3(45, 0, 0));
        mainCamera.transform.position = cameraAlign.GetPoint(maxDistance + 10);

        Debug.Log($"center {center}, maxdistance {maxDistance}, rayDistance {maxDistance + 10}, rayPoint {cameraAlign.GetPoint(maxDistance + 10)}");

    }

    float GetMaxObjectDistanceFrom(Vector3 position) {
        float d = 0;
        List<GameObject> collection = new List<GameObject>();
        collection.AddRange(flock.sheepHerd);
        collection.Add(player);

        foreach (var item in collection) {
            float dist = (item.transform.position - position).magnitude;
            if (dist > d)
                d = dist;
        }

        return d;
    }

    Vector3 GetObjectsCenter() {
        List<GameObject> all = new List<GameObject>();
        all.AddRange(flock.sheepHerd);
        all.Add(player);

        Vector3 sum = Vector3.zero;
        foreach (GameObject obj in all)
        {
            sum += obj.transform.position;
        }
        Vector3 avg = sum/all.Count;
        return avg;
    }

    private void ManualControl()
    {
        moveDelta = Vector3.zero;
        Vector3 viewDir = transform.rotation * Vector3.forward;
        Vector2 mouseMoveDelta = Input.mousePosition - lastMousePosition;

        /*        if (Input.GetKey(KeyCode.Mouse0)) {

                    float yawDelta = mouseMoveDelta.y * Time.deltaTime * manualRotateSpeed;
                    float pitchDelta = - mouseMoveDelta.x * Time.deltaTime * manualRotateSpeed;

                    transform.eulerAngles += new Vector3(yawDelta, pitchDelta, 0);
                }*/

        if (Input.GetKey(KeyCode.Mouse1))
        {
            moveDelta -= transform.up * mouseMoveDelta.y * Time.deltaTime * manualTranslateSpeed;
            moveDelta -= transform.right * mouseMoveDelta.x * Time.deltaTime * manualTranslateSpeed;
        }

        // zooming
        moveDelta += transform.rotation * Vector3.forward * Input.mouseScrollDelta.y * Time.deltaTime * manualScaleSpeed;
        transform.position += moveDelta;
        lastMousePosition = Input.mousePosition;
    }
}
