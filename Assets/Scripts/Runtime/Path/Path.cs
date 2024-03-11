using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Path : CarCollidable
{
    [SerializeField] private bool disableCheckpoints;

    [HideInInspector] public Transform staticCamPos;
    [HideInInspector] public float staticCamSize = 20;

    private Transform[] lavaPath;

    [SerializeField] private Transform lava;

    [SerializeField] private float lavaSpeed = 2;

    private int curPathPos;

    private Vector3 startPos;

    [SerializeField] private float startLavaWaitTime = 3;

    public bool selfUpdate = false; // debug option, otherwise updated via handler, if handler is active & this is target path, this option will be disabled

    public Checkpoint[] points; // first point is meant to be behind car spawn point

    private bool showCheckpoints => Handler.handler.displayCheckpoints;

    public Transform carSpawnPoint;

    private PathPart[] parts;

    [SerializeField] private Transform pathParent;

    [Header("")]
    public Checkpoint firstCheckpoint;
    [SerializeField] private Checkpoint lastCheckpoint;

    protected override void OnCollisionWithCarEnter(Car car) => car.OnCollisionWithWall();

    private PathPart[] GetPathParts() => GetComponentsInChildrenOrdered<PathPart>(pathParent);

    private Checkpoint[] GetCheckpoints()
    {
        List<Checkpoint> points = new();

        Vector3 recentPos = firstCheckpoint.transform.position;

        for (int i = 0; i < parts.Length; i++)
        {
            points.AddRange(parts[i].GetPoints(recentPos));

            recentPos = points.Last().transform.position;
        }

        points.Add(lastCheckpoint);

        return points.ToArray();
    }

    /// <returns> Array of components ordered based on hierarchy </returns>
    public static T[] GetComponentsInChildrenOrdered<T>(Transform t) where T : Component
    {
        if (!t) Debug.LogError($"Transform cannot be null");

        List<T> comps = new List<T>();

        for (int i = 0; i < t.childCount; i++)
        {
            T part = t.GetChild(i).GetComponent<T>();

            if (part) comps.Add(part);
        }

        return comps.ToArray();
    }

    private Transform[] GetLavaPath()
    {
        List<Transform> newPath = new List<Transform>();

        Vector3 recentPos = lava.position;

        for (int i = 0; i < parts.Length; i++)
        {
            newPath.AddRange(parts[i].GetLavaPath(recentPos));

            recentPos = newPath.Last().position;
        }

        return newPath.ToArray();
    }

    private void Start()
    {
        if (!selfUpdate) return;
        InializePath();
    }

    public void UpdateConeFields()
    {
        ConeField[] fields = GetComponentsInChildren<ConeField>();

        for (int i = 0; i < fields.Length; i++)
        {
            fields[i].ChangeCones();
        }
    }

    private void CreateAndCenterStaticCameraPos()
    {
        staticCamPos = new GameObject().transform;
        staticCamPos.SetParent(transform);
        staticCamPos.name = "StaticCameraPosition";

        Transform[] trs = pathParent.GetComponentsInChildren<Transform>();

        Vector2 max = Vector2.one * float.MinValue;
        Vector2 min = Vector2.one * float.MaxValue;

        for (int i = 0; i < trs.Length; i++)
        {
            float x = trs[i].position.x;
            float y = trs[i].position.y;

            if (x > max.x) max.x = x;
            if (x < min.x) min.x = x;

            if (y > max.y) max.y = y;
            if (y < min.y) min.y = y;
        }

        float distance = Mathf.Abs(max.y - min.y);

        staticCamSize = distance / 1.8f;
        staticCamPos.position = new Vector2((min.x + max.x) / 2, (min.y + max.y) / 2);
    }

    private void TryInializeLava()
    {
        if (!lava) return;

        lava.GetComponent<SpriteRenderer>().sortingOrder = Handler.lavaOrderInLayerOrder;
        lava.gameObject.layer = 2;

        lavaDefRot = lava.localRotation;
        startPos = lava.transform.position;

        lavaPath = GetLavaPath();

        ResetAndStartLava();
    }

    public void InializePath()
    {        
        CreateAndCenterStaticCameraPos();

        parts = GetPathParts();

        TryInializeLava();

        TrySetupCheckpoints();
    }

    private void TrySetupCheckpoints()
    {
        if (disableCheckpoints) return;
        
        points = GetCheckpoints();

        firstCheckpoint.gameObject.SetActive(false);
        lastCheckpoint.gameObject.SetActive(false);

        // setup checkpoints
        for (int i = 0; i < points.Length; i++)
        {
            points[i].id = i;
            points[i].Show(points[i].visible && showCheckpoints);

            if (i != points.Length - 1) points[i].next = points[i + 1];

            if (i != 0 && i != 1)
            {
                points[i].localScore = Vector2.Distance(points[i].transform.position, points[i - 1].transform.position);
                points[i].totalScore = points[i].localScore + points[i - 1].totalScore;
            }
            else if (i != 0)
            {
                points[i].localScore = Vector2.Distance(points[i].transform.position, carSpawnPoint.transform.position);
                points[i].totalScore = points[i].localScore;
            }
            else
            {
                points[i].localScore = -Car.ScoreCalculation(0, Vector2.Distance(points[i + 1].transform.position, carSpawnPoint.transform.position));
                points[i].totalScore = points[i].localScore;
            }
        }
    }

    private float curWaitTime;

    private void Update()
    {
        if (!selfUpdate) return;
        UpdateLava();
    }

    private PathPart curPath = null;

    public void UpdateLava()
    {
        if (lavaPath == null) return;
        if (curPathPos >= lavaPath.Length) return;

        if (curWaitTime > 0)
        {
            curWaitTime -= Time.deltaTime;
            return;
        }

        if (Vector2.Distance(lava.transform.position, lavaPath[curPathPos].position) > .001f)
        {
            UpdateLavaPosition();
        }
        else
        {
            curPathPos++;
            curPath = lavaPath[curPathPos].GetComponentInParent<PathPart>();

            if (curPathPos < lavaPath.Length) angleDif = GetAngleDistance(lava.rotation.eulerAngles.z, lavaPath[curPathPos].rotation.eulerAngles.z);

            UpdateLavaPosition();
        }
    }

    private void UpdateLavaPosition()
    {
        float mult = curPath ? curPath.lavaSpeedMult : 1;
        float deltaSpeed = Time.deltaTime * lavaSpeed * mult;

        Vector3 pos = Vector3.MoveTowards(lava.position, lavaPath[curPathPos].position, deltaSpeed);
        Quaternion rot = Quaternion.RotateTowards(lava.rotation, lavaPath[curPathPos].rotation, deltaSpeed * angleDif * .6f);

        lava.SetPositionAndRotation(pos, rot);

        lava.localScale = Vector3.MoveTowards(lava.localScale, MultiplayVectors(defaultLavaSize, lavaPath[curPathPos].localScale), deltaSpeed * 2);
    }

    private float angleDif;

    public static float GetAngleDistance(float angle1, float angle2)
    {
        float angleDifference = Mathf.Abs(angle1 - angle2) % 360;
        float distance = angleDifference > 180 ? 360 - angleDifference : angleDifference;
        return distance;
    }

    private static Vector3 MultiplayVectors(Vector3 a, Vector3 b)
    {
        return new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
    }

    private static readonly Vector3 defaultLavaSize = new Vector3(3.25f, .15f, 1);

    private Quaternion lavaDefRot;

    public void ResetAndStartLava()
    {
        if (!lava) return;

        //print("reset");

        lava.transform.position = startPos;
        lava.localRotation = lavaDefRot;
        lava.transform.localScale = defaultLavaSize;

        curWaitTime = startLavaWaitTime;

        curPathPos = 0;
    }
}
