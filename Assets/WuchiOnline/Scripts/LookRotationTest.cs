using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookRotationTest : MonoBehaviour
{

    public Transform target;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        UpdateRotation();
    }

    public void UpdateRotation()
    {
        Vector3 direction = (target.position - transform.position).normalized;

        // create the rotation we need to be in to look at the target
        Quaternion lookAtRotation = Quaternion.LookRotation(direction);

        Quaternion lookAtRotation_onlyY = Quaternion.Euler(transform.rotation.eulerAngles.x, lookAtRotation.eulerAngles.y, transform.rotation.eulerAngles.z);

        transform.rotation = lookAtRotation_onlyY;
    }
}
