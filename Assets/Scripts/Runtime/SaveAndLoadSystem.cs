using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public static class SaveAndLoadSystem
{
    public static string SavePath => Application.persistentDataPath + "/Save";

    public static T LoadDataFromDisk<T>() => LoadDataFromDisk<T>(SavePath);
    public static void SaveDataOntoDisk(object data) => SaveDataOntoDisk(SavePath, data);

    private static T LoadDataFromDisk<T>(string path)
    {
        if (File.Exists(path))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(path, FileMode.Open);

            object data = formatter.Deserialize(stream);
            stream.Close();

            return (T)data;
        }

        return default(T);
    }

    private static void SaveDataOntoDisk(string path, object data)
    {
        BinaryFormatter formatter = new BinaryFormatter();
        FileStream stream = new FileStream(path, FileMode.Create);
        formatter.Serialize(stream, data);

        stream.Close();
    }
}

[System.Serializable]
public class Save
{
    public Data simData;

    public NeuralNetwork parent1;
    public NeuralNetwork parent2;

    public NeuralNetwork[] bestNeuralNetworks;
    public NeuralNetwork[] secondbestNeuralNetworks;
    public TimeSpan[] bestNetworkTrainingTimes;

    // Graphs only
    public GenerationResult[] genResults;
    public float[] finishTimes;
}
