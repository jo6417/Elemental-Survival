using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JointMaker : MonoBehaviour
{
    public List<GameObject> allBones = new List<GameObject>();

    [ContextMenu("CreateJoints")]
    public void CreateJoints()
    {
        Rigidbody2D preRigid = null;

        for (int i = 0; i < transform.childCount; i++)
        {
            if (preRigid != null)
                transform.GetChild(i).GetComponent<HingeJoint2D>().connectedBody = preRigid;

            preRigid = transform.GetChild(i).GetComponent<Rigidbody2D>();
        }
    }
}
