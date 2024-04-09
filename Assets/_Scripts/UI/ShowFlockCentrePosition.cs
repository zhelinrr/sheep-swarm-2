using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ShowFlockCentrePosition : MonoBehaviour
{
    [SerializeField]GameObject trackCentre;
    [SerializeField] TextMeshProUGUI tmpro;

    // Update is called once per frame
    void Update()
    {
        var v = new Vector2(trackCentre.transform.position.x, trackCentre.transform.position.z);
        tmpro.text = "Flock Centre Position: "  + v;
    }
}
