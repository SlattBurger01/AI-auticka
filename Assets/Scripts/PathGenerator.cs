using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PathGenerator : MonoBehaviour
{
    public static void SetRotationOfPart(PathPart part, PathPart prevPart, bool start)
    {
        Transform endP = prevPart.start ? prevPart.endPos : prevPart.startPos;

        Quaternion q = start ? endP.rotation : part.endRot;

        part.transform.rotation = start ? q : q * endP.rotation;

        Vector3 vPos = start ? part.startPos.position : part.endPos.position;

        part.transform.position = endP.position + part.transform.position - vPos;

        part.start = start;
    }
}
