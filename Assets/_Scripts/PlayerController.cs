using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : Threat
{

    Rigidbody rb;
    public float maxMovespeed;
    // Start is called before the first frame update
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 vel = Vector2.zero;

        float x = Input.GetAxisRaw("Horizontal") * maxMovespeed;
        float z = Input.GetAxisRaw("Vertical") * maxMovespeed;

        vel.x = x;
        vel.z = z;
        transform.Translate(vel.normalized * maxMovespeed * Time.deltaTime);   
    }
}

