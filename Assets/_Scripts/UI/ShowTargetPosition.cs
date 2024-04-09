using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ShowTargetPosition : MonoBehaviour
{

    [SerializeField] GameObject target;
    [SerializeField] TextMeshProUGUI textMeshPro;

    // Update is called once per frame
    void Update()
    {
        var v = new Vector2(target.transform.position.x, target.transform.position.z);
        textMeshPro.text = "Pen position " + v;
    }
}
