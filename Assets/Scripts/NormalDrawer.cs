using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NormalDrawer : MonoBehaviour
{
    public Transform target1;
    public Transform target2;

    // Update is called once per frame
    void Update()
    {
        //Vector3 tangent = Vector3.Cross(target1.position, target2.position);
        Vector3 normal = target1.position - transform.position;
        Quaternion q = Quaternion.Euler(normal);
        Quaternion q1 = Quaternion.Euler(-normal);
        Quaternion q2 = Quaternion.RotateTowards(q, q1, 90);
        Vector3 tangent = q2.eulerAngles;
        Debug.DrawRay(transform.position, tangent);
    }
}
