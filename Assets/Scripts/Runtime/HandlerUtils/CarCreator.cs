using System;
using UnityEditor;
using UnityEngine;

public class CarCreator
{
    private readonly GameObject carPrefab;
    private readonly Path targetPath;

    public CarCreator(GameObject cP, Path tP)
    {
        carPrefab = cP;
        targetPath = tP;
    }

    public Car[] CreateCars(int count, int addCarsCount, NeuralNetwork parent1 = null, NeuralNetwork parent2 = null)
    {
        Car[] cars = new Car[count];

        for (int i = 0; i < count - addCarsCount; i++) // -1 because of the copied car
        {
            if (parent1 != null) cars[i] = CreateNewCar(parent1, parent2);
            else cars[i] = CreateNewCar();

            cars[i].name = $"Car {i}";
            cars[i].id = i;
        }

        return cars;
    }

    public Car CreateNewCar(NeuralNetwork parent1 = null, NeuralNetwork parent2 = null)
    {
        Layer[] layers = CarNetworkCreator.CreateNewCarLayers();

        if (parent1) NeuralNetwork.MergeLayers(ref layers, parent1, parent2);
        else NetworkRandomizer.FullRandomizeLayers(ref layers);

        return CreateNewCarF(new NeuralNetwork(layers));
    }

    public Car CreateNewCarF(NeuralNetwork network)
    {
        GameObject clone = SpawnCarF();
        Car car = clone.GetComponent<Car>();

        car.network = network;

        //HandlerUtils.SetFirstCheckpoint(car, targetPath);

        return car;
    }

    public Car CopyCar(NeuralNetwork parent)
    {
        GameObject clone = SpawnCarF();
        Car car = clone.GetComponent<Car>();

        car.transform.GetChild(0).name = "Copy";

#if UNITY_EDITOR
        GUIContent iconContent = EditorGUIUtility.IconContent("sv_label_1");
        EditorGUIUtility.SetIconForObject(clone, (Texture2D)iconContent.image);
#endif

        //HandlerUtils.SetFirstCheckpoint(car, targetPath);

        car.network = parent;
        return car;
    }

    private GameObject SpawnCarF() => UnityEngine.Object.Instantiate(carPrefab, targetPath.carSpawnPoint.position, targetPath.carSpawnPoint.rotation);
}

public static class CarNetworkCreator
{
    private static readonly int[] hiddenLayersNeurons = new int[] { 15, 15 }; //{ 12, 12, 12 }; // 11, 11, 11

    public static Layer[] CreateNewCarLayers() => GetLayers();

    private static Layer[] GetLayers()
    {
        Layer[] layers = new Layer[hiddenLayersNeurons.Length + 2];

        layers[0] = Layer.NewLayer(Car.inputsLength, hiddenLayersNeurons[0]);
        layers[^1] = Layer.NewLayer(Car.outputsLength, 0);

        for (int i = 1; i < layers.Length - 1; i++)
        {
            int nextLength = i == layers.Length - 2 ? Car.outputsLength : hiddenLayersNeurons[i];

            layers[i] = Layer.NewLayer(hiddenLayersNeurons[i - 1], nextLength);
        }

        return layers;
    }

    public static NeuralNetwork CreateNewCarNetwork() => new NeuralNetwork(CreateNewCarLayers());

    public static NeuralNetwork CreateNewRandomCarNetwork()
    {
        Layer[] layers = CreateNewCarLayers();
        NetworkRandomizer.FullRandomizeLayers(ref layers);
        return new NeuralNetwork(layers);
    }
}


