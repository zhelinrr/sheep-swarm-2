using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ShowTimeElapsed : MonoBehaviour
{
    float time;
    [SerializeField] TextMeshProUGUI textMeshPro;

    // Update is called once per frame
    void Update()
    {
        time += Time.deltaTime;
        textMeshPro.text = "Time Elapsed: " + time;
    }
}
