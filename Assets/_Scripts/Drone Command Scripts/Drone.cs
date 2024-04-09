using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;

public class Drone : Threat
{
    public Rigidbody rb;
    public DroneManager droneManager;
    public DroneParams droneParams;

    public Vector3 targetPoint;
    public Vector3 flockCentre;
    public Vector3 moveDir = Vector3.zero;

    [SerializeField] public bool useHighSpeed = false;
    [SerializeField] public bool avoidFlock;
    [SerializeField] Vector3 diff;
    [SerializeField] public bool lifting;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }
    private void Update()
    {

        rb.velocity = Vector3.zero;

        // move towards target position
        // may need to change the stop threshold 
        diff = targetPoint - transform.position;
        diff.y = 0;

        Vector3 minVec = Vector3.zero;
        var canMoveStraightChecked = CheckCanMoveStraight(out minVec);


        if (diff.magnitude > 0.5f)
        {

            // print("Can move straight " + avoidFlock);

            var speed = droneParams.highSpeed;

            if (droneManager.droneCommandState == DroneCommandState.Drive)
                speed = droneParams.lowSpeed;
            
            var deltaDist = speed * Time.deltaTime;

            moveDir.y = 0;
            moveDir = diff.normalized;

            //transform.position = Vector3.Lerp(transform.position, diff,  diff.magnitude / highSpeed);
            /*if (deltaDist <= diff.magnitude)
                transform.Translate(moveDir.normalized * deltaDist);
            else
                transform.Translate(diff);*/
            rb.AddForce(moveDir * speed, ForceMode.VelocityChange);

        }

        if (droneParams.useHeightVariaion)
        {
            // handles vertical movement
            Vector3 verticalMoveVec = Vector3.zero;
            float targetHeight = TargetHeight();
            float verticalDiff = targetHeight - transform.position.y;
            var verticalDeltaDist = droneParams.verticalSpeed * Time.deltaTime;
            if (Mathf.Abs(verticalDiff) > 0.3)
            {
                var vSpeed = droneParams.verticalSpeed;

                if (verticalDiff < 0)
                {
                    vSpeed *= -1;
                    verticalDeltaDist *= -1;
                }
                /*
                        if (verticalDiff <= diff.magnitude)
                            transform.Translate(new Vector3(0,  verticalDeltaDist, 0));
                        else
                            transform.Translate(new Vector3(0, verticalDiff, 0));
                */

                rb.AddForce(new Vector3(0, vSpeed, 0), ForceMode.VelocityChange);
                //transform.position = targetPoint;
            }
            else
            {

            }
        }
    }


    /*
    private Vector3 TangentialMoveDirection()
    {
        var flockCentre_ = new Vector3(flockCentre.x, 0, flockCentre.z);


        if ((flockCentre - transform.position).magnitude < droneParams.allowedDistOffset) {
            return transform.position - flockCentre;
        }
        
        var midPoint = targetPoint - transform.position;
        var midPointDir = midPoint - flockCentre;

        var farPoint = midPoint + midPoint.magnitude * midPointDir / 2;
        var output = farPoint - transform.position;
        return output;
        
    }
    */
    private bool CheckCanMoveStraight(out Vector3 minVec) {
        float allowedDist = droneManager.boundingRadius + droneParams.allowedDistOffset;

        Vector3 a = targetPoint - transform.position;
        Vector3 b = flockCentre - transform.position;
        float dot = Vector3.Dot(a, b);
        Vector3 intersection = targetPoint + dot * b;
        minVec = flockCentre - intersection;

        float dist = minVec.magnitude;
        return dist > allowedDist;
    }

    private float TargetHeight() {

        var hPos = transform.position;
        hPos.y = 0;
        var hPosTarget = targetPoint;
        hPosTarget.y = 0;

        var diff = hPosTarget - hPos;

        if (diff.magnitude < 10)
            return droneParams.chasingHeight;
        return droneParams.avoidingHeight;
     }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position, 1);
    }
}
