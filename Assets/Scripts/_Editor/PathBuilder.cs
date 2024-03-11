#if UNITY_EDITOR
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class PathBuilder : EditorWindow
{
    // To do: don't implement drag and drop menu, start by selecting (clicking) the path you want as first

    private PartsDatabase database;

    private PathPart basePart;

    [MenuItem("Builder/PathBuilder")]
    public static void ShowWindow()
    {
        PathBuilder b = GetWindow<PathBuilder>(false, "PathBuilder", true);
        b.Setup();
    }

    private void Setup()
    {
        string[] parts = AssetDatabase.GetAllAssetPaths();

        for (int i = 0; i < parts.Length; i++)
        {
            PartsDatabase d = (PartsDatabase)AssetDatabase.LoadAssetAtPath(parts[i], typeof(PartsDatabase));

            if (d) { database = d; break; }
        }
    }

    private void OnGUI()
    {
        GUILayout.Label($"Locked part= {basePart}");

        if (GUILayout.Button($"Lock part"))
        {
            PathPart p = Selection.gameObjects[0].GetComponent<PathPart>();

            if (p) LockBasePart(p);

        }

        GUILayout.Space(20);

        DrawPathBuilder();

        GUILayout.Space(40);

        xAxis = GUILayout.Toggle(xAxis, "xAxis");
        if (GUILayout.Button("Center path point")) CenterPoint(xAxis);
        if (GUILayout.Button("Create start & end")) CreateStartAndEnd();

        GUILayout.EndVertical();
    }

    private bool straightP = true, turnP = true, otherP = true;

    private PathPart[] GetParts() => database.GetParts(straightP, turnP, otherP);

    private void DrawPathBuilder()
    {
        GUI.enabled = basePart != null;

        DrawToggles();

        DrawPartChanging();

        GUILayout.Space(20);

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Rotate")) RotatePart(basePart);

        GUILayout.EndHorizontal();

        GUILayout.BeginVertical();

        GUILayout.Space(10);

        if (GUILayout.Button("Place part", GUILayout.Height(40))) PlacePart();

        if (GUILayout.Button("STOP")) StopBuilding();

        GUI.enabled = true;
    }

    private void DrawToggles()
    {
        GUILayout.BeginHorizontal();

        straightP = GUILayout.Toggle(straightP, "straightP");
        turnP = GUILayout.Toggle(turnP, "turnP");
        otherP = GUILayout.Toggle(otherP, "otherP");

        GUILayout.EndHorizontal();
    }

    private void DrawPartChanging()
    {
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Previous part")) ChangePart(false);
        if (GUILayout.Button("Next part")) ChangePart(true);

        GUILayout.EndHorizontal();
    }

    private int currentPart;

    private bool xAxis;

    private PathPart tempPart;

    private void ChangePart(bool up)
    {
        PathPart[] parts = GetParts();

        if (up)
        {
            currentPart++;

            if (currentPart >= parts.Length) currentPart = 0;
        }
        else
        {
            currentPart--;

            if (currentPart < 0) currentPart = parts.Length - 1;
        }

        TryDestroyTempPart();
        DisplayNewPart(parts[currentPart], basePart);
    }

    private void LockBasePart(PathPart part)
    {
        PathPart[] parts = GetParts();

        basePart = part;
        r = true;

        Debug.Log(basePart);

        DisplayNewPart(parts[currentPart], basePart);

        MoveSceneCamera(basePart);
    }

    private bool r = true;

    private void PlacePart() => LockBasePart(tempPart);

    private void RotatePart(PathPart prevPart) => SetRotationOfPart(tempPart, prevPart, r = !r);

    private void MoveSceneCamera(PathPart part)
    {
        SceneView scene = SceneView.lastActiveSceneView;
        scene.pivot = new Vector3(part.transform.position.x, part.transform.position.y, scene.pivot.z);
        scene.Repaint();
    }

    private void DisplayNewPart(PathPart part, PathPart prevPart)
    {
        tempPart = SpawnPart(part, prevPart);

        SetRotationOfPart(tempPart, prevPart, true);
    }

    private static void SetRotationOfPart(PathPart part, PathPart prevPart, bool start)
    {
        PathGenerator.SetRotationOfPart(part, prevPart, start);
        EditorUtility.SetDirty(part);
    }

    private void StopBuilding() => TryDestroyTempPart();

    private void TryDestroyTempPart() 
    { 
        if (tempPart) DestroyImmediate(tempPart.gameObject); 
        tempPart = null; 
    }

    private PathPart SpawnPart(PathPart part, PathPart prevPart)
    {
        PathPart clone = InstantiatePathPrefab(part);

        clone.transform.SetAsLastSibling();

        return clone;
    }

    private PathPart InstantiatePathPrefab(PathPart part)
    {
        //Object clone = PrefabUtility.InstantiatePrefab(part);
        return PrefabUtility.InstantiatePrefab(part).GetComponent<PathPart>(); // other conversions did not worked :/
    }

    // --- PATH PART - not really connected with Path building
    private void CenterPoint(bool xAxis)
    {
        if (Selection.gameObjects.Length != 3) return;

        Transform pointToCenter = null;
        List<Transform> walls = new List<Transform>();

        PathPart p = Selection.gameObjects[0].GetComponentInParent<PathPart>();

        for (int i = 0; i < Selection.gameObjects.Length; i++)
        {
            Transform tr = Selection.gameObjects[i].transform;

            if (tr == p.startPos || tr == p.endPos)
            {
                pointToCenter = tr;
            }
            else walls.Add(tr);
        }

        if (xAxis)
        {
            float xpos = (walls[0].transform.position.x + walls[1].transform.position.x) / 2;
            pointToCenter.position = new Vector2(xpos, pointToCenter.transform.position.y);
        }
        else
        {
            float ypos = (walls[0].transform.position.y + walls[1].transform.position.y) / 2;
            pointToCenter.position = new Vector2(pointToCenter.transform.position.x, ypos);
        }

        EditorUtility.SetDirty(pointToCenter.root.gameObject);
    }

    private void CreateStartAndEnd()
    {
        if (Selection.gameObjects.Length <= 0) return;

        PathPart p = Selection.gameObjects[0].GetComponent<PathPart>();

        p.startPos = CreatePoint(p.transform, "start");
        p.endPos = CreatePoint(p.transform, "end");
    }

    private static Transform CreatePoint(Transform parent, string name)
    {
        GameObject point = new GameObject();
        point.transform.SetParent(parent);
        point.name = name;
        return point.transform;
    }
}
#endif