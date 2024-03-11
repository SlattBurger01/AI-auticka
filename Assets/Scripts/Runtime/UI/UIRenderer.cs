using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class UIRenderer
{
    public static Image CreateLine(Vector2 p1, Vector2 p2, Color col, float width, Transform parent)
    {
        if (p1 == p2) Debug.LogError($"Cannot draw line from {p1} to {p2}");

        GameObject clone = new GameObject();

        clone.name = "line from " + p1.x + " to " + p2.x;
        Image img = clone.AddComponent<Image>();
        img.color = col;

        RectTransform rect = clone.GetComponent<RectTransform>();
        rect.SetParent(parent);
        rect.localScale = Vector3.one;

        Vector3 a = new Vector3(p1.x, p1.y, 0);
        Vector3 b = new Vector3(p2.x, p2.y, 0);

        rect.localPosition = (a + b) / 2;

        Vector3 dif = a - b;
        rect.sizeDelta = new Vector3(dif.magnitude, width);
        rect.rotation = Quaternion.Euler(new Vector3(0, 0, 180 * Mathf.Atan(dif.y / dif.x) / Mathf.PI));

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;

        return img;
    }
}
