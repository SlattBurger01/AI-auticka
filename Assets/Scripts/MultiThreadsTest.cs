using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using static NeuralNetwork;

public class MultiThreadsTest : MonoBehaviour
{

}

public static class NeuralNetworkDeconstructor
{
    public static (NativeArray<int>, NativeArray<float>, NativeArray<float>, NativeArray<float>) DeconstructNeuralNetworks(NeuralNetwork[] networks, out int totLength1, out int totLength2)
    {
        NativeArray<int> layersLengths = new NativeArray<int>(networks[0].layers.Length, Allocator.TempJob);

        for (int i = 0; i < networks[0].layers.Length; i++) layersLengths[i] = networks[0].layers[i].values.Length;

        totLength1 = 0;
        totLength2 = 0;

        for (int i = 0; i < networks[0].layers.Length; i++)
        {
            totLength1 += networks[0].layers[i].values.Length;
            totLength2 += networks[0].layers[i].weights.Length;
        }

        NativeArray<float> values = new NativeArray<float>(totLength1 * networks.Length, Allocator.TempJob);
        NativeArray<float> biases = new NativeArray<float>(totLength1 * networks.Length, Allocator.TempJob);
        NativeArray<float> weights = new NativeArray<float>(totLength2 * networks.Length, Allocator.TempJob);

        int fId = 0;
        int fId1 = 0;

        for (int x = 0; x < networks.Length; x++)
        {
            for (int i = 0; i < networks[x].layers.Length; i++)
            {
                for (int j = 0; j < networks[x].layers[i].values.Length; j++)
                {
                    values[fId] = networks[x].layers[i].values[j];
                    biases[fId] = networks[x].layers[i].biases[j];

                    fId++;
                }

                for (int j = 0; j < networks[x].layers[i].weights.Length; j++)
                {
                    weights[fId1] = networks[x].layers[i].weights[j];

                    fId1++;
                }
            }
        }

        return (layersLengths, values, biases, weights);
    }
}

public class TestHistory
{
    private static (NativeArray<int>, NativeArray<float>, NativeArray<float>, NativeArray<float>) DeconstructNeuralNetwork(NeuralNetwork network)
    {
        NativeArray<int> layersLengths = new NativeArray<int>(network.layers.Length, Allocator.TempJob);

        int totLength1 = 0;
        int totLength2 = 0;

        for (int i = 0; i < network.layers.Length; i++)
        {
            totLength1 += network.layers[i].values.Length;
            totLength2 += network.layers[i].weights.Length;
        }

        NativeArray<float> values = new NativeArray<float>(totLength1, Allocator.TempJob);
        NativeArray<float> biases = new NativeArray<float>(totLength1, Allocator.TempJob);

        NativeArray<float> weights = new NativeArray<float>(totLength2, Allocator.TempJob);

        int fId = 0;
        int fId1 = 0;

        for (int i = 0; i < network.layers.Length; i++)
        {
            layersLengths[i] = network.layers[i].values.Length;

            for (int j = 0; j < network.layers[i].values.Length; j++)
            {
                values[fId] = network.layers[i].values[j];
                biases[fId] = network.layers[i].biases[j];

                fId++;
            }

            for (int j = 0; j < network.layers[i].weights.Length; j++)
            {
                weights[fId] = network.layers[i].weights[j];

                fId1++;
            }
        }

        return (layersLengths, values, biases, weights);
    }

    public static NeuralNetwork ConstructNeuralNetwork(NativeArray<int> layersLengths, NativeArray<float> values, NativeArray<float> biases, NativeArray<float> weights)
    {
        Layer[] layers = new Layer[layersLengths.Length];

        for (int i = 0; i < layersLengths.Length; i++)
        {
            int nextLayerL = i == layersLengths.Length - 1 ? 0 : layersLengths[i + 1];

            Layer layer = Layer.NewLayer(layersLengths[i], nextLayerL);

            layer.values = values.GetSubArray(i * layersLengths[i], layersLengths[i]).ToArray();
            layer.biases = biases.GetSubArray(i * layersLengths[i], layersLengths[i]).ToArray();
            layer.weights = weights.GetSubArray(i * layersLengths[i] * nextLayerL, layersLengths[i] * nextLayerL).ToArray();

            layers[i] = layer;
        }

        return new NeuralNetwork(layers);
    }

    private void Test()
    {
        int length = 4_000_000;

        float[] inp = new float[length];
        for (int i = 0; i < length; i++) { inp[i] = i; }

        NativeArray<float> input = new NativeArray<float>(inp, Allocator.Persistent);
        NativeArray<float> output = new NativeArray<float>(length, Allocator.Persistent);
        NativeArray<float> output1 = new NativeArray<float>(length, Allocator.Persistent);
        NativeArray<float> output2 = new NativeArray<float>(length, Allocator.Persistent);

        JobHandle j1 = new MyJob() { Input = input, Output = output }.Schedule();
        JobHandle j2 = new MyJob() { Input = input, Output = output1 }.Schedule();
        JobHandle j3 = new MyJob() { Input = input, Output = output2 }.Schedule();

        JobHandle.CompleteAll(ref j1, ref j2, ref j3);

        input.Dispose();
        output.Dispose();
        output1.Dispose();
        output2.Dispose();
    }


    private void JobRaycast(int l)
    {
        var results = new NativeArray<RaycastHit>(l, Allocator.TempJob);
        var commands = new NativeArray<RaycastCommand>(l, Allocator.TempJob);

        for (int i = 0; i < l; i++)
        {
            commands[i] = new RaycastCommand(Vector3.zero, Vector3.up);
        }

        JobHandle handler = RaycastCommand.ScheduleBatch(commands, results, 1);

        handler.Complete();

        results.Dispose();
        commands.Dispose();
    }

    private void TestRaycasts()
    {
        Raycasts(100_000);
        Raycasts2D(100_000);
    }

    private void Raycasts(int l)
    {
        for (int i = 0; i < l; i++)
        {
            Physics.Raycast(Vector3.zero, Vector3.up);
        }
    }

    private void Raycasts2D(int l)
    {
        for (int i = 0; i < l; i++)
        {
            Physics2D.Raycast(Vector2.zero, Vector2.up);
        }
    }
}

[BurstCompile]
public struct MyJob : IJob
{
    [ReadOnly]
    public NativeArray<float> Input;

    [WriteOnly]
    public NativeArray<float> Output;

    public void Execute()
    {
        for (int i = 0; i < Input.Length; i++)
        {
            Output[i] = MathHelper.Sigmoid(Input[i]);
        }
    }
}