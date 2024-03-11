using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

public class PathPart : MonoBehaviour
{
    [SerializeField] private string comment;

    [Header("Transform")]
    public Transform startPos;
    [SerializeField] private float startRotZ;
    public Quaternion startRot => Quaternion.Euler(0, 0, startRotZ);

    public Transform endPos;
    [SerializeField] private float endRotZ;
    public Quaternion endRot => Quaternion.Euler(0, 0, endRotZ);

    [Header("Other")]
    public float lavaSpeedMult = 1;

    public Transform[] lavaPath;

    public Checkpoint[] checkpoints;

    public bool start = true;

    /// <summary> Get path no matter what rotation the piece has </summary>
    public Transform[] GetLavaPath(Vector3 relativeTo) => GetStuff<Transform>(lavaPath, relativeTo, true);

    public Checkpoint[] GetPoints(Vector3 relativeTo)
    {
        List<Checkpoint> points = GetStuff<Checkpoint>(checkpoints, relativeTo, false).ToList();

        int id = 0;

        foreach (Checkpoint point in points.ToList())
        {
            if (!point.gameObject.activeSelf) points.Remove(point);
            else point.name = $"{this.name} - point {id++}";
        }

        if (points.Count > 0)
        {
            points[0].gameObject.SetActive(false); // first checkpoint is meant to be disabled
            points.RemoveAt(0);
        }

        return points.ToArray();
    }

    public void DisableLocalCheckpoints()
    {
        for (int i = 0; i < checkpoints.Length; i++)
        {
            checkpoints[i].gameObject.SetActive(false);
        }
    }

    private static T[] GetStuff<T>(T[] array, Vector3 relativeTo, bool updateRotation) where T: Component
    {
        if (array.Length < 2) return array;

        float d1 = Vector3.Distance(relativeTo, array[0].transform.position);
        float d2 = Vector3.Distance(relativeTo, array[^1].transform.position);

        // distances should never be same (I hope)
        if (d1 < d2) return array;
        else
        {
            T[] invertedPath = new T[array.Length];

            for (int i = invertedPath.Length - 1; i >= 0; i--)
            {
                invertedPath[^(i + 1)] = array[i];

                if (updateRotation)
                {
                    Quaternion r = array[i].transform.rotation;

                    Quaternion newRot = Quaternion.Euler(r.eulerAngles.x, r.eulerAngles.y, r.eulerAngles.z + 180);

                    array[i].transform.rotation = newRot;
                }
            }

            return invertedPath;
        }
    }

    private bool c = false;

    // on first trigger (on any checkpoint) with any car
    public void OnCarEnter()
    {
        if (c) return;

        onCarEnterAction.Invoke();

        c = true;
    }

    public Action onCarEnterAction = delegate { };

    [SerializeField] private Transform[] conesPos;

    public void SpawnRandomCones()
    {
        for (int i = 0; i < conesPos.Length; i++)
        {
            if (UnityEngine.Random.Range(0, 2) == 1) continue;

            GameObject clone = Instantiate(Handler.handler.conePrefab, conesPos[i]);
            clone.transform.localPosition = Vector3.zero;
        }
    }
}
