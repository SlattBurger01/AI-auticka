using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NeuralNetworkDisplayer : MonoBehaviour
{
    private static readonly string textValuesRounding = "F2";

    [SerializeField] private bool displayBiases;
    [SerializeField] private bool displayTexts;
    public bool displayActivatedNeurons;

    [SerializeField] private Transform parent;
    [SerializeField] private Transform parent1;

    [SerializeField] private float weightLimitMin = 0.01f; // absolute value of weight
    [SerializeField] private float weightLimitMax = 30; // absolute value of weight
    [SerializeField] private float weightMultiplayer = 5; // absolute value of weight

    private List<List<GameObject>> layers_ = new List<List<GameObject>>();

    private List<Image> connections = new();

    private List<TextMeshProUGUI> inputTexts = new List<TextMeshProUGUI>();
    private List<TextMeshProUGUI> outputTexts = new List<TextMeshProUGUI>();

    private NeuralNetwork tNetwork;
    private string[] tInputNames;
    private string[] tOutputNames;

    public void SetTargetNetwork(NeuralNetwork network, string[] inputNames, string[] outputNames)
    {
        gameObject.SetActive(true);

        tNetwork = network;

        tInputNames = inputNames;
        tOutputNames = outputNames;

        //Display(tNetwork);
        OverrideDisplay(tNetwork);

        UpdateTexts();
    }

    public void PresetNetwork(NeuralNetwork network)
    {
        Display(network);

        gameObject.SetActive(false);
    }

    private float timeLeftToDisplay;
    private static readonly float pauseTime = .05f;

    private void Start()
    {
        if (displayTexts) return;

        print("Start");
        Vector2 tPos = transform.position - new Vector3(90, 0, 0) * GetComponentInParent<Canvas>().scaleFactor;
        transform.position = tPos;
    }

    private void Update()
    {
        /*if (Input.GetKeyDown(KeyCode.Space))
        {
            OverrideDisplay(tNetwork);
        }*/

        if (!tNetwork) return;

        if (timeLeftToDisplay <= 0)
        {
            UpdateTexts();
            timeLeftToDisplay = pauseTime;
        }
        else timeLeftToDisplay -= Time.deltaTime;
    }

    private void UpdateTexts()
    {
        UpdateInputs(tNetwork.layers[0].values, tInputNames);
        UpdateOutputs(tNetwork.outputs, tOutputNames);
    }

    private void UpdateInputs(float[] inputs, string[] ids)
    {
        if (displayActivatedNeurons)
        {
            for (int i = 0; i < layers_[0].Count; i++)
            {
                bool b = i > layers_[0].Count - 3;
                float middlePoint = b ? 0 : 2f;

                //print($"({i}) {ids[i]}: {middlePoint}, {b}");

                layers_[0][i].GetComponentInChildren<Image>().color = GetNeuronColor(inputs[i], false, middlePoint, b);
            }
        }

        for (int i = 0; i < inputTexts.Count; i++)
        {
            inputTexts[i].text = $"{ids[i]}: {inputs[i].ToString(textValuesRounding)}";
        }
    }

    private void UpdateOutputs(float[] outputs, string[] ids)
    {
        if (displayActivatedNeurons)
        {
            for (int i = 0; i < layers_[^1].Count; i++)
            {
                layers_[^1][i].GetComponentInChildren<Image>().color = GetNeuronColor(outputs[i], i == layers_[^1].Count - 1);
            }
        }

        for (int i = 0; i < outputTexts.Count; i++)
        {
            string o = i != outputTexts.Count - 1 ? (outputs[i] - .5f).ToString(textValuesRounding) : NeuralNetwork.FloatOutputToBool(outputs[i]).ToString();

            outputTexts[i].text = $"{ids[i]}: {o}";
        }
    }

    private static readonly Color activatedColor = Color.green;
    private static readonly Color sleepColor = Color.white;

    private Color GetNeuronColor(float v, bool isBrake, float middlePoint = .5f, bool larger = true)
    {
        if (isBrake) return NeuralNetwork.FloatOutputToBool(v) ? activatedColor : sleepColor;

        bool b = larger ? v > middlePoint : v < middlePoint;

        return b ? activatedColor : sleepColor;
    }

    private void Display(NeuralNetwork network)
    {
        DestroyAllChilds(parent);
        DestroyAllChilds(parent1);

        layers_.Clear();
        inputTexts.Clear();
        outputTexts.Clear();

        connections.Clear();

        if (displayTexts)
        {
            CreateInputs(network.layers);
            CreateOutputs(network.layers);
        }

        for (int i = 0; i < network.layers.Length; i++)
        {
            layers_.Add(DrawLayer(network.layers[i], i));
        }

        for (int i = 0; i < network.layers.Length - 1; i++)
        {
            connections.AddRange(DrawConnections(layers_[i], layers_[i + 1], network.layers[i]));
        }
    }

    private void OverrideDisplay(NeuralNetwork network)
    {
        int currentNeuronId = 0;

        for (int i = 0; i < network.layers.Length; i++)
        {
            OverwriteDrawLayer(layers_[i], network.layers[i], ref currentNeuronId);
        }

        int id = 0;

        for (int i = 0; i < network.layers.Length - 1; i++)
        {
            OverwriteConnections(layers_[i], layers_[i + 1], network.layers[i], ref id);
        }
    }

    private void CreateInputs(Layer[] layers) => CreateTexts(layers[0], -1, inputTexts, new Vector2(160 - textOffsetD, 0), TextAlignmentOptions.MidlineRight);

    private void CreateOutputs(Layer[] layers) => CreateTexts(layers.Last(), layers.Length, outputTexts, new Vector2(90 + textOffsetD, 0), TextAlignmentOptions.MidlineLeft);

    private void CreateTexts(Layer layer, int xPos, List<TextMeshProUGUI> texts, Vector2 offset, TextAlignmentOptions aligment)
    {
        for (int i = 0; i < layer.NeuronCount; i++)
        {
            float centerOffSet = ((float)layer.NeuronCount / 2) * spacing.y;

            GameObject clone = new GameObject();
            clone.transform.parent = parent1;

            TextMeshProUGUI text = clone.AddComponent<TextMeshProUGUI>();
            text.alignment = aligment;

            clone.transform.localPosition = GetNeuronPosition(xPos, i, centerOffSet) + offset; // + new Vector2(0, 20); fixed
            clone.transform.localScale = Vector3.one;

            clone.GetComponent<RectTransform>().pivot = new Vector2(1, .5f);
            clone.GetComponent<RectTransform>().sizeDelta = new Vector2(250, 50);

            texts.Add(text);
        }
    }

    private Vector2 spacing = new Vector2(200, 82);

    private float textOffsetD = 10;

    private List<GameObject> DrawLayer(Layer layer, int id)
    {
        float centerOffSet = ((float)layer.NeuronCount / 2) * spacing.y;

        List<GameObject> objs = new List<GameObject>();

        for (int i = 0; i < layer.NeuronCount; i++)
        {
            GameObject clone = new GameObject();
            clone.transform.parent = parent1;

            clone.name = $"Neuron {id}";

            clone.AddComponent<Image>();

            if (displayBiases) DrawBias(clone.transform, layer.biases[i]);

            clone.transform.localPosition = GetNeuronPosition(id, i, centerOffSet);
            clone.transform.localScale = Vector3.one * .5f;

            objs.Add(clone);
        }

        return objs;
    }

    private static void DrawBias(Transform parent, float bias)
    {
        GameObject clone1 = new GameObject();
        clone1.transform.parent = parent;

        TextMeshProUGUI text = clone1.AddComponent<TextMeshProUGUI>();

        clone1.GetComponent<RectTransform>().sizeDelta = Vector2.one * 100;

        text.text = bias.ToString("F2");
        text.color = Color.black;
        text.alignment = TextAlignmentOptions.Center;
    }

    private void OverwriteDrawLayer(List<GameObject> objs, Layer layer, ref int currentNeuronId)
    {
        for (int i = 0; i < layer.NeuronCount; i++)
        {
            objs[i].GetComponentInChildren<Image>().color = NeuralNetwork.NeuronIsEnabled(currentNeuronId) ? Color.white : Color.black;

            if (displayBiases)
            {
                TextMeshProUGUI text = objs[i].GetComponentInChildren<TextMeshProUGUI>();

                text.text = layer.biases[i].ToString("F2");
            }

            currentNeuronId++;
        }
    }

    private Vector2 GetNeuronPosition(int id, int i, float offset)
    {
        return new Vector2(id * spacing.x, (i * spacing.y) - offset);
    }

    private List<Image> DrawConnections(List<GameObject> l1, List<GameObject> l2, Layer layer1)
    {
        List<Image> connections = new();

        for (int i = 0; i < l1.Count; i++)
        {
            for (int y = 0; y < l2.Count; y++)
            {
                (Color, float) lineData = GetWeightData(layer1.weights[i + y]);

                Image img = UIRenderer.CreateLine(l1[i].transform.localPosition, l2[y].transform.localPosition, lineData.Item1, lineData.Item2, parent);

                connections.Add(img);
            }
        }

        return connections;
    }

    private void OverwriteConnections(List<GameObject> l1, List<GameObject> l2, Layer layer1, ref int id)
    {
        for (int i = 0; i < l1.Count; i++)
        {
            for (int y = 0; y < l2.Count; y++)
            {
                (Color, float) lineData = GetWeightData(layer1.weights[i + y]);

                RectTransform t = connections[id].GetComponent<RectTransform>();
                t.sizeDelta = new Vector2(t.sizeDelta.x, lineData.Item2);
                connections[id].color = lineData.Item1;

                id++;
            }
        }
    }

    private (Color, float) GetWeightData(float weight) // color, width
    {
        Color color = weight > 0 ? Color.green : Color.red;

        float width = Mathf.Abs(weight * weightMultiplayer);

        width = Mathf.Min(width, weightLimitMax);
        if (width < weightLimitMin) width = 0;

        return (color, width);
    }

    private static void DestroyAllChilds(Transform tr)
    {
        for (int i = 0; i < tr.childCount; i++)
        {
            Destroy(tr.GetChild(i).gameObject);
        }
    }
}
