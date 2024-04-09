using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Main : MonoBehaviour
{
    public float elapsedTime = 0;

    private void Update()
    {
        elapsedTime += Time.deltaTime;
    }
}
