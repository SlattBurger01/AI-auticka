using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GraphRenderer : MonoBehaviour
{
    [SerializeField] private float width;
    [SerializeField] private float height;

    [SerializeField] private float defaultZeroOffset = 40;

    // points with its original values
    private List<Vector2>[] points = new List<Vector2>[3] { new List<Vector2>(), new List<Vector2>(), new List<Vector2>() };

    private List<Vector2>[] pointsRefs = new List<Vector2>[3] { new List<Vector2>(), new List<Vector2>(), new List<Vector2>() };
    private Color[] colors = new Color[3] { Color.magenta, Color.yellow, Color.red };

    [SerializeField] private Transform linesParent;
    [SerializeField] private Transform infoLinesParent;

    [SerializeField] private Vector2 scale = new Vector2(20, 10);
    [SerializeField] private float lineWidth = 4;

    private float zeroOffset;

    [SerializeField] private bool disableIfUnused;

    private void Start()
    {
        zeroOffset = defaultZeroOffset;

        if (disableIfUnused) gameObject.SetActive(false);
    }

    private int testId;

    private void Update()
    {
        return;

        if (Input.GetKeyDown(KeyCode.P))
        {
            AddPointsOnly(-testId);
            testId++;
        }

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKey(KeyCode.KeypadEnter))
        {
            AddPointsOnly(Random.Range(0, 50 + testId));
            testId++;
        }
    }

    private bool redraw = false;

    private void LateUpdate()
    {
        if (!redraw) return;

        UpdateScaleAndRedraw();
        redraw = false;
    }

    public void AddPointsOnly(float yVal) => AddPointsOnly(new float[] { yVal });

    private float curXPos = 0;

    public void AddPointsOnly(float[] yVals) 
    {
        for (int i = 0; i < yVals.Length; i++)
        {
            Vector2 values = new Vector2(curXPos, yVals[i]);
            points[i].Add(values);
        }

        curXPos++;

        redraw = true;
    }

    private static readonly string numFormat = "F2";

    [SerializeField] private bool maxIsPrimary = true;

    private static readonly float minDottedLineDist = 27; // minimal distance (of dotted line) from 0 to be drawn

    private void UpdateScaleAndRedraw()
    {
        UpdateScale(out float yMax, out float yMin);

        Redraw();

        if (points[0].Count >= 1)
        {
            float maxDottedLinePos = Mathf.Abs(yMax) * scale.y;
            float minDottedLinePos = Mathf.Abs(yMin) * scale.y;

            float relativeLinesDist = Mathf.Abs(maxDottedLinePos - minDottedLinePos);

            //print($"{name}: {relativeLinesDist}, ({maxDottedLinePos}, {minDottedLinePos})");

            if (relativeLinesDist > minDottedLineDist) // draw both
            {
                if (maxDottedLinePos > minDottedLineDist) DrawDottedLine(GetYCoordinates(yMax), $"{yMax.ToString(numFormat)}");
                if (Mathf.Abs(minDottedLinePos) > minDottedLineDist) DrawDottedLine(GetYCoordinates(yMin), $"{yMin.ToString(numFormat)}");
            }
            else // draw primary only
            {
                if (maxIsPrimary)
                {
                    if (maxDottedLinePos > minDottedLineDist) DrawDottedLine(GetYCoordinates(yMax), $"{yMax.ToString(numFormat)}");
                }
                else
                {
                    if (Mathf.Abs(minDottedLinePos) > minDottedLineDist) DrawDottedLine(GetYCoordinates(yMin), $"{yMin.ToString(numFormat)}");
                }
            }
        }
    }

    private float GetYCoordinates(float value) { return value * scale.y + zeroOffset; }

    private void AddPointF(Vector2 values, int id)
    {
        Vector2 fValue = GetPointFinalValue(values);

        if (pointsRefs[id].Count != 0)
        {
            CreateLine(pointsRefs[id][^1], fValue, colors[id]);
        }

        pointsRefs[id].Add(fValue);

        if (!gameObject.activeSelf) gameObject.SetActive(true);
    }

    private Vector2 GetPointFinalValue(Vector2 original) { return (original * scale) + new Vector2(0, zeroOffset); }

    private void UpdateScale(out float yMax, out float yMin)
    {
        yMax = float.MinValue;
        yMin = float.MaxValue;

        float xMax = points[0].Count != 0 ?points[0][^1].x : 0; // last point is expected to have the highest x pos

        // Get highest & lowest point
        for (int i = 0; i < points.Length; i++)
        {
            for (int y = 0; y < points[i].Count; y++)
            {
                float yV = points[i][y].y;

                if (yV > yMax) yMax = yV;
                if (yV < yMin) yMin = yV;
            }
        }

        float targetY = height - 100;
        float targetX = width - 20;

        float scaleY = scale.y;
        float scaleX = scale.x;

        if (yMax != 0 || yMin != 0) scaleY = targetY / (Mathf.Abs(yMax) + Mathf.Abs(yMin));

        if (xMax != 0) scaleX = targetX / Mathf.Abs(xMax);

        scale = new Vector2(scaleX, scaleY);

        zeroOffset = Mathf.Abs(yMin) * scale.y + defaultZeroOffset;
    }

    public void Redraw()
    {
        //pointsRefs = new List<GameObject>[3] { new List<GameObject>(), new List<GameObject>(), new List<GameObject>() };
        pointsRefs  = new List<Vector2>[3] { new List<Vector2>(), new List<Vector2>(), new List<Vector2>() };

        //DestroyAllChilds(pointsParent);
        DestroyAllChilds(linesParent);
        DestroyAllChilds(infoLinesParent);

        for (int i = 0; i < points.Length; i++)
        {
            for (int y = 0; y < points[i].Count; y++)
            {
                AddPointF(points[i][y], i);
            }
        }

        DrawDottedLine(zeroOffset, "0");
    }

    private void DestroyAllChilds(Transform parent)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Destroy(parent.GetChild(i).gameObject);
        }
    }

    // ----- GRAPH RENDERING ----
    private static readonly Color dottetLineColor = Color.white;

    private void DrawDottedLine(float yPos, string lineText)
    {
        GameObject tClone = new GameObject();
        tClone.transform.SetParent(infoLinesParent);

        DrawDottedLineText(tClone, yPos, lineText);

        for (int i = 0; i < 50; i++)
        {
            GameObject clone = new GameObject();

            Image img = clone.AddComponent<Image>();
            img.color = dottetLineColor;

            RectTransform rect = clone.GetComponent<RectTransform>();
            rect.SetParent(infoLinesParent);
            rect.localScale = Vector3.one * .05f;

            rect.transform.localPosition = new Vector2(i * 10, yPos);
        }
    }

    private static void DrawDottedLineText(GameObject tClone, float yPos, string lineText)
    {
        TextMeshProUGUI text = tClone.AddComponent<TextMeshProUGUI>();

        text.alignment = TextAlignmentOptions.MidlineRight;

        text.text = lineText;
        text.color = dottetLineColor;

        text.enableAutoSizing = true;
        text.fontSizeMax = 34;

        // --- --- ---
        RectTransform tr = text.GetComponent<RectTransform>();

        tr.localPosition = new Vector2(-25, yPos);
        tr.localScale = Vector3.one;

        tr.sizeDelta = new Vector2(95, 50);
        tr.pivot = new Vector2(1, .5f);
        // --- --- ---
    }

    private void CreateLine(Vector2 p1, Vector2 p2, Color col) => UIRenderer.CreateLine(p1, p2, col, lineWidth, linesParent);
}
