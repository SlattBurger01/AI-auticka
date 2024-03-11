using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TextArrayDisplayer : MonoBehaviour
{
    [SerializeField] private Transform textsParent;
    [SerializeField] private float spacing = 40;
    public Image background;

    private TMP_Text[] texts;

    private void Start()
    {
        texts = Path.GetComponentsInChildrenOrdered<TMP_Text>(textsParent);

        UpdateTextsPosition();
    }

    private void UpdateTextsPosition()
    {
        float yPos = texts[0].transform.localPosition.y;
        float xPos = texts[0].transform.localPosition.x;

        for (int i = 0; i < texts.Length; i++)
        {
            texts[i].transform.localPosition = new Vector2(xPos, yPos);
            yPos -= spacing;
        }
    }
}
