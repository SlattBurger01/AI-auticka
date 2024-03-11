using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;
using Settings = NeuralNetworkSettings;

public static class NeuralNetworkSettings
{
    public static readonly float mutationChance = .13f;
    public static readonly float mutationWeight = .11f;

    public static readonly float baseRange = 1;

    public enum MutationOption { Addition, Multiplication, Replacement, Sign }
    public static readonly float[] chances = new float[] { .1f, .85f, .0049f, 0.0001f };
}

[System.Serializable]
public class NeuralNetwork
{
    public static readonly ActivationFunction outputActivation = ActivationFunction.sigmoid;
    public static readonly ActivationFunction hiddenActivation = ActivationFunction.tanh;
    // ------ - ------ - ------ - ------

    private static int[] disabledNeurons = new int[0]; // refference to neuron from start (layer1 neurons + layer2 neurons ... )
    private static int disabledNeuronsLength;
    // ------ - ------ - ------ - ------

    public readonly Layer[] layers;
    public readonly float[] outputs;
    private readonly int totalNeuralCount;

    public NeuralNetwork(Layer[] layers_)
    {
        layers = layers_;
        outputs = new float[layers.Last().NeuronCount];

        totalNeuralCount = 0;
        for (int i = 0; i < layers.Length; i++) totalNeuralCount += layers[i].NeuronCount;
    }

    public static bool FloatOutputToBool(float f) { return f > .9f; }

    public static void DisableRandomNeurons(int count, NeuralNetwork refNetwork)
    {
        disabledNeuronsLength = count;

        disabledNeurons = new int[disabledNeuronsLength];
        int neuronCount = refNetwork.totalNeuralCount;

        for (int i = 0; i < disabledNeuronsLength; i++)
        {
            int disabledNeuron = Random.Range(refNetwork.layers[0].NeuronCount, neuronCount - refNetwork.layers[^1].NeuronCount);
            disabledNeurons[i] = disabledNeuron;
        }
    }

    public static bool NeuronIsEnabled(int neuronId)
    {
        if (disabledNeuronsLength == 0) return true;

        //return disabledNeurons.Contains(neuronId);

        for (int i = 0; i < disabledNeurons.Length; i++)
        {
            if (disabledNeurons[i] == neuronId) return false;
        }

        return true;
    }

    public float[] ProcessData(float[] inputValues)
    {
        SetInputValues(inputValues);
        return ProcessDataWithAssignedInputs();
    }

    public float[] ProcessDataWithAssignedInputs()
    {
        PropagateValues();

        ActivateOutputLayer();

        return outputs;
    }

    /// <summary> pass values through network </summary>
    private void PropagateValues()
    {
        int currentNeuronId = 0;

        for (int i = 0; i < layers.Length - 1; i++)
        {
            PassToNextLayer(layers[i], layers[i + 1], ref currentNeuronId);
        }
    }

    /// <summary> activate values from output layer </summary>
    private void ActivateOutputLayer()
    {
        for (int i = 0; i < outputs.Length; i++)
        {
            outputs[i] = ActivateNeuron(layers.Last().values[i], outputActivation);
        }
    }

    public void SetInputValues(float[] inputValues)
    {
        if (inputValues.Length != layers[0].NeuronCount) Debug.LogError($"Input values have wrong lenght! (i: {inputValues.Length}, n: {layers[0].NeuronCount})");

        for (int i = 0; i < layers[0].NeuronCount; i++)
        {
            layers[0].values[i] = inputValues[i];
        }
    }

    private void PassToNextLayer(Layer l1, Layer l2, ref int curNeuronId)
    {
        float l1Count = l1.NeuronCount;
        float l2Count = l2.NeuronCount;

        for (int y = 0; y < l2Count; y++)
        {
            l2.values[y] = 0;
        }

        for (int i = 0; i < l1Count; i++) // source neurons
        {
            if (!NeuronIsEnabled(curNeuronId))
            {
                curNeuronId++;
                continue;
            }

            for (int y = 0; y < l2Count; y++) // target neurons
            {
                float value = l1.values[i] + l1.biases[i];

                if (i != 0) value = ActivateNeuron(value, hiddenActivation); // don't activate input layer

                l2.values[y] += value * l1.weights[i + y];
            }

            curNeuronId++;
        }
    }

    public enum ActivationFunction { sigmoid, tanh, leakyRelu };

    private static float ActivateNeuron(float f, ActivationFunction a)
    {
        switch (a)
        {
            case ActivationFunction.sigmoid: return MathHelper.Sigmoid(f);
            case ActivationFunction.leakyRelu: return MathHelper.LeakyRelu(f);
            case ActivationFunction.tanh: return MathHelper.Tanh(f);
            default: throw new System.Exception("Uniplemented activation function !");
        }
    }

    // ------------------------------------------ --- ------------------------------------------
    public static bool CanMutate() => NetworkRandomizer.CanDo(Settings.mutationChance);
    private static Layer ChooseParent(Layer parent1, Layer parent2) { return NetworkRandomizer.CanDo(.5f) ? parent1 : parent2; }

    public static Layer MergeLayer(Layer parent1, Layer parent2)
    {
        Layer newLayer = Layer.CloneLayer(parent1);

        for (int i = 0; i < newLayer.WeightsCount; i++)
        {
            float weight = ChooseParent(parent1, parent2).weights[i];
            newLayer.weights[i] = NetworkRandomizer.RandomizedNeuron(weight, Settings.mutationWeight);
        }

        for (int i = 0; i < newLayer.NeuronCount; i++)
        {
            float bias = ChooseParent(parent1, parent2).biases[i];
            newLayer.biases[i] = NetworkRandomizer.RandomizedNeuron(bias, Settings.mutationWeight);
        }

        return newLayer;
    }

    public static void MergeLayers(ref Layer[] layers, NeuralNetwork p1, NeuralNetwork p2)
    {
        for (int i = 0; i < layers.Length; i++)
        {
            layers[i] = MergeLayer(p1.layers[i], p2.layers[i]);
        }
    }

    public static NeuralNetwork DeepCloneNetwork(NeuralNetwork parent)
    {
        Layer[] newLayers = new Layer[parent.layers.Length];

        for (int i = 0; i < newLayers.Length; i++)
        {
            newLayers[i] = Layer.DeepCloneLayer(parent.layers[i]);
        }

        return new NeuralNetwork(newLayers);
    }

    public static implicit operator bool(NeuralNetwork network) { return network != null && network.layers != null && network.layers.Length != 0; }
}
