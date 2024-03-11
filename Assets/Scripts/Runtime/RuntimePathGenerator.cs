using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class RuntimePathGenerator : MonoBehaviour
{
    private List<PathPart> path = new();

    [SerializeField] private int maxPartCount = 3;
    [SerializeField] private PartsDatabase database;
    [SerializeField] private PathPart startPart;

    private PathPart prevPart;

    [SerializeField] private bool obstacleCones;

    private void Start()
    {
        prevPart = startPart;
        path.Add(startPart);

        CreateNewPart();
        CreateNewPart();
    }

    /*private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            CreateNewPart();
        }
    }*/

    private void CreateNewPart()
    {
        prevPart = SpawnPathPart(GetRandomPart(), prevPart);

        path[^1].onCarEnterAction += OnPartEnter;
        path[^2].onCarEnterAction -= OnPartEnter;

        while (path.Count > maxPartCount)
        {
            Destroy(path[0].gameObject);
            path.RemoveAt(0);
        }
    }

    private void OnPartEnter() => CreateNewPart();

    private PathPart SpawnPathPart(PathPart pref, PathPart prev)
    {
        PathPart part = Instantiate(pref);
        part.transform.SetParent(prev.transform.parent);
        PathGenerator.SetRotationOfPart(part, prev, Random.Range(0, 3) != 0);

        foreach (Checkpoint point in part.checkpoints) point.Show(false);
        if (obstacleCones) part.SpawnRandomCones();

        path.Add(part);

        return part;
    }

    private PathPart GetRandomPart()
    {
        PathPart[] p = database.GetParts(true, true, true);

        return p[Random.Range(0, p.Length)];
    }
}
